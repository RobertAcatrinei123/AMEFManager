using System.Collections.Generic;
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
}
