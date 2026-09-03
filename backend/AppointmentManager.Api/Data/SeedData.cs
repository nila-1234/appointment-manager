using AppointmentManager.Api.Data.Entities;

namespace AppointmentManager.Api.Data;

public static class SeedData
{
    public static void EnsureSeeded(AppDbContext db)
    {
        if (db.Providers.Any()) return;

        var providers = new[]
        {
            new Provider { Name = "Dr. Alice", Specialty = "General Practice" },
            new Provider { Name = "Dr. Bob", Specialty = "Dermatology" },
            new Provider { Name = "Dr. Chen", Specialty = "Physical Therapy" },
        };
        db.Providers.AddRange(providers);
        db.SaveChanges();

        var today = DateTime.UtcNow.Date.AddDays(1);
        var slots = new List<AvailabilitySlot>();

        foreach (var provider in providers)
        {
            for (var day = 0; day < 5; day++)
            {
                var date = today.AddDays(day);
                foreach (var hour in new[] { 9, 10, 11, 13, 14, 15 })
                {
                    var start = date.AddHours(hour);
                    slots.Add(new AvailabilitySlot
                    {
                        ProviderId = provider.Id,
                        StartTime = start,
                        EndTime = start.AddMinutes(30),
                        IsBooked = false
                    });
                }
            }
        }

        db.Slots.AddRange(slots);
        db.SaveChanges();
    }
}
