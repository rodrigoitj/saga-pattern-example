namespace Hotel.API.Infrastructure.Persistence;

using Hotel.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class HotelDbContext : DbContext
{
    public HotelDbContext(DbContextOptions<HotelDbContext> options)
        : base(options) { }

    public DbSet<HotelBooking> HotelBookings => Set<HotelBooking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
