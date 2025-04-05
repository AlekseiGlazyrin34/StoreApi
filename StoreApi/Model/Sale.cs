using System;
using System.Collections.Generic;

namespace TicketApi.Model;

public partial class Sale
{
    public int SaleId { get; set; }

    public int ClientId { get; set; }

    public int StoreId { get; set; }

    public DateTime SaleDate { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal TotalAmount { get; set; }

    public string DeliveryAddress { get; set; } = null!;

    public DateTime DeliveryDate { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<Saleitem> Saleitems { get; set; } = new List<Saleitem>();

    public virtual Store Store { get; set; } = null!;
}
