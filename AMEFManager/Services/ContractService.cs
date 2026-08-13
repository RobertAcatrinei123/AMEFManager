using System.Collections.Generic;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AMEFManager.Services;

public class ContractService : AbstractService<Contract>
{
    public ContractService(AppDbContext context) : base(context)
    {
    }

    public override Task<List<Contract>> FindAll()
    {
        return _entities
            .Include(c => c.Type)
            .Include(c => c.Client).ThenInclude(cl => cl.Address)
            .Include(c => c.Client).ThenInclude(cl => cl.Person)
            .Include(c => c.Amefs).ThenInclude(a => a.Address)
            .Include(c => c.Amefs).ThenInclude(a => a.Authorization)
            .Include(c => c.Amefs).ThenInclude(a => a.Bill)
            .ToListAsync();
    }

    public async Task<Contract?> FindByNumber(int number)
    {
        return await _entities.FirstOrDefaultAsync(c => c.Number == number);
    }

    public async Task<Contract?> FindByNumberAndClientAsync(int number, int clientId)
    {
        return await _entities.FirstOrDefaultAsync(c => c.Number == number && c.ClientId == clientId);
    }
}
