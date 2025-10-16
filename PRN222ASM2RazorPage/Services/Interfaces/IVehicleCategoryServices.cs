using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.DataTransferObject.Common;
using Services.DataTransferObject.VehicleCategoryDTO;

namespace Services.Interfaces
{
    public interface IVehicleCategoryServices
    {
        Task<ServiceResponse<IEnumerable<VehicleCategoryResponse>>> GetAllCategoriesAsync();
        Task<ServiceResponse<VehicleCategoryResponse>> GetCategoryByIdAsync(int id);
        Task<ServiceResponse<VehicleCategoryResponse>> CreateCategoryAsync(CreateVehicleCategoryRequest request);
        Task<ServiceResponse<VehicleCategoryResponse>> UpdateCategoryAsync(UpdateVehicleCategoryRequest request);
        Task<ServiceResponse> DeleteCategoryAsync(int id);
    }
}
