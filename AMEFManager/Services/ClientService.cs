using System.Collections.Generic;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AMEFManager.Services;

public class ClientService : AbstractService<Client>
{
    public ClientService(AppDbContext context) : base(context)
    {
    }

    public override Task<List<Client>> FindAll()
    {
        return _entities.Include(c => c.Address).Include(c => c.Person).ToListAsync();
    }

    public async Task<Client?> FindByNationalIdentifier(string nationalIdentifier)
    {
        if (string.IsNullOrWhiteSpace(nationalIdentifier)) return null;
        var raw = nationalIdentifier.Trim();
        var stripped = raw.StartsWith("RO", System.StringComparison.OrdinalIgnoreCase) ? raw[2..].Trim() : raw;
        var withRo = raw.StartsWith("RO", System.StringComparison.OrdinalIgnoreCase) ? raw : $"RO{raw}";
        return await _entities.FirstOrDefaultAsync(c => c.NationalIdentifier == raw || c.NationalIdentifier == stripped || c.NationalIdentifier == withRo);
    }
    public async Task<Client?> FindByRegistrationNumber(string registrationNumber)
    {
        return await _entities.FirstOrDefaultAsync(c => c.RegistrationNumber == registrationNumber);
    }

    public async Task<bool> IsClientInUseAsync(int clientId)
    {
        return await _context.Contracts.AnyAsync(c => c.ClientId == clientId);
    }
}
