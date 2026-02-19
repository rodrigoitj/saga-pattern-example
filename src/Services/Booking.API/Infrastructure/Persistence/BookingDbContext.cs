namespace Booking.API.Infrastructure.Persistence;

using Booking.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;

public class BookingDbContext : BaseApplicationDbContext
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Booking>(builder =>
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.Id).ValueGeneratedNever();
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
