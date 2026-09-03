using AppointmentManager.Api.Agent;
using AppointmentManager.Api.Agent.Tools;
using AppointmentManager.Api.Data;
using AppointmentManager.Api.GoogleCalendar;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

// Loads secrets (e.g. GoogleCalendar__ClientId) from the repo-root .env into
// process env vars, so they flow into IConfiguration without ever touching
// appsettings.json. Walks up from the working directory to find it; a no-op
// if none exists (e.g. in CI, where real env vars are set directly).
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=appointments.db"));

var liteLlmOptions = new LiteLlmOptions();
builder.Configuration.GetSection("LiteLlm").Bind(liteLlmOptions);
builder.Services.AddSingleton(liteLlmOptions);
builder.Services.AddHttpClient<LiteLlmClient>();

var googleCalendarOptions = new GoogleCalendarOptions();
builder.Configuration.GetSection("GoogleCalendar").Bind(googleCalendarOptions);
builder.Services.AddSingleton(googleCalendarOptions);
builder.Services.AddScoped<GoogleCalendarService>();

builder.Services.AddScoped<AppointmentTools>();
builder.Services.AddScoped<AgentOrchestrator>();

const string FrontendCorsPolicy = "FrontendCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    SeedData.EnsureSeeded(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
