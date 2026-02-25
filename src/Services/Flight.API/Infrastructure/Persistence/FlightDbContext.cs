namespace Flight.API.Infrastructure.Persistence;

using Flight.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Configuration;
using Shared.Infrastructure.Messaging.Inbox;
using Shared.Infrastructure.Messaging.Outbox;

public class FlightDbContext : DbContext, IOutboxInboxDbContext
{
    public FlightDbContext(DbContextOptions<FlightDbContext> options)
        : base(options) { }

    public DbSet<FlightBooking> FlightBookings => Set<FlightBooking>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureOutboxInbox();

        modelBuilder.Entity<FlightBooking>(builder =>
        {
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Id).ValueGeneratedNever();
            builder.Property(f => f.BookingId).IsRequired();
            builder.Property(f => f.ConfirmationCode).IsRequired();
            builder.Property(f => f.Status).HasConversion<string>();
            builder.ToTable("FlightBookings");
        });
    }
}
