using AutoMapper;
using Repositories.Interfaces;
using Repositories.Model;
using Services.DataTransferObject.Common;
using Services.DataTransferObject.InventoryDTO;
using Services.Interfaces;

namespace Services.Implements
{
    public class DealerServices : IDealerServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DealerServices(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<IEnumerable<InventoryResponse>>> GetDealerInventoryAsync(int dealerId)
        {
            try
            {
                var vehicleDealerRepository = _unitOfWork.GetRepository<VehicleDealer, int>();
                var inventoryItems = await vehicleDealerRepository.GetAllAsync(
                    predicate: vd => vd.DealerId == dealerId,
                    orderBy: null,
                    vd => vd.Vehicle,
                    vd => vd.Dealer
                );

                var inventoryResponses = new List<InventoryResponse>();
                
                foreach (var vd in inventoryItems)
                {
                    var vehicle = vd.Vehicle;
                    var categoryRepository = _unitOfWork.GetRepository<VehicleCategory, int>();
                    var category = await categoryRepository.GetByIdAsync(vehicle.CategoryId);
                    
                    inventoryResponses.Add(new InventoryResponse
                    {
                        VehicleId = vd.VehicleId,
                        DealerId = vd.DealerId,
                        Quantity = vd.Quantity,
                        VehicleModel = vehicle.Model,
                        VehicleColor = vehicle.Color,
                        VehicleVersion = vehicle.Version,
                        VehiclePrice = vehicle.Price,
                        VehicleManufactureDate = vehicle.ManufactureDate,
                        VehicleImage = vehicle.Image,
                        CategoryName = category?.Name ?? "Unknown",
                        CategoryId = vehicle.CategoryId,
                        DealerName = vd.Dealer.DealerName,
                        DealerAddress = vd.Dealer.Address
                    });
                }

                return new ServiceResponse<IEnumerable<InventoryResponse>>
                {
                    Success = true,
                    Message = "Inventory retrieved successfully",
                    Data = inventoryResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<IEnumerable<InventoryResponse>>
                {
                    Success = false,
                    Message = $"Error retrieving inventory: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<InventoryResponse>> AddToInventoryAsync(AddToInventoryRequest request)
        {
            try
            {
                var vehicleDealerRepository = _unitOfWork.GetRepository<VehicleDealer, int>();
                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();
                var dealerRepository = _unitOfWork.GetRepository<Dealer, int>();

                // Check if vehicle exists
                var vehicle = await vehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null || vehicle.IsDeleted)
                {
                    return new ServiceResponse<InventoryResponse>
                    {
                        Success = false,
                        Message = "Vehicle not found or deleted",
                        Data = null
                    };
                }

                // Check if dealer exists
                var dealer = await dealerRepository.GetByIdAsync(request.DealerId);
                if (dealer == null)
                {
                    return new ServiceResponse<InventoryResponse>
                    {
                        Success = false,
                        Message = "Dealer not found",
                        Data = null
                    };
                }

                // Check if inventory item already exists
                var existingInventoryItem = await vehicleDealerRepository.FirstOrDefaultAsync(
                    vd => vd.VehicleId == request.VehicleId && vd.DealerId == request.DealerId);

                if (existingInventoryItem != null)
                {
                    // Update existing quantity
                    existingInventoryItem.Quantity += request.Quantity;
                    await vehicleDealerRepository.UpdateAsync(existingInventoryItem);
                }
                else
                {
                    // Create new inventory item
                    var newInventoryItem = new VehicleDealer
                    {
                        VehicleId = request.VehicleId,
                        DealerId = request.DealerId,
                        Quantity = request.Quantity
                    };
                    existingInventoryItem = await vehicleDealerRepository.AddAsync(newInventoryItem);
                }

                // Get category info
                var categoryRepository = _unitOfWork.GetRepository<VehicleCategory, int>();
                var category = await categoryRepository.GetByIdAsync(vehicle.CategoryId);

                var inventoryResponse = new InventoryResponse
                {
                    VehicleId = existingInventoryItem.VehicleId,
                    DealerId = existingInventoryItem.DealerId,
                    Quantity = existingInventoryItem.Quantity,
                    VehicleModel = vehicle.Model,
                    VehicleColor = vehicle.Color,
                    VehicleVersion = vehicle.Version,
                    VehiclePrice = vehicle.Price,
                    VehicleManufactureDate = vehicle.ManufactureDate,
                    VehicleImage = vehicle.Image,
                    CategoryName = category?.Name ?? "Unknown",
                    CategoryId = vehicle.CategoryId,
                    DealerName = dealer.DealerName,
                    DealerAddress = dealer.Address
                };

                return new ServiceResponse<InventoryResponse>
                {
                    Success = true,
                    Message = "Vehicle added to inventory successfully",
                    Data = inventoryResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<InventoryResponse>
                {
                    Success = false,
                    Message = $"Error adding to inventory: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse<InventoryResponse>> UpdateInventoryAsync(UpdateInventoryRequest request)
        {
            try
            {
                var vehicleDealerRepository = _unitOfWork.GetRepository<VehicleDealer, int>();

                var inventoryItem = await vehicleDealerRepository.FirstOrDefaultAsync(
                    vd => vd.VehicleId == request.VehicleId && vd.DealerId == request.DealerId);

                if (inventoryItem == null)
                {
                    return new ServiceResponse<InventoryResponse>
                    {
                        Success = false,
                        Message = "Inventory item not found",
                        Data = null
                    };
                }

                inventoryItem.Quantity = request.Quantity;
                await vehicleDealerRepository.UpdateAsync(inventoryItem);

                // Load related data for response
                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();
                var dealerRepository = _unitOfWork.GetRepository<Dealer, int>();
                var categoryRepository = _unitOfWork.GetRepository<VehicleCategory, int>();
                
                var vehicle = await vehicleRepository.GetByIdAsync(inventoryItem.VehicleId);
                var dealer = await dealerRepository.GetByIdAsync(inventoryItem.DealerId);
                var category = await categoryRepository.GetByIdAsync(vehicle.CategoryId);

                var inventoryResponse = new InventoryResponse
                {
                    VehicleId = inventoryItem.VehicleId,
                    DealerId = inventoryItem.DealerId,
                    Quantity = inventoryItem.Quantity,
                    VehicleModel = vehicle.Model,
                    VehicleColor = vehicle.Color,
                    VehicleVersion = vehicle.Version,
                    VehiclePrice = vehicle.Price,
                    VehicleManufactureDate = vehicle.ManufactureDate,
                    VehicleImage = vehicle.Image,
                    CategoryName = category?.Name ?? "Unknown",
                    CategoryId = vehicle.CategoryId,
                    DealerName = dealer.DealerName,
                    DealerAddress = dealer.Address
                };

                return new ServiceResponse<InventoryResponse>
                {
                    Success = true,
                    Message = "Inventory updated successfully",
                    Data = inventoryResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<InventoryResponse>
                {
                    Success = false,
                    Message = $"Error updating inventory: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResponse> RemoveFromInventoryAsync(int vehicleId, int dealerId)
        {
            try
            {
                var vehicleDealerRepository = _unitOfWork.GetRepository<VehicleDealer, int>();

                var inventoryItem = await vehicleDealerRepository.FirstOrDefaultAsync(
                    vd => vd.VehicleId == vehicleId && vd.DealerId == dealerId);

                if (inventoryItem == null)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "Inventory item not found"
                    };
                }

                // Set quantity to 0 instead of deleting (workaround for composite key)
                inventoryItem.Quantity = 0;
                await vehicleDealerRepository.UpdateAsync(inventoryItem);

                return new ServiceResponse
                {
                    Success = true,
                    Message = "Vehicle removed from inventory successfully"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Success = false,
                    Message = $"Error removing from inventory: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse<InventoryResponse>> GetInventoryItemAsync(int vehicleId, int dealerId)
        {
            try
            {
                var vehicleDealerRepository = _unitOfWork.GetRepository<VehicleDealer, int>();

                var inventoryItem = await vehicleDealerRepository.FirstOrDefaultAsync(
                    vd => vd.VehicleId == vehicleId && vd.DealerId == dealerId);

                if (inventoryItem == null)
                {
                    return new ServiceResponse<InventoryResponse>
                    {
                        Success = false,
                        Message = "Inventory item not found",
                        Data = null
                    };
                }

                // Load related data
                var vehicleRepository = _unitOfWork.GetRepository<Vehicle, int>();
                var dealerRepository = _unitOfWork.GetRepository<Dealer, int>();
                var categoryRepository = _unitOfWork.GetRepository<VehicleCategory, int>();
                
                var vehicle = await vehicleRepository.GetByIdAsync(inventoryItem.VehicleId);
                var dealer = await dealerRepository.GetByIdAsync(inventoryItem.DealerId);
                var category = await categoryRepository.GetByIdAsync(vehicle.CategoryId);

                var inventoryResponse = new InventoryResponse
                {
                    VehicleId = inventoryItem.VehicleId,
                    DealerId = inventoryItem.DealerId,
                    Quantity = inventoryItem.Quantity,
                    VehicleModel = vehicle.Model,
                    VehicleColor = vehicle.Color,
                    VehicleVersion = vehicle.Version,
                    VehiclePrice = vehicle.Price,
                    VehicleManufactureDate = vehicle.ManufactureDate,
                    VehicleImage = vehicle.Image,
                    CategoryName = category?.Name ?? "Unknown",
                    CategoryId = vehicle.CategoryId,
                    DealerName = dealer.DealerName,
                    DealerAddress = dealer.Address
                };

                return new ServiceResponse<InventoryResponse>
                {
                    Success = true,
                    Message = "Inventory item retrieved successfully",
                    Data = inventoryResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<InventoryResponse>
                {
                    Success = false,
                    Message = $"Error retrieving inventory item: {ex.Message}",
                    Data = null
                };
            }
        }
    }
}