namespace Hotel.API.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Hotel.API.Domain.Entities;
using Hotel.API.Infrastructure.Persistence;
using Shared.Domain.Abstractions;

public class HotelBookingRepository : IRepository<HotelBooking>
{
    private readonly HotelDbContext _context;

    public HotelBookingRepository(HotelDbContext context) => _context = context;

    public async Task<HotelBooking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.HotelBookings.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<List<HotelBooking>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.HotelBookings.ToListAsync(cancellationToken);

    public async Task AddAsync(HotelBooking entity, CancellationToken cancellationToken = default)
    {
        await _context.HotelBookings.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(HotelBooking entity, CancellationToken cancellationToken = default)
    {
        _context.HotelBookings.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(HotelBooking entity, CancellationToken cancellationToken = default)
    {
        _context.HotelBookings.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}
