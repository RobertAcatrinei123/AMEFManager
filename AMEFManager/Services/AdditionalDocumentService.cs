using System.Collections.Generic;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AMEFManager.Services;

public class AdditionalDocumentService : AbstractService<AdditionalDocument>
{
    public AdditionalDocumentService(AppDbContext context) : base(context)
    {
    }

    public override Task<List<AdditionalDocument>> FindAll()
    {
        return _entities.Include(d => d.Client).ToListAsync();
    }
}
