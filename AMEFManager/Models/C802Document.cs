using System.Collections.Generic;

namespace AMEFManager.Models;

public class C802Document
{
    public int Id { get; set; }
    public long Number { get; set; }
    
    public ICollection<Amef> Amefs { get; set; } = new List<Amef>();
}
