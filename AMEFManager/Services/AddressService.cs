using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;

namespace AMEFManager.Services;

public class AddressService : AbstractService<Address>
{
    public AddressService(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> IsAddressInUseAsync(int addressId, int? excludePersonId = null, int? excludeClientId = null, int? excludeAmefId = null)
    {
        var usedByClient = await _context.Clients.AnyAsync(c => c.AddressId == addressId && (!excludeClientId.HasValue || c.Id != excludeClientId.Value));
        if (usedByClient) return true;

        var usedByPerson = await _context.Persons.AnyAsync(p => p.AddressId == addressId && (!excludePersonId.HasValue || p.Id != excludePersonId.Value));
        if (usedByPerson) return true;

        var usedByAmef = await _context.Amefs.AnyAsync(a => a.AddressId == addressId && (!excludeAmefId.HasValue || a.Id != excludeAmefId.Value));
        return usedByAmef;
    }
}
