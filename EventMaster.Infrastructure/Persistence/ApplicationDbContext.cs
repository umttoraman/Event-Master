using EventMaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventMaster.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(128);
            e.Property(x => x.PasswordHash).HasMaxLength(512);
        });

        modelBuilder.Entity<Room>(r =>
        {
            r.Property(x => x.RoomName).HasMaxLength(256);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(512);
            e.Property(x => x.PosterFileName).HasMaxLength(512);

            e.HasOne(x => x.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(x => x.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Room)
                .WithMany(r => r.Events)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Ticket>(t =>
        {
            t.Property(x => x.SeatNumber).HasMaxLength(64);
            t.Property(x => x.Price).HasPrecision(18, 2);

            t.HasOne(x => x.Event)
                .WithMany(ev => ev.Tickets)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            t.HasOne(x => x.User)
                .WithMany(u => u.PurchasedTickets)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            t.HasIndex(x => new { x.EventId, x.SeatNumber }).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(a =>
        {
            a.Property(x => x.Action).HasMaxLength(128);
            a.Property(x => x.EntityType).HasMaxLength(128);
            a.Property(x => x.EntityId).HasMaxLength(64);
            a.Property(x => x.Details).HasMaxLength(2000);

            a.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
