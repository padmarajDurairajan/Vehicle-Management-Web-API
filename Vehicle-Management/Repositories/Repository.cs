using Microsoft.EntityFrameworkCore;
using VehicleManagementApi.Database;
using VehicleManagementApi.Models;

namespace VehicleManagementApi.Repositories;

public class Repository<T> : IRepository<T> where T : class, IEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<List<T>> GetAllAsync()
        => await _dbSet.AsNoTracking().ToListAsync();

    public virtual async Task<T?> GetByIdAsync(int id)
        => await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public virtual async Task<T> AddAsync(T entity)
    {
        _dbSet.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<bool> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var existing = await _dbSet.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null) return false;

        _dbSet.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}