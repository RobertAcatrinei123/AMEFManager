using System.Collections.Generic;
using System.Threading.Tasks;
using AMEFManager.Data;
using Microsoft.EntityFrameworkCore;

namespace AMEFManager.Services;

public abstract class AbstractService<T> : IService<T> where T : class
{
    protected readonly DbSet<T> _entities;
    protected readonly AppDbContext _context;
    
    protected AbstractService(AppDbContext context)
    {
        _context = context;
        _entities = context.Set<T>();
    }
    
    public virtual Task<List<T>> FindAll()
    {
        return _entities.ToListAsync();
    }

    public virtual Task<List<T>> FindAllReadOnly()
    {
        return _entities.AsNoTracking().ToListAsync();
    }
    
    public virtual Task<T?> FindById(int id)
    { 
        return _entities.FindAsync(id).AsTask();
    }

    public virtual async Task<T> Add(T entity)
    {
        await _entities.AddAsync(entity);
        return entity;
    }

    public virtual Task<T> Update(T entity)
    {
        _context.Update(entity);
        return Task.FromResult(entity);
    }

    public virtual Task Delete(T entity)
    {
        _context.Remove(entity);
        return Task.CompletedTask;
    }
    
    public virtual async Task SubmitChanges()
    {
        await _context.SaveChangesAsync();
    }
}