using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AMEFManager.Data;
using AMEFManager.Models;

namespace AMEFManager.Services;

public class ReasonService : AbstractService<Reason>
{
    public ReasonService(AppDbContext context) : base(context)
    {
    }

    public async Task<Reason?> FindByText(string text)
    {
        return await _entities.FirstOrDefaultAsync(r => r.Text == text);
    }
}
