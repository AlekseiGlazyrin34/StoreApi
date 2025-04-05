using System;
using System.Collections.Generic;

namespace TicketApi.Model;

public partial class Group
{
    public int GroupId { get; set; }

    public string GroupName { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
