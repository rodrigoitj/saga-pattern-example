namespace Flight.API.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Flight.API.Domain.Entities;

public class FlightDbContext : DbContext
{
    public FlightDbContext(DbContextOptions<FlightDbContext> options) : base(options) { }

    public DbSet<FlightBooking> FlightBookings => Set<FlightBooking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FlightBooking>(builder =>
        {
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Id).ValueGeneratedNever();
            builder.Property(f => f.ConfirmationCode).IsRequired();
            builder.Property(f => f.Status).HasConversion<string>();
            builder.ToTable("FlightBookings");
        });
    }
}
