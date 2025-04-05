using System;
using System.Collections.Generic;

namespace TicketApi.Model;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string ProductBrand { get; set; } = null!;

    public int GroupId { get; set; }

    public byte[]? ProductImage { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual ICollection<Retailprice> Retailprices { get; set; } = new List<Retailprice>();

    public virtual ICollection<Saleitem> Saleitems { get; set; } = new List<Saleitem>();
}
