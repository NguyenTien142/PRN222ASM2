using System;
using System.Collections.Generic;

namespace Repositories.Model;

public partial class OrderVehicle
{
    public int OrderId { get; set; }

    public int VehicleId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
