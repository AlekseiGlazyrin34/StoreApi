using System;
using System.Collections.Generic;

namespace TicketApi.Model;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string ContactInfo { get; set; } = null!;

    public int StoreId { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshtokenExpiredtime { get; set; }

    public virtual Store Store { get; set; } = null!;
}
