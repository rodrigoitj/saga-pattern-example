namespace Hotel.API.Infrastructure.Persistence;

using Hotel.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Configuration;
using Shared.Infrastructure.Messaging.Inbox;
using Shared.Infrastructure.Messaging.Outbox;

public class HotelDbContext : DbContext, IOutboxInboxDbContext
{
    public HotelDbContext(DbContextOptions<HotelDbContext> options)
        : base(options) { }

    public DbSet<HotelBooking> HotelBookings => Set<HotelBooking>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureOutboxInbox();

        modelBuilder.Entity<HotelBooking>(builder =>
        {
            builder.HasKey(h => h.Id);
            builder.Property(h => h.Id).ValueGeneratedNever();
            builder.Property(h => h.BookingId).IsRequired();
            builder.Property(h => h.ConfirmationCode).IsRequired();
            builder.Property(h => h.Status).HasConversion<string>();
            builder.ToTable("HotelBookings");
        });
    }
}
