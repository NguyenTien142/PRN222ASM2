namespace Services.DataTransferObject.AppointmentDTO;

public class AppointmentResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public string VehicleModel { get; set; } = string.Empty;
    public string VehicleVersion { get; set; } = string.Empty;
    public string VehicleColor { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}