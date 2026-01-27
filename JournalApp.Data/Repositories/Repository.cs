using JournalApp.Core.Entities;
using JournalApp.Core.Interfaces;
using JournalApp.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace JournalApp.Data.Repositories;

/// <summary>
/// Generic repository implementation for common CRUD operations.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly JournalDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(JournalDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await Task.CompletedTask;
    }

    public virtual async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
