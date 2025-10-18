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
        Task<bool> CreateOrder(int customerId, int vehicleId, decimal amount);
       
    }
}
