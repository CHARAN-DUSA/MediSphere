using System.Linq.Expressions;
using MediSphere.Domain.Common;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediSphere.Infrastructure.Persistence;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);
    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => await _dbSet.Where(predicate).ToListAsync();
    public async Task<T> AddAsync(T entity) { var e = await _dbSet.AddAsync(entity); return e.Entity; }
    public Task UpdateAsync(T entity) { _dbSet.Update(entity); return Task.CompletedTask; }
    public Task DeleteAsync(T entity) { _dbSet.Remove(entity); return Task.CompletedTask; }
    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) => await _dbSet.AnyAsync(predicate);
    public IQueryable<T> Query() => _dbSet.AsQueryable();
}
