using AppointmentManager.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppointmentManager.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<AvailabilitySlot> Slots => Set<AvailabilitySlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ConversationSession> Sessions => Set<ConversationSession>();
    public DbSet<ConversationMessage> Messages => Set<ConversationMessage>();
    public DbSet<GoogleAuthToken> GoogleAuthTokens => Set<GoogleAuthToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AvailabilitySlot>()
            .HasOne(s => s.Provider)
            .WithMany(p => p.Slots)
            .HasForeignKey(s => s.ProviderId);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Provider)
            .WithMany()
            .HasForeignKey(a => a.ProviderId);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Slot)
            .WithMany()
            .HasForeignKey(a => a.SlotId);

        modelBuilder.Entity<ConversationMessage>()
            .HasOne(m => m.Session)
            .WithMany(s => s.Messages)
            .HasForeignKey(m => m.SessionId);
    }
}
