using System;
using System.Collections.Generic;

namespace Repositories.Model;

public partial class VehicleCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
