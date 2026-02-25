namespace Car.API.Infrastructure.Persistence;

using Car.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Configuration;
using Shared.Infrastructure.Messaging.Inbox;
using Shared.Infrastructure.Messaging.Outbox;

public class CarDbContext : DbContext, IOutboxInboxDbContext
{
    public CarDbContext(DbContextOptions<CarDbContext> options)
        : base(options) { }

    public DbSet<CarRental> CarRentals => Set<CarRental>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureOutboxInbox();

        modelBuilder.Entity<CarRental>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).ValueGeneratedNever();
            builder.Property(c => c.BookingId).IsRequired();
            builder.Property(c => c.ReservationCode).IsRequired();
            builder.Property(c => c.Status).HasConversion<string>();
            builder.ToTable("CarRentals");
        });
    }
}
