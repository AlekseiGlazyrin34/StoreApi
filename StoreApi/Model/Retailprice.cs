using System;
using System.Collections.Generic;

namespace TicketApi.Model;

public partial class Retailprice
{
    public int PriceId { get; set; }

    public int ProductId { get; set; }

    public decimal Price { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public virtual Product Product { get; set; } = null!;
}
