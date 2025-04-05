using System;
using System.Collections.Generic;

namespace TicketApi.Model;

public partial class Partner
{
    public int PartnerId { get; set; }

    public string PartnerName { get; set; } = null!;

    public string ContactInfo { get; set; } = null!;
}
