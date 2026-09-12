using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;

namespace AMEFManager.Services;

public class AuthorizationService : AbstractService<Authorization>
{
    public AuthorizationService(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> IsAuthorizationInUseAsync(int authorizationId)
    {
        return await _context.Amefs.AnyAsync(a => a.AuthorizationId == authorizationId);
    }
}
