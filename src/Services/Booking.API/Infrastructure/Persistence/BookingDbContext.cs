namespace Booking.API.Infrastructure.Persistence;

using Booking.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Configuration;
using Shared.Infrastructure.Messaging.Inbox;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Persistence;

public class BookingDbContext : BaseApplicationDbContext, IOutboxInboxDbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options)
        : base(options) { }

    public DbSet<Booking> Bookings
    {
        get { return Set<Booking>(); }
    }

    public DbSet<BookingStep> BookingSteps
    {
        get { return Set<BookingStep>(); }
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureOutboxInbox();

        modelBuilder.Entity<Booking>(builder =>
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.Id).ValueGeneratedNever();
            builder.Property(b => b.Version).IsRowVersion();
            builder.Property(b => b.ReferenceNumber).IsRequired();
            builder.Property(b => b.Status).HasConversion<string>();
            builder.Property(b => b.CreatedAt).IsRequired();
            builder.Property(b => b.UpdatedAt).IsRequired(false);
            builder
                .HasMany(b => b.Steps)
                .WithOne(bs => bs.Booking)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.ToTable("Bookings");
        });

        modelBuilder.Entity<BookingStep>(builder =>
        {
            builder.HasKey(bs => bs.Id);
            builder.Property(bs => bs.Id).ValueGeneratedOnAdd();
            builder.Property(bs => bs.BookingId).IsRequired();
            builder.Property(bs => bs.StepType).HasConversion<string>();
            builder.Property(bs => bs.Status).HasConversion<string>();
            builder.ToTable("BookingSteps");
        });
    }
}
