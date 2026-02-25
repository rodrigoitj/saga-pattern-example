namespace Car.API.Infrastructure.Persistence;

using Car.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class CarDbContext : DbContext
{
    public CarDbContext(DbContextOptions<CarDbContext> options)
        : base(options) { }

    public DbSet<CarRental> CarRentals => Set<CarRental>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
