using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AMEFManager.Data;
using AMEFManager.Models;

namespace AMEFManager.Services;

public class C802DocumentService : AbstractService<C802Document>
{
    public C802DocumentService(AppDbContext context) : base(context)
    {
    }

    public override Task<List<C802Document>> FindAll()
    {
        return _context.C802Documents.Include(d => d.Amefs).ToListAsync();
    }

    public async Task<long> GetNextNumberAsync()
    {
        if (await _context.C802Documents.AnyAsync())
        {
            var maxNumber = await _context.C802Documents.MaxAsync(d => (long?)d.Number);
            return (maxNumber ?? 0) + 1;
        }
        return 1;
    }
}
