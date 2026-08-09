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
}
