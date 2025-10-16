using AutoMapper;
using Repositories.Helpper;
using Repositories.Interfaces;
using Repositories.Model;
using Services.DataTransferObject.Common;
using Services.DataTransferObject.VehicleDTO;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements
{
    public class VehicleServices : IVehicleServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public VehicleServices(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<IEnumerable<VehicleResponse>>> GetAllVehiclesAsync(bool includeDeleted = false)
        {
            try
            {
                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();
                var vehicles = await vehicleRepository.GetAllAsync(
                    predicate: includeDeleted ? null : v => !v.IsDeleted,
                    orderBy: q => q.OrderBy(v => v.Model),
                    includes: v => v.Category
                );

                var vehicleResponses = _mapper.Map<IEnumerable<VehicleResponse>>(vehicles);

                return new ServiceResponse<IEnumerable<VehicleResponse>>
                {
                    Success = true,
                    Message = "Vehicles retrieved successfully",
                    Data = vehicleResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<IEnumerable<VehicleResponse>>
                {
                    Success = false,
                    Message = $"Error retrieving vehicles: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<VehicleResponse>> GetVehicleByIdAsync(int id)
        {
            try
            {
                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();
                var vehicle = await vehicleRepository.GetByIdAsync(id, v => v.Category);

                if (vehicle == null)
                {
                    return new ServiceResponse<VehicleResponse>
                    {
                        Success = false,
                        Message = "Vehicle not found",
                        Data = null
                    };
                }

                var vehicleResponse = _mapper.Map<VehicleResponse>(vehicle);

                return new ServiceResponse<VehicleResponse>
                {
                    Success = true,
                    Message = "Vehicle retrieved successfully",
                    Data = vehicleResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<VehicleResponse>
                {
                    Success = false,
                    Message = $"Error retrieving vehicle: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<PagedResult<VehicleResponse>>> GetVehiclesPagedAsync(int pageIndex, int pageSize, int? categoryId = null, bool includeDeleted = false)
        {
            try
            {
                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();
                
                var pagedVehicles = await vehicleRepository.GetPagedAsync(
                    pageIndex: pageIndex,
                    pageSize: pageSize,
                    predicate: v => (includeDeleted || !v.IsDeleted) && (categoryId == null || v.CategoryId == categoryId),
                    orderBy: q => q.OrderBy(v => v.Model),
                    includes: v => v.Category
                );

                var vehicleResponses = _mapper.Map<List<VehicleResponse>>(pagedVehicles.Items);
                
                var pagedResult = new PagedResult<VehicleResponse>(
                    vehicleResponses,
                    pagedVehicles.TotalCount,
                    pageIndex,
                    pageSize
                );

                return new ServiceResponse<PagedResult<VehicleResponse>>
                {
                    Success = true,
                    Message = "Vehicles retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<PagedResult<VehicleResponse>>
                {
                    Success = false,
                    Message = $"Error retrieving paged vehicles: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<VehicleResponse>> CreateVehicleAsync(CreateVehicleRequest request)
        {
            try
            {
                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();
                var categoryRepository = _unitOfWork.GetRepository<VehicleCategory, int>();

                // Validate category exists
                var categoryExists = await categoryRepository.GetByIdAsync(request.CategoryId);
                if (categoryExists == null)
                {
                    return new ServiceResponse<VehicleResponse>
                    {
                        Success = false,
                        Message = "Invalid category ID",
                        Data = null
                    };
                }

                // Check if vehicle with same model, version, and color already exists
                var existingVehicle = await vehicleRepository.FirstOrDefaultAsync(v => 
                    v.Model == request.Model && 
                    v.Version == request.Version && 
                    v.Color == request.Color && 
                    !v.IsDeleted);

                if (existingVehicle != null)
                {
                    return new ServiceResponse<VehicleResponse>
                    {
                        Success = false,
                        Message = "A vehicle with the same model, version, and color already exists",
                        Data = null
                    };
                }

                var vehicle = _mapper.Map<Vehicle>(request);
                var createdVehicle = await vehicleRepository.AddAsync(vehicle);
                
                // Get the created vehicle with category for response
                var vehicleWithCategory = await vehicleRepository.GetByIdAsync(createdVehicle.Id, v => v.Category);
                var vehicleResponse = _mapper.Map<VehicleResponse>(vehicleWithCategory);

                return new ServiceResponse<VehicleResponse>
                {
                    Success = true,
                    Message = "Vehicle created successfully",
                    Data = vehicleResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<VehicleResponse>
                {
                    Success = false,
                    Message = $"Error creating vehicle: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<VehicleResponse>> UpdateVehicleAsync(UpdateVehicleRequest request)
        {
            try
            {
                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();
                var categoryRepository = _unitOfWork.GetRepository<VehicleCategory, int>();

                // Check if vehicle exists
                var existingVehicle = await vehicleRepository.GetByIdAsync(request.Id);
                if (existingVehicle == null)
                {
                    return new ServiceResponse<VehicleResponse>
                    {
                        Success = false,
                        Message = "Vehicle not found",
                        Data = null
                    };
                }

                // Validate category exists
                var categoryExists = await categoryRepository.GetByIdAsync(request.CategoryId);
                if (categoryExists == null)
                {
                    return new ServiceResponse<VehicleResponse>
                    {
                        Success = false,
                        Message = "Invalid category ID",
                        Data = null
                    };
                }

                // Check if another vehicle with same model, version, and color already exists
                var duplicateVehicle = await vehicleRepository.FirstOrDefaultAsync(v => 
                    v.Model == request.Model && 
                    v.Version == request.Version && 
                    v.Color == request.Color && 
                    v.Id != request.Id && 
                    !v.IsDeleted);

                if (duplicateVehicle != null)
                {
                    return new ServiceResponse<VehicleResponse>
                    {
                        Success = false,
                        Message = "A vehicle with the same model, version, and color already exists",
                        Data = null
                    };
                }

                // Update the vehicle properties
                existingVehicle.CategoryId = request.CategoryId;
                existingVehicle.Color = request.Color;
                existingVehicle.Price = request.Price;
                existingVehicle.ManufactureDate = request.ManufactureDate;
                existingVehicle.Model = request.Model;
                existingVehicle.Version = request.Version;
                existingVehicle.Image = request.Image;

                var updatedVehicle = await vehicleRepository.UpdateAsync(existingVehicle);
                
                // Get the updated vehicle with category for response
                var vehicleWithCategory = await vehicleRepository.GetByIdAsync(updatedVehicle.Id, v => v.Category);
                var vehicleResponse = _mapper.Map<VehicleResponse>(vehicleWithCategory);

                return new ServiceResponse<VehicleResponse>
                {
                    Success = true,
                    Message = "Vehicle updated successfully",
                    Data = vehicleResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<VehicleResponse>
                {
                    Success = false,
                    Message = $"Error updating vehicle: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse> SoftDeleteVehicleAsync(int id)
        {
            try
            {
                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();

                // Check if vehicle exists
                var existingVehicle = await vehicleRepository.GetByIdAsync(id);
                if (existingVehicle == null)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Vehicle not found"
                    };
                }

                if (existingVehicle.IsDeleted)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Vehicle is already deleted"
                    };
                }

                // Check if vehicle is being used in active appointments
                var appointmentRepository = _unitOfWork.GetRepository<Appointment, int>();
                var hasActiveAppointments = await appointmentRepository.AnyAsync(a => a.VehicleId == id && a.Status != "Cancelled");

                if (hasActiveAppointments)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Cannot delete vehicle because it has active appointments"
                    };
                }

                // Use the soft delete functionality from generic repository
                var deleted = await vehicleRepository.SoftDeleteAsync(id);
                if (!deleted)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Failed to delete vehicle"
                    };
                }

                return new ServiceResponse
                {
                    Success = true,
                    Message = "Vehicle deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Success = false,
                    Message = $"Error deleting vehicle: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse> RestoreVehicleAsync(int id)
        {
            try
            {
                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();

                // Check if vehicle exists
                var existingVehicle = await vehicleRepository.GetByIdAsync(id);
                if (existingVehicle == null)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Vehicle not found"
                    };
                }

                if (!existingVehicle.IsDeleted)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Vehicle is not deleted"
                    };
                }

                existingVehicle.IsDeleted = false;
                await vehicleRepository.UpdateAsync(existingVehicle);

                return new ServiceResponse
                {
                    Success = true,
                    Message = "Vehicle restored successfully"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Success = false,
                    Message = $"Error restoring vehicle: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse<IEnumerable<VehicleResponse>>> GetVehiclesByCategoryAsync(int categoryId, bool includeDeleted = false)
        {
            try
            {
                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();
                var vehicles = await vehicleRepository.GetAllAsync(
                    predicate: v => v.CategoryId == categoryId && (includeDeleted || !v.IsDeleted),
                    orderBy: q => q.OrderBy(v => v.Model),
                    includes: v => v.Category
                );

                var vehicleResponses = _mapper.Map<IEnumerable<VehicleResponse>>(vehicles);

                return new ServiceResponse<IEnumerable<VehicleResponse>>
                {
                    Success = true,
                    Message = "Vehicles retrieved successfully",
                    Data = vehicleResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<IEnumerable<VehicleResponse>>
                {
                    Success = false,
                    Message = $"Error retrieving vehicles by category: {ex.Message}",
                    Data = null
                };
            }
        }
    }
}
