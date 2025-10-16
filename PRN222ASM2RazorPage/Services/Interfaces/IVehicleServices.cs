using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Helpper;
using Services.DataTransferObject.Common;
using Services.DataTransferObject.VehicleDTO;

namespace Services.Interfaces
{
    public interface IVehicleServices
    {
        Task<ServiceResponse<IEnumerable<VehicleResponse>>> GetAllVehiclesAsync(bool includeDeleted = false);
        Task<ServiceResponse<VehicleResponse>> GetVehicleByIdAsync(int id);
        Task<ServiceResponse<PagedResult<VehicleResponse>>> GetVehiclesPagedAsync(int pageIndex, int pageSize, int? categoryId = null, bool includeDeleted = false);
        Task<ServiceResponse<VehicleResponse>> CreateVehicleAsync(CreateVehicleRequest request);
        Task<ServiceResponse<VehicleResponse>> UpdateVehicleAsync(UpdateVehicleRequest request);
        Task<ServiceResponse> SoftDeleteVehicleAsync(int id);
        Task<ServiceResponse> RestoreVehicleAsync(int id);
        Task<ServiceResponse<IEnumerable<VehicleResponse>>> GetVehiclesByCategoryAsync(int categoryId, bool includeDeleted = false);
    }
}
