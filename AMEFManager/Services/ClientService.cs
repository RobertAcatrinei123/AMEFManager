using System.Collections.Generic;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AMEFManager.Services;

public class ClientService : AbstractService<Client>
{
    public ClientService(AppDbContext context) : base(context)
    {
    }

    public override Task<List<Client>> FindAll()
    {
        return _entities.Include(c => c.Address).Include(c => c.Person).ToListAsync();
    }

    public async Task<Client?> FindByNationalIdentifier(string nationalIdentifier)
    {
        return await _entities.FirstOrDefaultAsync(c => c.NationalIdentifier == nationalIdentifier);
    }
    public async Task<Client?> FindByRegistrationNumber(string registrationNumber)
    {
        return await _entities.FirstOrDefaultAsync(c => c.RegistrationNumber == registrationNumber);
    }
}
