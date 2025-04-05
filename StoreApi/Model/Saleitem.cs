using System;
using System.Collections.Generic;

namespace TicketApi.Model;

public partial class Saleitem
{
    public int SaleItemId { get; set; }

    public int ProductId { get; set; }

    public int SaleId { get; set; }

    public int Quantity { get; set; }

    public decimal Total { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Sale Sale { get; set; } = null!;
}
