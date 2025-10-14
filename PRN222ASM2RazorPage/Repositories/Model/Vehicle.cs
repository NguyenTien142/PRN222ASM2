using System;
using System.Collections.Generic;

namespace Repositories.Model;

public partial class Vehicle
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string Color { get; set; } = null!;

    public decimal Price { get; set; }

    public DateOnly ManufactureDate { get; set; }

    public string Model { get; set; } = null!;

    public string? Version { get; set; }

    public string? Image { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual VehicleCategory Category { get; set; } = null!;

    public virtual ICollection<OrderVehicle> OrderVehicles { get; set; } = new List<OrderVehicle>();

    public virtual ICollection<VehicleDealer> VehicleDealers { get; set; } = new List<VehicleDealer>();
}
