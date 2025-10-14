using System;
using System.Collections.Generic;

namespace Repositories.Model;

public partial class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int DealerId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual Dealer Dealer { get; set; } = null!;

    public virtual ICollection<OrderVehicle> OrderVehicles { get; set; } = new List<OrderVehicle>();
}
