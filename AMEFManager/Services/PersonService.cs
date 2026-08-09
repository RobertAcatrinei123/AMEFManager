using System.Collections.Generic;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AMEFManager.Services;

public class PersonService : AbstractService<Person>
{
    public PersonService(AppDbContext context) : base(context)
    {
    }

    public override Task<List<Person>> FindAll()
    {
        return _entities.Include(p => p.Address).ToListAsync();
    }

    public Task<Person?> FindBySeriesAndNumber(string series, string number)
    {
        return _entities.Include(p => p.Address).FirstOrDefaultAsync(p => p.Series == series && p.Number == number);
    }

    public async Task<Person?> FindByCnp(string cnp)
    {
        return await _entities.FirstOrDefaultAsync(p => p.Cnp == cnp);
    }
}
