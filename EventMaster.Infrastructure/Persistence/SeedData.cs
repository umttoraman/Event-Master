using EventMaster.Domain.Entities;
using EventMaster.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventMaster.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task EnsureSeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        if (await db.Users.AnyAsync(cancellationToken))
            return;

        var hasher = new PasswordHasher<User>();

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Role = UserRole.Admin,
            LastLogin = null
        };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");

        var organizer = new User
        {
            Id = Guid.NewGuid(),
            Username = "organizer",
            Role = UserRole.Organizer,
            LastLogin = null
        };
        organizer.PasswordHash = hasher.HashPassword(organizer, "Org123!");

        var customer = new User
        {
            Id = Guid.NewGuid(),
            Username = "customer",
            Role = UserRole.Customer,
            LastLogin = null
        };
        customer.PasswordHash = hasher.HashPassword(customer, "Cust123!");

        db.Users.AddRange(admin, organizer, customer);

        var roomA = new Room { Id = Guid.NewGuid(), RoomName = "Konferans A", Capacity = 50, IsAvailable = true };
        var roomB = new Room { Id = Guid.NewGuid(), RoomName = "Konferans B", Capacity = 30, IsAvailable = true };
        db.Rooms.AddRange(roomA, roomB);

        await db.SaveChangesAsync(cancellationToken);
    }
}
