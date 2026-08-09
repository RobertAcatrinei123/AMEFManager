using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;

namespace AMEFManager.Services;

public class BillService : AbstractService<Bill>
{
    public BillService(AppDbContext context) : base(context)
    {
    }

    public async Task<Bill?> FindBySeriesAndNumber(string series, int number)
    {
        return await _entities.FirstOrDefaultAsync(b => b.BillSeries == series && b.BillNumber == number);
    }
}
