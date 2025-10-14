using System;
using System.Collections.Generic;

namespace Repositories.Model;

public partial class VehicleDealer
{
    public int VehicleId { get; set; }

    public int DealerId { get; set; }

    public int Quantity { get; set; }

    public virtual Dealer Dealer { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
