namespace Booking.API.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Booking.API.Domain.Entities;
using Booking.API.Infrastructure.Persistence;
using Shared.Domain.Abstractions;

/// <summary>
/// Repository for Booking aggregate.
/// Implements Repository Pattern with EF Core.
/// </summary>
public class BookingRepository : IRepository<Booking>
{
    private readonly BookingDbContext _context;

    public BookingRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Include(b => b.Steps)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<List<Booking>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Include(b => b.Steps)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Booking entity, CancellationToken cancellationToken = default)
    {
        await _context.Bookings.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(Booking entity, CancellationToken cancellationToken = default)
    {
        _context.Bookings.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Booking entity, CancellationToken cancellationToken = default)
    {
        _context.Bookings.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
