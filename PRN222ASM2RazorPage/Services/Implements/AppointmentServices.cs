using AutoMapper;
using Repositories.Interfaces;
using Repositories.Model;
using Services.DataTransferObject.AppointmentDTO;
using Services.DataTransferObject.Common;
using Services.Interfaces;

namespace Services.Implements
{
public class AppointmentServices : IAppointmentServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AppointmentServices(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<AppointmentResponse>> CreateAppointmentAsync(CreateAppointmentRequest request)
        {
            try
            {
                var customerRepository = _unitOfWork.GetRepository<Customer, int>();
                var customer = await customerRepository.GetByIdAsync(request.CustomerId);
                if (customer == null)
                {
                    return new ServiceResponse<AppointmentResponse>
                    {
                        Success = false,
                        Message = "Customer not found",
                        Data = null
                    };
                }

                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();
                var vehicle = await vehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null || vehicle.IsDeleted)
                {
                    return new ServiceResponse<AppointmentResponse>
                    {
                        Success = false,
                        Message = "Vehicle not found or deleted",
                        Data = null
                    };
                }

                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();
                var existingAppointment = await appointmentRepository.FirstOrDefaultAsync(a => 
                    a.CustomerId == request.CustomerId && 
                    a.VehicleId == request.VehicleId && 
                    a.AppointmentDate == request.AppointmentDate);

                if (existingAppointment != null)
                {
                    return new ServiceResponse<AppointmentResponse>
                    {
                        Success = false,
                        Message = "Appointment already exists for this customer, vehicle and time slot",
                        Data = null
                    };
                }

                var appointment = _mapper.Map<Appointment>(request);
                var createdAppointment = await appointmentRepository.AddAsync(appointment);

                var appointmentWithDetails = await appointmentRepository.GetByIdAsync(
                    createdAppointment.Id, 
                    a => a.Customer, 
                    a => a.Vehicle
                );

                var appointmentResponse = _mapper.Map<AppointmentResponse>(appointmentWithDetails);

                return new ServiceResponse<AppointmentResponse>
                {
                    Success = true,
                    Message = "Appointment created successfully",
                    Data = appointmentResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<AppointmentResponse>
                {
                    Success = false,
                    Message = $"Error creating appointment: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<IEnumerable<AppointmentResponse>>> GetAppointmentsByCustomerAsync(int customerId)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();
                var appointments = await appointmentRepository.GetAllAsync(
                    predicate: a => a.CustomerId == customerId,
                    orderBy: q => q.OrderByDescending(a => a.AppointmentDate),
                    a => a.Customer, a => a.Vehicle
                );

                var appointmentResponses = _mapper.Map<IEnumerable<AppointmentResponse>>(appointments);

                return new ServiceResponse<IEnumerable<AppointmentResponse>>
                {
                    Success = true,
                    Message = "Appointments retrieved successfully",
                    Data = appointmentResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<IEnumerable<AppointmentResponse>>
                {
                    Success = false,
                    Message = $"Error retrieving appointments: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<IEnumerable<AppointmentResponse>>> GetAllAppointmentsAsync(bool includeDeleted = false)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();
                var appointments = await appointmentRepository.GetAllAsync(
                    predicate: includeDeleted ? null : a => true,
                    orderBy: q => q.OrderByDescending(a => a.AppointmentDate),
                    a => a.Customer, a => a.Vehicle
                );

                var appointmentResponses = _mapper.Map<IEnumerable<AppointmentResponse>>(appointments);

                return new ServiceResponse<IEnumerable<AppointmentResponse>>
                {
                    Success = true,
                    Message = "All appointments retrieved successfully",
                    Data = appointmentResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<IEnumerable<AppointmentResponse>>
                {
                    Success = false,
                    Message = $"Error retrieving all appointments: {ex.Message}",
                    Data = null
                };
            }
        }
    }
}
