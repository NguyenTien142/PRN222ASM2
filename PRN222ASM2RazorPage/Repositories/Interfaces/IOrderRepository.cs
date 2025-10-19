using Repositories.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order, int>
    {
        Task<bool> CreateOrder(int customerId, int vehicleId, decimal amount, int dealerId);
        Task<List<Order>> GetAllOrdersWithDetails();
        Task<List<Order>> GetOrdersByDealerWithDetails(int dealerId);
        Task<bool> UpdateOrderStatus(int orderId, string status);
        Task<bool> UpdateOrderStatusAndReduceInventory(int orderId, string status);
        Task<int> GetOrderCount();
        Task<List<Order>> GetAllOrdersSimple();
    }
}
