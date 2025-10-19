using Repositories.Model;
using Services.DataTransferObject.Common;
using Services.DataTransferObject.OrderDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IOrderServices 
    {
        Task<ServiceResponse> CreateOrder(int customerId, int vehicleId, decimal amount, int dealerId);
        Task<ServiceResponse<List<OrderResponse>>> GetAllOrders();
        Task<ServiceResponse<List<OrderResponse>>> GetOrdersByDealer(int dealerId);
        Task<ServiceResponse> UpdateOrderStatus(int orderId, string status);
        Task<ServiceResponse> UpdateOrderStatusWithInventory(int orderId, string status);
    }
}
