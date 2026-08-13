using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AMEFManager.Services;

public class DeliveryDocumentService : AbstractService<DeliveryDocument>
{
    public DeliveryDocumentService(AppDbContext context) : base(context)
    {
    }

    public override async Task<List<DeliveryDocument>> FindAll()
    {
        return await _context.Set<DeliveryDocument>()
            .Include(d => d.Amef)
                .ThenInclude(a => a.Contract)
                    .ThenInclude(c => c.Client)
            .ToListAsync();
    }

    public async Task<DeliveryDocument?> FindByNumber(int number)
    {
        return await _entities.FirstOrDefaultAsync(d => d.Number == number);
    }

    public async Task<int> GetNextNumberAsync()
    {
        var max = await _entities.Select(d => (int?)d.Number).MaxAsync() ?? 0;
        return max + 1;
    }

    public async Task<DeliveryDocument?> FindByAmefIdAsync(int amefId)
    {
        return await _entities
            .Include(d => d.Amef)
                .ThenInclude(a => a.Contract)
                    .ThenInclude(c => c.Client)
            .FirstOrDefaultAsync(d => d.AmefId == amefId);
    }
}
