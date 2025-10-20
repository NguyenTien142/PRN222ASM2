using Services.DataTransferObject.AppointmentDTO;
using Services.DataTransferObject.Common;

namespace Services.Interfaces
{
    public interface IAppointmentServices
    {
        Task<ServiceResponse<AppointmentResponse>> CreateAppointmentAsync(CreateAppointmentRequest request);
        Task<ServiceResponse<IEnumerable<AppointmentResponse>>> GetAppointmentsByCustomerAsync(int customerId);
        Task<ServiceResponse<IEnumerable<AppointmentResponse>>> GetAllAppointmentsAsync(bool includeDeleted = false);
    }
}
