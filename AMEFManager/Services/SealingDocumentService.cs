using System.Collections.Generic;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AMEFManager.Services;

public class SealingDocumentService : AbstractService<SealingDocument>
{
    public SealingDocumentService(AppDbContext context) : base(context)
    {
    }

    public override Task<List<SealingDocument>> FindAll()
    {
        return _entities
            .Include(s => s.Amef)
                .ThenInclude(a => a.Contract)
                    .ThenInclude(c => c.Client)
            .ToListAsync();
    }

    public async Task<SealingDocument?> FindByNumber(int number)
    {
        return await _entities.FirstOrDefaultAsync(s => s.Number == number);
    }
}
