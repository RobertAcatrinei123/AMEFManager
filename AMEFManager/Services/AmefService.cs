using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AMEFManager.Services;

public class AmefService : AbstractService<Amef>
{
    public AmefService(AppDbContext context) : base(context)
    {
    }

    public override Task<List<Amef>> FindAll()
    {
        return _entities
            .Include(a => a.Bill)
            .Include(a => a.Address)
            .Include(a => a.Authorization)
            .Include(a => a.Contract)
                .ThenInclude(c => c!.Client)
                    .ThenInclude(client => client.Address)
            .Include(a => a.Contract)
                .ThenInclude(c => c!.Client)
                    .ThenInclude(client => client.Person)
                        .ThenInclude(person => person.Address)
            .ToListAsync();
    }

    public async Task<Amef?> FindByNui(string nui)
    {
        return await _entities.FirstOrDefaultAsync(a => a.NUI == nui);
    }
    public async Task<Amef?> FindBySeries(string series)
    {
        return await _entities.FirstOrDefaultAsync(a => a.Series == series);
    }

    public async Task<bool> IsAmefInUseAsync(int amefId)
    {
        return await _context.DeliveryDocuments.AnyAsync(d => d.AmefId == amefId)
            || await _context.SealingDocuments.AnyAsync(s => s.AmefId == amefId)
            || await _context.C802Documents.AnyAsync(c => c.Amefs.Any(a => a.Id == amefId));
    }
}
