using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMEFManager.Services;

public interface IService<T>
{
    public Task<List<T>> FindAll();
    public Task<List<T>> FindAllReadOnly();
    public Task<T?> FindById(int id);
    public Task<T> Add(T entity);
    public Task<T> Update(T entity);
    public Task Delete(T entity);
}