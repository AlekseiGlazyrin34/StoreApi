using System;
using System.Collections.Generic;

namespace TicketApi.Model;

public partial class Store
{
    public int StoreId { get; set; }

    public string StoreAddress { get; set; } = null!;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
