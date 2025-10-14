using System;
using System.Collections.Generic;

namespace Repositories.Model;

public partial class Dealer
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string DealerName { get; set; } = null!;

    public string Address { get; set; } = null!;

    public int Quantity { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<VehicleDealer> VehicleDealers { get; set; } = new List<VehicleDealer>();
}
