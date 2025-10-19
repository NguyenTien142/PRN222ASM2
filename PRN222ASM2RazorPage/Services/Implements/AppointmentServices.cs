using AutoMapper;
using Repositories.Interfaces;
using Repositories.Model;
using Services.DataTransferObject.AppointmentDTO;
using Services.DataTransferObject.Common;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Helpper;
using System.Linq.Expressions;

namespace Services.Implements
{
    public class AppointmentServices : IAppointmentServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        // Appointment status constants
        private const string STATUS_PENDING = "PENDING";
        private const string STATUS_APPROVE = "APPROVE";
        private const string STATUS_CANCELLED = "CANCELLED";
        private const string STATUS_RUNNING = "RUNNING";
        private const string STATUS_COMPLETED = "COMPLETED";
        private const string STATUS_EXPIRED = "EXPIRED";

        public AppointmentServices(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<PagedResult<AppointmentResponse>>> GetAllAppointmentsAsync(AppointmentFilterRequest filter)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();

                // Update expired appointments before retrieving
                await UpdateExpiredAppointmentsAsync();

                // Build predicate based on filter
                System.Linq.Expressions.Expression<Func<Appointment, bool>>? predicate = null;

                if (filter.CustomerId.HasValue || filter.VehicleId.HasValue || !string.IsNullOrEmpty(filter.Status) || filter.StartDate.HasValue || filter.EndDate.HasValue)
                {
                    predicate = a =>
                        (!filter.CustomerId.HasValue || a.CustomerId == filter.CustomerId.Value) &&
                        (!filter.VehicleId.HasValue || a.VehicleId == filter.VehicleId.Value) &&
                        (string.IsNullOrEmpty(filter.Status) || a.Status == filter.Status) &&
                        (!filter.StartDate.HasValue || a.AppointmentDate >= filter.StartDate.Value) &&
                        (!filter.EndDate.HasValue || a.AppointmentDate <= filter.EndDate.Value);
                }

                // Get all appointments with necessary includes
                var appointments = await appointmentRepository.GetAllAsync(
                    predicate,
                    q => q.OrderByDescending(a => a.AppointmentDate),
                    a => a.Customer,
                    a => a.Vehicle,
                    a => a.Vehicle.Category
                );

                // Apply pagination manually
                var totalCount = appointments.Count;
                var pagedItems = appointments.Skip(filter.PageIndex * filter.PageSize)
                                           .Take(filter.PageSize)
                                           .ToList();

                var appointmentResponses = await MapToAppointmentResponsesAsync(pagedItems);

                var pagedResult = new PagedResult<AppointmentResponse>(
                    appointmentResponses,
                    totalCount,
                    filter.PageIndex + 1, // PagedResult expects 1-based page number
                    filter.PageSize
                );

                return new ServiceResponse<PagedResult<AppointmentResponse>>
                {
                    Success = true,
                    Message = "Appointments retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<PagedResult<AppointmentResponse>>
                {
                    Success = false,
                    Message = $"Error retrieving appointments: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<AppointmentResponse>> GetAppointmentByIdAsync(int appointmentId)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();

                // Update expired appointments before retrieving
                await UpdateExpiredAppointmentsAsync();

                var appointment = await appointmentRepository.GetByIdAsync(
                    appointmentId,
                    a => a.Customer,
                    a => a.Vehicle,
                    a => a.Vehicle.Category
                );

                if (appointment == null)
                {
                    return new ServiceResponse<AppointmentResponse>
                    {
                        Success = false,
                        Message = "Appointment not found",
                        Data = null
                    };
                }

                var appointmentResponse = await MapToAppointmentResponseAsync(appointment);

                return new ServiceResponse<AppointmentResponse>
                {
                    Success = true,
                    Message = "Appointment retrieved successfully",
                    Data = appointmentResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<AppointmentResponse>
                {
                    Success = false,
                    Message = $"Error retrieving appointment: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<IEnumerable<AppointmentResponse>>> GetAppointmentsByUserIdAsync(int userId)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();
                var customerRepository = _unitOfWork.GetRepository<Customer, int>();

                // Update expired appointments before retrieving
                await UpdateExpiredAppointmentsAsync();

                // Find customer by user ID
                var customer = await customerRepository.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null)
                {
                    return new ServiceResponse<IEnumerable<AppointmentResponse>>
                    {
                        Success = false,
                        Message = "Customer not found for this user",
                        Data = new List<AppointmentResponse>()
                    };
                }

                var appointments = await appointmentRepository.GetAllAsync(
                    a => a.CustomerId == customer.Id,
                    q => q.OrderByDescending(a => a.AppointmentDate),
                    a => a.Customer,
                    a => a.Vehicle,
                    a => a.Vehicle.Category
                );

                var appointmentResponses = await MapToAppointmentResponsesAsync(appointments);

                return new ServiceResponse<IEnumerable<AppointmentResponse>>
                {
                    Success = true,
                    Message = "User appointments retrieved successfully",
                    Data = appointmentResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<IEnumerable<AppointmentResponse>>
                {
                    Success = false,
                    Message = $"Error retrieving user appointments: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<AppointmentResponse>> CreateAppointmentAsync(CreateAppointmentRequest request)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();
                var customerRepository = _unitOfWork.GetRepository<Customer, int>();
                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();

                // Validate customer exists
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

                // Validate vehicle exists and is not deleted
                var vehicle = await vehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null || vehicle.IsDeleted)
                {
                    return new ServiceResponse<AppointmentResponse>
                    {
                        Success = false,
                        Message = "Vehicle not found or is no longer available",
                        Data = null
                    };
                }

                // Validate appointment date is in the future
                if (request.AppointmentDate <= DateTime.Now)
                {
                    return new ServiceResponse<AppointmentResponse>
                    {
                        Success = false,
                        Message = "Appointment date must be in the future",
                        Data = null
                    };
                }

                // Check for existing pending appointment for the same customer and vehicle
                var existingAppointment = await appointmentRepository.FirstOrDefaultAsync(
                    a => a.CustomerId == request.CustomerId &&
                         a.VehicleId == request.VehicleId &&
                         a.Status == STATUS_PENDING);

                if (existingAppointment != null)
                {
                    return new ServiceResponse<AppointmentResponse>
                    {
                        Success = false,
                        Message = "You already have a pending appointment for this vehicle",
                        Data = null
                    };
                }

                var appointment = new Appointment
                {
                    CustomerId = request.CustomerId,
                    VehicleId = request.VehicleId,
                    AppointmentDate = request.AppointmentDate,
                    Status = STATUS_PENDING
                };

                var createdAppointment = await appointmentRepository.AddAsync(appointment);
                await _unitOfWork.SaveChangesAsync();

                // Get the created appointment with includes
                var appointmentWithIncludes = await appointmentRepository.GetByIdAsync(
                    createdAppointment.Id,
                    a => a.Customer,
                    a => a.Vehicle,
                    a => a.Vehicle.Category
                );

                var appointmentResponse = await MapToAppointmentResponseAsync(appointmentWithIncludes!);

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

        public async Task<ServiceResponse<AppointmentResponse>> UpdateAppointmentAsync(UpdateAppointmentRequest request)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();

                // Update expired appointments first
                await UpdateExpiredAppointmentsAsync();

                var appointment = await appointmentRepository.GetByIdAsync(request.Id);

                if (appointment == null)
                {
                    return new ServiceResponse<AppointmentResponse>
                    {
                        Success = false,
                        Message = "Appointment not found",
                        Data = null
                    };
                }

                // Check if appointment can be updated (only PENDING appointments can be updated)
                if (appointment.Status != STATUS_PENDING)
                {
                    return new ServiceResponse<AppointmentResponse>
                    {
                        Success = false,
                        Message = "Only pending appointments can be updated",
                        Data = null
                    };
                }

                // Validate appointment date is in the future
                if (request.AppointmentDate <= DateTime.Now)
                {
                    return new ServiceResponse<AppointmentResponse>
                    {
                        Success = false,
                        Message = "Appointment date must be in the future",
                        Data = null
                    };
                }

                appointment.AppointmentDate = request.AppointmentDate;
                await appointmentRepository.UpdateAsync(appointment);
                await _unitOfWork.SaveChangesAsync();

                // Get updated appointment with includes
                var updatedAppointment = await appointmentRepository.GetByIdAsync(
                    appointment.Id,
                    a => a.Customer,
                    a => a.Vehicle,
                    a => a.Vehicle.Category
                );

                var appointmentResponse = await MapToAppointmentResponseAsync(updatedAppointment!);

                return new ServiceResponse<AppointmentResponse>
                {
                    Success = true,
                    Message = "Appointment updated successfully",
                    Data = appointmentResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<AppointmentResponse>
                {
                    Success = false,
                    Message = $"Error updating appointment: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse> CancelAppointmentAsync(int appointmentId)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();
                var appointment = await appointmentRepository.GetByIdAsync(appointmentId);

                if (appointment == null)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Appointment not found"
                    };
                }

                // Check if appointment can be cancelled (only PENDING appointments can be cancelled)
                if (appointment.Status != STATUS_PENDING)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Only pending appointments can be cancelled"
                    };
                }

                appointment.Status = STATUS_CANCELLED;
                await appointmentRepository.UpdateAsync(appointment);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResponse
                {
                    Success = true,
                    Message = "Appointment cancelled successfully"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Success = false,
                    Message = $"Error cancelling appointment: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse> ApproveAppointmentAsync(int appointmentId)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();
                var appointment = await appointmentRepository.GetByIdAsync(appointmentId);

                if (appointment == null)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Appointment not found"
                    };
                }

                // Check if appointment can be approved (only PENDING appointments can be approved)
                if (appointment.Status != STATUS_PENDING)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Only pending appointments can be approved"
                    };
                }

                appointment.Status = STATUS_APPROVE;
                await appointmentRepository.UpdateAsync(appointment);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResponse
                {
                    Success = true,
                    Message = "Appointment approved successfully"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Success = false,
                    Message = $"Error approving appointment: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse> StartAppointmentAsync(int appointmentId)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();
                var appointment = await appointmentRepository.GetByIdAsync(appointmentId);

                if (appointment == null)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Appointment not found"
                    };
                }

                // Check if appointment can be started (only APPROVED appointments can be started)
                if (appointment.Status != STATUS_APPROVE)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Only approved appointments can be started"
                    };
                }

                // Check if appointment is within the allowed time range (can start up to 30 minutes before appointment time)
                var currentTime = DateTime.Now;
                var appointmentTime = appointment.AppointmentDate;
                var startWindow = appointmentTime.AddMinutes(-30); // Allow starting 30 minutes early
                var endWindow = appointmentTime.AddHours(2); // Allow starting up to 2 hours after appointment time

                if (currentTime < startWindow)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = $"Appointment can only be started from {startWindow:HH:mm} onwards"
                    };
                }

                if (currentTime > endWindow)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Appointment start window has expired"
                    };
                }

                appointment.Status = STATUS_RUNNING;
                await appointmentRepository.UpdateAsync(appointment);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResponse
                {
                    Success = true,
                    Message = "Appointment started successfully"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Success = false,
                    Message = $"Error starting appointment: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse> CompleteAppointmentAsync(int appointmentId)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();
                var appointment = await appointmentRepository.GetByIdAsync(appointmentId);

                if (appointment == null)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Appointment not found"
                    };
                }

                // Check if appointment can be completed (only RUNNING appointments can be completed)
                if (appointment.Status != STATUS_RUNNING)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Only running appointments can be completed"
                    };
                }

                appointment.Status = STATUS_COMPLETED;
                await appointmentRepository.UpdateAsync(appointment);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResponse
                {
                    Success = true,
                    Message = "Appointment completed successfully"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Success = false,
                    Message = $"Error completing appointment: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Automatically checks and updates appointments that have passed their appointment date
        /// Only APPROVED appointments are automatically set to EXPIRED when past their date
        /// </summary>
        public async Task<ServiceResponse<int>> UpdateExpiredAppointmentsAsync()
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();

                // Get all APPROVED appointments that have passed their appointment date
                var expiredAppointments = await appointmentRepository.GetAllAsync(
                    a => a.Status == STATUS_APPROVE && a.AppointmentDate < DateTime.Now
                );

                if (!expiredAppointments.Any())
                {
                    return new ServiceResponse<int>
                    {
                        Success = true,
                        Message = "No appointments to expire",
                        Data = 0
                    };
                }

                // Update all expired appointments to EXPIRED status
                var expiredCount = 0;
                foreach (var appointment in expiredAppointments)
                {
                    appointment.Status = STATUS_EXPIRED;
                    await appointmentRepository.UpdateAsync(appointment);
                    expiredCount++;
                }

                await _unitOfWork.SaveChangesAsync();

                return new ServiceResponse<int>
                {
                    Success = true,
                    Message = $"Updated {expiredCount} appointments to expired status",
                    Data = expiredCount
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<int>
                {
                    Success = false,
                    Message = $"Error updating expired appointments: {ex.Message}",
                    Data = 0
                };
            }
        }

        /// <summary>
        /// Checks if an individual appointment should be expired based on current date/time
        /// Only APPROVED appointments are considered for expiration
        /// </summary>
        /// <param name="appointment">The appointment to check</param>
        /// <returns>True if the appointment should be expired</returns>
        private bool ShouldExpireAppointment(Appointment appointment)
        {
            return appointment.Status == STATUS_APPROVE && appointment.AppointmentDate < DateTime.Now;
        }

        #region Private Helper Methods

        private async Task<AppointmentResponse> MapToAppointmentResponseAsync(Appointment appointment)
        {
            // Check if this specific appointment should be expired and update it
            if (ShouldExpireAppointment(appointment))
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();
                appointment.Status = STATUS_EXPIRED;
                await appointmentRepository.UpdateAsync(appointment);
                await _unitOfWork.SaveChangesAsync();
            }

            var categoryRepository = _unitOfWork.GetRepository<VehicleCategory, int>();
            var category = await categoryRepository.GetByIdAsync(appointment.Vehicle.CategoryId);

            // Get customer's email from User entity with null checks
            string customerEmail = "N/A";
            try
            {
                if (appointment.Customer != null)
                {
                    var userRepository = _unitOfWork.GetRepository<User, int>();
                    var customerUser = await userRepository.GetByIdAsync(appointment.Customer.UserId);
                    customerEmail = customerUser?.Email ?? "N/A";
                }
            }
            catch (Exception)
            {
                // If any error occurs getting email, use N/A
                customerEmail = "N/A";
            }

            return new AppointmentResponse
            {
                Id = appointment.Id,
                CustomerId = appointment.CustomerId,
                VehicleId = appointment.VehicleId,
                AppointmentDate = appointment.AppointmentDate,
                Status = appointment.Status,
                CustomerName = appointment.Customer?.Name ?? "Unknown",
                CustomerPhone = appointment.Customer?.Phone ?? "N/A",
                CustomerAddress = appointment.Customer?.Address ?? "N/A",
                CustomerEmail = customerEmail,
                VehicleModel = appointment.Vehicle?.Model ?? "Unknown",
                VehicleColor = appointment.Vehicle?.Color ?? "Unknown",
                VehicleVersion = appointment.Vehicle?.Version,
                VehiclePrice = appointment.Vehicle?.Price ?? 0,
                VehicleCategoryName = category?.Name ?? "Unknown",
                VehicleImage = appointment.Vehicle?.Image
            };
        }

        private async Task<List<AppointmentResponse>> MapToAppointmentResponsesAsync(IEnumerable<Appointment> appointments)
        {
            var responses = new List<AppointmentResponse>();

            foreach (var appointment in appointments)
            {
                var response = await MapToAppointmentResponseAsync(appointment);
                responses.Add(response);
            }

            return responses;
        }

        #endregion
    }
}
