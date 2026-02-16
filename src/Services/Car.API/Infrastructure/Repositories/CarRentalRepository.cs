namespace Car.API.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Car.API.Domain.Entities;
using Car.API.Infrastructure.Persistence;
using Shared.Domain.Abstractions;

public class CarRentalRepository : IRepository<CarRental>
{
    private readonly CarDbContext _context;

    public CarRentalRepository(CarDbContext context) => _context = context;

    public async Task<CarRental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.CarRentals.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<List<CarRental>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.CarRentals.ToListAsync(cancellationToken);

    public async Task AddAsync(CarRental entity, CancellationToken cancellationToken = default)
    {
        await _context.CarRentals.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(CarRental entity, CancellationToken cancellationToken = default)
    {
        _context.CarRentals.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(CarRental entity, CancellationToken cancellationToken = default)
    {
        _context.CarRentals.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}
