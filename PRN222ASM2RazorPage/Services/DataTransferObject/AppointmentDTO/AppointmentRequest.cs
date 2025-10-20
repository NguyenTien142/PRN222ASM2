namespace Services.DataTransferObject.AppointmentDTO;

public class CreateAppointmentRequest
{
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = "Pending";
}

public class UpdateAppointmentRequest
{
    public int Id { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public string? Status { get; set; }
}