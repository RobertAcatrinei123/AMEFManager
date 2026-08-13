using System.Collections.Generic;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AMEFManager.Services;

public class AdditionalDocumentService : AbstractService<AdditionalDocument>
{
    public AdditionalDocumentService(AppDbContext context) : base(context)
    {
    }

    public override Task<List<AdditionalDocument>> FindAll()
    {
        return _entities
            .Include(d => d.Contract).ThenInclude(c => c.Client).ThenInclude(cl => cl.Address)
            .Include(d => d.Contract).ThenInclude(c => c.Client).ThenInclude(cl => cl.Person)
            .Include(d => d.Contract).ThenInclude(c => c.Type)
            .Include(d => d.Contract).ThenInclude(c => c.Amefs).ThenInclude(a => a.Address)
            .Include(d => d.Contract).ThenInclude(c => c.Amefs).ThenInclude(a => a.Authorization)
            .Include(d => d.Contract).ThenInclude(c => c.Amefs).ThenInclude(a => a.Bill)
            .ToListAsync();
    }

    public async Task<int> GetNextNumberForContractAsync(int contractId)
    {
        var maxNumber = await _entities
            .Where(d => d.ContractId == contractId)
            .MaxAsync(d => (int?)d.Number);
        return (maxNumber ?? 0) + 1;
    }
}
