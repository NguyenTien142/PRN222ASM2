using AutoMapper;
using Repositories.Interfaces;
using Services.DataTransferObject.Common;
using Services.DataTransferObject.OrderDTO;
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

       

        public async Task<ServiceResponse> CreateOrder(int customerId, int vehicleId, decimal amount)
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
                var result = await _orderRepository.CreateOrder(customerId, vehicleId, amount);
                
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

    }
}
