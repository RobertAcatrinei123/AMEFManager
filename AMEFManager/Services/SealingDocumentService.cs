using System.Collections.Generic;
using System.Linq;
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

    public async Task<int> GetNextNumberAsync()
    {
        var max = await _entities.Select(s => (int?)s.Number).MaxAsync() ?? 0;
        return max + 1;
    }

    public async Task<SealingDocument?> FindByAmefIdAsync(int amefId)
    {
        return await _entities
            .Include(s => s.Amef)
                .ThenInclude(a => a.Contract)
                    .ThenInclude(c => c.Client)
            .FirstOrDefaultAsync(s => s.AmefId == amefId);
    }
}
