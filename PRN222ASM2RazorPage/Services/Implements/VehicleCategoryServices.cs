using AutoMapper;
using Repositories.Interfaces;
using Repositories.Model;
using Services.DataTransferObject.Common;
using Services.DataTransferObject.VehicleCategoryDTO;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements
{
    public class VehicleCategoryServices : IVehicleCategoryServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public VehicleCategoryServices(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<IEnumerable<VehicleCategoryResponse>>> GetAllCategoriesAsync()
        {
            try
            {
                var categoryRepository = _unitOfWork.GetRepository<VehicleCategory, int>();
                var categories = await categoryRepository.GetAllAsync();
                
                var categoryResponses = _mapper.Map<IEnumerable<VehicleCategoryResponse>>(categories);
                
                return new ServiceResponse<IEnumerable<VehicleCategoryResponse>>
                {
                    Success = true,
                    Message = "Categories retrieved successfully",
                    Data = categoryResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<IEnumerable<VehicleCategoryResponse>>
                {
                    Success = false,
                    Message = $"Error retrieving categories: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<VehicleCategoryResponse>> GetCategoryByIdAsync(int id)
        {
            try
            {
                var categoryRepository = _unitOfWork.GetRepository<VehicleCategory, int>();
                var category = await categoryRepository.GetByIdAsync(id);

                if (category == null)
                {
                    return new ServiceResponse<VehicleCategoryResponse>
                    {
                        Success = false,
                        Message = "Category not found",
                        Data = null
                    };
                }

                var categoryResponse = _mapper.Map<VehicleCategoryResponse>(category);

                return new ServiceResponse<VehicleCategoryResponse>
                {
                    Success = true,
                    Message = "Category retrieved successfully",
                    Data = categoryResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<VehicleCategoryResponse>
                {
                    Success = false,
                    Message = $"Error retrieving category: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<VehicleCategoryResponse>> CreateCategoryAsync(CreateVehicleCategoryRequest request)
        {
            try
            {
                var categoryRepository = _unitOfWork.GetRepository<VehicleCategory, int>();

                // Check if category with same name already exists
                var existingCategory = await categoryRepository.FirstOrDefaultAsync(c => c.Name == request.Name);
                if (existingCategory != null)
                {
                    return new ServiceResponse<VehicleCategoryResponse>
                    {
                        Success = false,
                        Message = "A category with this name already exists",
                        Data = null
                    };
                }

                var category = _mapper.Map<VehicleCategory>(request);
                var createdCategory = await categoryRepository.AddAsync(category);
                var categoryResponse = _mapper.Map<VehicleCategoryResponse>(createdCategory);

                return new ServiceResponse<VehicleCategoryResponse>
                {
                    Success = true,
                    Message = "Category created successfully",
                    Data = categoryResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<VehicleCategoryResponse>
                {
                    Success = false,
                    Message = $"Error creating category: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<VehicleCategoryResponse>> UpdateCategoryAsync(UpdateVehicleCategoryRequest request)
        {
            try
            {
                var categoryRepository = _unitOfWork.GetRepository<VehicleCategory, int>();

                // Check if category exists
                var existingCategory = await categoryRepository.GetByIdAsync(request.Id);
                if (existingCategory == null)
                {
                    return new ServiceResponse<VehicleCategoryResponse>
                    {
                        Success = false,
                        Message = "Category not found",
                        Data = null
                    };
                }

                // Check if another category with same name already exists
                var duplicateCategory = await categoryRepository.FirstOrDefaultAsync(c => c.Name == request.Name && c.Id != request.Id);
                if (duplicateCategory != null)
                {
                    return new ServiceResponse<VehicleCategoryResponse>
                    {
                        Success = false,
                        Message = "A category with this name already exists",
                        Data = null
                    };
                }

                // Update the category
                existingCategory.Name = request.Name;
                var updatedCategory = await categoryRepository.UpdateAsync(existingCategory);
                var categoryResponse = _mapper.Map<VehicleCategoryResponse>(updatedCategory);

                return new ServiceResponse<VehicleCategoryResponse>
                {
                    Success = true,
                    Message = "Category updated successfully",
                    Data = categoryResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<VehicleCategoryResponse>
                {
                    Success = false,
                    Message = $"Error updating category: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse> DeleteCategoryAsync(int id)
        {
            try
            {
                var categoryRepository = _unitOfWork.GetRepository<VehicleCategory, int>();

                // Check if category exists
                var existingCategory = await categoryRepository.GetByIdAsync(id);
                if (existingCategory == null)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Category not found"
                    };
                }

                // Check if category is being used by any vehicles
                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();
                var hasVehicles = await vehicleRepository.AnyAsync(v => v.CategoryId == id);
                if (hasVehicles)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Cannot delete category because it is being used by one or more vehicles"
                    };
                }

                var deleted = await categoryRepository.DeleteAsync(id);
                if (!deleted)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Failed to delete category"
                    };
                }

                return new ServiceResponse
                {
                    Success = true,
                    Message = "Category deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Success = false,
                    Message = $"Error deleting category: {ex.Message}"
                };
            }
        }
    }
}
