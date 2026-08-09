using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using AMEFManager.Data;
using AMEFManager.Models;

namespace AMEFManager.Services;

public class ContractTypeService : AbstractService<ContractType>
{
    public ContractTypeService(AppDbContext context) : base(context)
    {
    }
}
