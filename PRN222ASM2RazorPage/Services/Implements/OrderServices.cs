using AutoMapper;
using Repositories.Interfaces;
using Services.DataTransferObject.Common;
using Services.DataTransferObject.OrderDTO;
using Services.DataTransferObject.VehicleDTO;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements
{
    public class OrderServices : IOrderServices
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OrderServices(IOrderRepository orderRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResponse> CreateOrder(int customerId, int vehicleId, decimal amount, int dealerId)
        {
            try
            {
                // Validate customer exists
                var customerRepository = _unitOfWork.GetRepository<Repositories.Model.Customer, int>();
                var customer = await customerRepository.GetByIdAsync(customerId);
                if (customer == null)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Customer not found."
                    };
                }

                // Validate dealer exists
                var dealerRepository = _unitOfWork.GetRepository<Repositories.Model.Dealer, int>();
                var dealer = await dealerRepository.GetByIdAsync(dealerId);
                if (dealer == null)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Dealer not found."
                    };
                }

                // Validate vehicle exists and is available
                var vehicleRepository = _unitOfWork.GetRepository<Repositories.Model.Vehicle, int>();
                var vehicle = await vehicleRepository.GetByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Vehicle not found."
                    };
                }

                if (vehicle.IsDeleted)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Vehicle is not available."
                    };
                }

                // Create the order
                var result = await _orderRepository.CreateOrder(customerId, vehicleId, amount, dealerId);
                
                if (result)
                {
                    return new ServiceResponse
                    {
                        Success = true,
                        Message = "Order created successfully."
                    };
                }
                else
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Failed to create order."
                    };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Success = false,
                    Message = $"An error occurred while creating the order: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse<List<OrderResponse>>> GetAllOrders()
        {
            try
            {
                // First check if any orders exist
                var orderCount = await _orderRepository.GetOrderCount();
                if (orderCount == 0)
                {
                    return new ServiceResponse<List<OrderResponse>>
                    {
                        Success = true,
                        Message = "No orders found in the database.",
                        Data = new List<OrderResponse>()
                    };
                }

                // Try to get orders with details
                var orders = await _orderRepository.GetAllOrdersWithDetails();
                
                if (orders == null || !orders.Any())
                {
                    // Try getting simple orders for debugging
                    var simpleOrders = await _orderRepository.GetAllOrdersSimple();
                    if (simpleOrders.Any())
                    {
                        return new ServiceResponse<List<OrderResponse>>
                        {
                            Success = false,
                            Message = $"Found {simpleOrders.Count} orders but failed to load navigation properties.",
                            Data = new List<OrderResponse>()
                        };
                    }
                    
                    return new ServiceResponse<List<OrderResponse>>
                    {
                        Success = true,
                        Message = "No orders found.",
                        Data = new List<OrderResponse>()
                    };
                }
                
                var orderResponses = orders.Select(order => new OrderResponse
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    DealerId = order.DealerId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    Customer = order.Customer != null ? new CustomerResponse
                    {
                        Id = order.Customer.Id,
                        Name = order.Customer.Name,
                        Phone = order.Customer.Phone,
                        Address = order.Customer.Address
                    } : null,
                    Dealer = order.Dealer != null ? new DealerResponse
                    {
                        Id = order.Dealer.Id,
                        DealerName = order.Dealer.DealerName,
                        Address = order.Dealer.Address,
                        Quantity = order.Dealer.Quantity
                    } : null,
                    OrderVehicles = order.OrderVehicles?.Select(ov => new OrderVehicleResponse
                    {
                        Id = ov.OrderId,
                        OrderId = ov.OrderId,
                        VehicleId = ov.VehicleId,
                        Quantity = ov.Quantity,
                        UnitPrice = ov.UnitPrice,
                        Vehicle = ov.Vehicle != null ? new VehicleResponse
                        {
                            Id = ov.Vehicle.Id,
                            Model = ov.Vehicle.Model,
                            Color = ov.Vehicle.Color,
                            Price = ov.Vehicle.Price,
                            ManufactureDate = ov.Vehicle.ManufactureDate,
                            Version = ov.Vehicle.Version,
                            Image = ov.Vehicle.Image,
                            CategoryName = ov.Vehicle.Category?.Name ?? "Unknown"
                        } : null
                    }).ToList() ?? new List<OrderVehicleResponse>()
                }).ToList();

                return new ServiceResponse<List<OrderResponse>>
                {
                    Success = true,
                    Message = $"Retrieved {orderResponses.Count} orders successfully.",
                    Data = orderResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<OrderResponse>>
                {
                    Success = false,
                    Message = $"An error occurred while retrieving orders: {ex.Message}",
                    Data = new List<OrderResponse>()
                };
            }
        }

        public async Task<ServiceResponse> UpdateOrderStatus(int orderId, string status)
        {
            try
            {
                // Validate status
                var validStatuses = new[] { "PENDING", "CANCELLED", "APPROVE", "PAID", "DELIVERING", "DONE" };
                if (!validStatuses.Contains(status.ToUpper()))
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Invalid order status."
                    };
                }

                var result = await _orderRepository.UpdateOrderStatus(orderId, status.ToUpper());
                
                if (result)
                {
                    return new ServiceResponse
                    {
                        Success = true,
                        Message = $"Order status updated to {status.ToUpper()} successfully."
                    };
                }
                else
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Failed to update order status. Order not found."
                    };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Success = false,
                    Message = $"An error occurred while updating order status: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse> UpdateOrderStatusWithInventory(int orderId, string status)
        {
            try
            {
                // Validate status
                var validStatuses = new[] { "PENDING", "CANCELLED", "APPROVE", "PAID", "DELIVERING", "DONE" };
                if (!validStatuses.Contains(status.ToUpper()))
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Invalid order status."
                    };
                }

                var result = await _orderRepository.UpdateOrderStatusAndReduceInventory(orderId, status.ToUpper());
                
                if (result)
                {
                    var message = status.ToUpper() == "PAID" 
                        ? $"Order status updated to {status.ToUpper()} and inventory reduced successfully."
                        : $"Order status updated to {status.ToUpper()} successfully.";
                    
                    return new ServiceResponse
                    {
                        Success = true,
                        Message = message
                    };
                }
                else
                {
                    var errorMessage = status.ToUpper() == "PAID" 
                        ? "Failed to update order status. Order not found or insufficient inventory."
                        : "Failed to update order status. Order not found.";
                    
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = errorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Success = false,
                    Message = $"An error occurred while updating order status: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse<List<OrderResponse>>> GetOrdersByDealer(int dealerId)
        {
            try
            {
                var orders = await _orderRepository.GetOrdersByDealerWithDetails(dealerId);
                
                if (orders == null || !orders.Any())
                {
                    return new ServiceResponse<List<OrderResponse>>
                    {
                        Success = true,
                        Message = "No orders found for this dealer.",
                        Data = new List<OrderResponse>()
                    };
                }
                
                var orderResponses = orders.Select(order => new OrderResponse
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    DealerId = order.DealerId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    Customer = order.Customer != null ? new CustomerResponse
                    {
                        Id = order.Customer.Id,
                        Name = order.Customer.Name,
                        Phone = order.Customer.Phone,
                        Address = order.Customer.Address
                    } : null,
                    Dealer = order.Dealer != null ? new DealerResponse
                    {
                        Id = order.Dealer.Id,
                        DealerName = order.Dealer.DealerName,
                        Address = order.Dealer.Address,
                        Quantity = order.Dealer.Quantity
                    } : null,
                    OrderVehicles = order.OrderVehicles?.Select(ov => new OrderVehicleResponse
                    {
                        Id = ov.OrderId,
                        OrderId = ov.OrderId,
                        VehicleId = ov.VehicleId,
                        Quantity = ov.Quantity,
                        UnitPrice = ov.UnitPrice,
                        Vehicle = ov.Vehicle != null ? new VehicleResponse
                        {
                            Id = ov.Vehicle.Id,
                            Model = ov.Vehicle.Model,
                            Color = ov.Vehicle.Color,
                            Price = ov.Vehicle.Price,
                            ManufactureDate = ov.Vehicle.ManufactureDate,
                            Version = ov.Vehicle.Version,
                            Image = ov.Vehicle.Image,
                            CategoryName = ov.Vehicle.Category?.Name ?? "Unknown"
                        } : null
                    }).ToList() ?? new List<OrderVehicleResponse>()
                }).ToList();

                return new ServiceResponse<List<OrderResponse>>
                {
                    Success = true,
                    Message = $"Retrieved {orderResponses.Count} orders for dealer successfully.",
                    Data = orderResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<OrderResponse>>
                {
                    Success = false,
                    Message = $"An error occurred while retrieving dealer orders: {ex.Message}",
                    Data = new List<OrderResponse>()
                };
            }
        }
    }
}
