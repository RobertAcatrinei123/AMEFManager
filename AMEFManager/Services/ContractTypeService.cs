using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;

namespace AMEFManager.Services;

public class ContractTypeService : AbstractService<ContractType>
{
    public ContractTypeService(AppDbContext context) : base(context)
    {
    }

    public async Task<ContractType?> FindByName(string name)
    {
        return await _entities.FirstOrDefaultAsync(c => c.Name == name);
    }
}
