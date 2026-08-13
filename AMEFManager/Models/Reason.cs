using System;

namespace AMEFManager.Models;

public class Reason
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;

    public override string ToString() => Text;
}
