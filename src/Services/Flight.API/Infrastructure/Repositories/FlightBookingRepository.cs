namespace Flight.API.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Flight.API.Domain.Entities;
using Flight.API.Infrastructure.Persistence;
using Shared.Domain.Abstractions;

public class FlightBookingRepository : IRepository<FlightBooking>
{
    private readonly FlightDbContext _context;

    public FlightBookingRepository(FlightDbContext context) => _context = context;

    public async Task<FlightBooking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.FlightBookings.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<List<FlightBooking>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.FlightBookings.ToListAsync(cancellationToken);

    public async Task AddAsync(FlightBooking entity, CancellationToken cancellationToken = default)
    {
        await _context.FlightBookings.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(FlightBooking entity, CancellationToken cancellationToken = default)
    {
        _context.FlightBookings.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(FlightBooking entity, CancellationToken cancellationToken = default)
    {
        _context.FlightBookings.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}
