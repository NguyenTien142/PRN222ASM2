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

        public async Task<bool> CreateOrder(int customerId, int vehicleId, decimal amount, int dealerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    CustomerId = customerId,
                    DealerId = dealerId, 
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

        public async Task<List<Order>> GetAllOrdersWithDetails()
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.Customer)
                        .ThenInclude(c => c.User)
                    .Include(o => o.Dealer)
                        .ThenInclude(d => d.User)
                    .Include(o => o.OrderVehicles)
                        .ThenInclude(ov => ov.Vehicle)
                            .ThenInclude(v => v.Category)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log exception in real application
                // For debugging, you can add a breakpoint here
                throw new Exception($"Error retrieving orders: {ex.Message}", ex);
            }
        }

        public async Task<List<Order>> GetOrdersByDealerWithDetails(int dealerId)
        {
            try
            {
                // Get orders that belong to this dealer OR contain vehicles managed by this dealer
                return await _context.Orders
                    .Include(o => o.Customer)
                        .ThenInclude(c => c.User)
                    .Include(o => o.Dealer)
                        .ThenInclude(d => d.User)
                    .Include(o => o.OrderVehicles)
                        .ThenInclude(ov => ov.Vehicle)
                            .ThenInclude(v => v.Category)
                    .Where(o => o.DealerId == dealerId || 
                               o.OrderVehicles.Any(ov => 
                                   _context.VehicleDealers
                                       .Any(vd => vd.DealerId == dealerId && vd.VehicleId == ov.VehicleId)))
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving orders for dealer {dealerId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateOrderStatus(int orderId, string status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                    return false;

                order.Status = status;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateOrderStatusAndReduceInventory(int orderId, string status)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get order with vehicle details
                var order = await _context.Orders
                    .Include(o => o.OrderVehicles)
                        .ThenInclude(ov => ov.Vehicle)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // Update order status
                order.Status = status;

                // If status is PAID, reduce inventory
                if (status.ToUpper() == "PAID")
                {
                    foreach (var orderVehicle in order.OrderVehicles)
                    {
                        // Try to find vehicle in current dealer's inventory first
                        var vehicleDealer = await _context.VehicleDealers
                            .FirstOrDefaultAsync(vd => vd.VehicleId == orderVehicle.VehicleId && vd.DealerId == order.DealerId);

                        // If not found in original dealer's inventory, try other dealers
                        if (vehicleDealer == null || vehicleDealer.Quantity < orderVehicle.Quantity)
                        {
                            vehicleDealer = await _context.VehicleDealers
                                .Where(vd => vd.VehicleId == orderVehicle.VehicleId && vd.Quantity >= orderVehicle.Quantity)
                                .FirstOrDefaultAsync();

                            if (vehicleDealer != null)
                            {
                                // Update order's dealer to the one who actually has the inventory
                                order.DealerId = vehicleDealer.DealerId;
                            }
                        }

                        if (vehicleDealer != null && vehicleDealer.Quantity >= orderVehicle.Quantity)
                        {
                            vehicleDealer.Quantity -= orderVehicle.Quantity;
                        }
                        else
                        {
                            // Not enough inventory anywhere - rollback
                            await transaction.RollbackAsync();
                            return false;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Error updating order status: {ex.Message}", ex);
            }
        }

        public async Task<int> GetOrderCount()
        {
            try
            {
                return await _context.Orders.CountAsync();
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<List<Order>> GetAllOrdersSimple()
        {
            try
            {
                return await _context.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Order>();
            }
        }
    }
}
