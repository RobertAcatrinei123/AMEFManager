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
}
