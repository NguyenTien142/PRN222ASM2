using Services.DataTransferObject.AppointmentDTO;
using Services.DataTransferObject.Common;
using Repositories.Helpper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IAppointmentServices
    {
        Task<ServiceResponse<PagedResult<AppointmentResponse>>> GetAllAppointmentsAsync(AppointmentFilterRequest filter);
        Task<ServiceResponse<AppointmentResponse>> GetAppointmentByIdAsync(int appointmentId);
        Task<ServiceResponse<IEnumerable<AppointmentResponse>>> GetAppointmentsByUserIdAsync(int userId);
        Task<ServiceResponse<AppointmentResponse>> CreateAppointmentAsync(CreateAppointmentRequest request);
        Task<ServiceResponse<AppointmentResponse>> UpdateAppointmentAsync(UpdateAppointmentRequest request);
        Task<ServiceResponse> CancelAppointmentAsync(int appointmentId);
        Task<ServiceResponse> ApproveAppointmentAsync(int appointmentId);
        Task<ServiceResponse> StartAppointmentAsync(int appointmentId);
        Task<ServiceResponse> CompleteAppointmentAsync(int appointmentId);
        Task<ServiceResponse<int>> UpdateExpiredAppointmentsAsync();
    }
}
