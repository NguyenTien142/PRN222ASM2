using Microsoft.EntityFrameworkCore;
using Repositories.Context;
using Repositories.GenericRepository;
using Repositories.Interfaces;
using Repositories.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.CustomRepositories
{
    public class OrderRepository : GenericRepository<Order, int>, IOrderRepository
    {
        private readonly Prn222asm2Context _context;

        public OrderRepository(Prn222asm2Context context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> CreateOrder(int customerId, int vehicleId, decimal amount)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    CustomerId = customerId,
                    DealerId = 1, 
                    OrderDate = DateTime.Now,
                    Status = "PENDING",
                    TotalAmount = amount
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                var orderVehicle = new OrderVehicle
                {
                    OrderId = order.Id, 
                    VehicleId = vehicleId,
                    Quantity = 1,
                    UnitPrice = amount
                };

                _context.OrderVehicles.Add(orderVehicle);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        
    }
}
