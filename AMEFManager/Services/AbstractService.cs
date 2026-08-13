using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Helpers;
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
    
    public virtual async Task<List<T>> FindAll()
    {
        AppLogger.LogDebug($"[AbstractService<{typeof(T).Name}>] Querying all entities");
        var list = await _entities.ToListAsync();
        AppLogger.LogDebug($"[AbstractService<{typeof(T).Name}>] Found {list.Count} entity(ies)");
        return list;
    }

    public virtual async Task<List<T>> FindAllReadOnly()
    {
        AppLogger.LogDebug($"[AbstractService<{typeof(T).Name}>] Querying all entities (read-only)");
        var list = await _entities.AsNoTracking().ToListAsync();
        AppLogger.LogDebug($"[AbstractService<{typeof(T).Name}>] Found {list.Count} entity(ies) (read-only)");
        return list;
    }
    
    public virtual async Task<T?> FindById(int id)
    { 
        AppLogger.LogDebug($"[AbstractService<{typeof(T).Name}>] Querying entity by Id: {id}");
        var entity = await _entities.FindAsync(id);
        if (entity == null)
        {
            AppLogger.LogDebug($"[AbstractService<{typeof(T).Name}>] Entity with Id {id} not found");
        }
        return entity;
    }

    public virtual async Task<T> Add(T entity)
    {
        AppLogger.LogInfo($"[AbstractService<{typeof(T).Name}>] Adding entity");
        await _entities.AddAsync(entity);
        return entity;
    }

    public virtual Task<T> Update(T entity)
    {
        AppLogger.LogInfo($"[AbstractService<{typeof(T).Name}>] Updating entity");
        _context.Update(entity);
        return Task.FromResult(entity);
    }

    public virtual Task Delete(T entity)
    {
        AppLogger.LogInfo($"[AbstractService<{typeof(T).Name}>] Marking entity for deletion");
        _context.Remove(entity);
        return Task.CompletedTask;
    }
    
    public virtual async Task SubmitChanges()
    {
        AppLogger.LogDebug($"[AbstractService<{typeof(T).Name}>] Saving changes to database...");
        try
        {
            var written = await _context.SaveChangesAsync();
            AppLogger.LogDebug($"[AbstractService<{typeof(T).Name}>] Database changes saved successfully. Entries written: {written}");
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"[AbstractService<{typeof(T).Name}>] Failed to submit database changes: {ex.Message}", ex);
            throw;
        }
    }
}