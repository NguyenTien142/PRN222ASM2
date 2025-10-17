using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.DataTransferObject.Common;
using Services.DataTransferObject.InventoryDTO;

namespace Services.Interfaces
{
    public interface IDealerServices
    {
        Task<ServiceResponse<IEnumerable<InventoryResponse>>> GetDealerInventoryAsync(int dealerId);
        Task<ServiceResponse<InventoryResponse>> AddToInventoryAsync(AddToInventoryRequest request);
        Task<ServiceResponse<InventoryResponse>> UpdateInventoryAsync(UpdateInventoryRequest request);
        Task<ServiceResponse> RemoveFromInventoryAsync(int vehicleId, int dealerId);
        Task<ServiceResponse<InventoryResponse>> GetInventoryItemAsync(int vehicleId, int dealerId);
    }
}
