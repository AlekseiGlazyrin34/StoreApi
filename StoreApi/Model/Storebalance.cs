using System;
using System.Collections.Generic;

namespace TicketApi.Model;

public partial class Storebalance
{
    public int StoreId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Store Store { get; set; } = null!;
}
