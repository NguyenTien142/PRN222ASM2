using AutoMapper;
using Repositories.Interfaces;
using Repositories.Model;
using Services.DataTransferObject.UserDTO;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements
{
    public class UserServices : IUserServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserServices(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var userRepository = _unitOfWork.GetRepository<User, int>();
                var user = await userRepository.FirstOrDefaultAsync(
                    u => u.Username == request.Username && !u.IsDeleted,
                    u => u.Role, u => u.Customer, u => u.Dealer);

                if (user == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid username or password."
                    };
                }

                // Verify password
                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid username or password."
                    };
                }

                var userResponse = _mapper.Map<GetUserRespond>(user);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login successful.",
                    User = userResponse
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Login failed: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse> LogoutAsync(int userId)
        {
            try
            {
                // In a real application, you might want to invalidate tokens or update session data
                // For now, we'll just return a success response
                var userRepository = _unitOfWork.GetRepository<User, int>();
                var user = await userRepository.GetByIdAsync(userId);
                
                if (user == null || user.IsDeleted)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "User not found."
                    };
                }

                return new ServiceResponse
                {
                    Success = true,
                    Message = "Logout successful."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Success = false,
                    Message = $"Logout failed: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse<GetUserRespond>> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var userRepository = _unitOfWork.GetRepository<User, int>();
                var customerRepository = _unitOfWork.GetRepository<Customer, int>();
                var dealerRepository = _unitOfWork.GetRepository<Dealer, int>();

                // Check if username already exists
                var existingUser = await userRepository.FirstOrDefaultAsync(
                    u => u.Username == request.Username);

                if (existingUser != null)
                {
                    return new ServiceResponse<GetUserRespond>
                    {
                        Success = false,
                        Message = "Username already exists."
                    };
                }

                await _unitOfWork.BeginTransactionAsync();

                // Create user
                var user = _mapper.Map<User>(request);
                user.PasswordHash = HashPassword(request.Password);

                var createdUser = await userRepository.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // Create customer or dealer based on role
                // Assuming role 1 = Customer, role 2 = Dealer (adjust as needed)
                if (request.RoleId == 1 && !string.IsNullOrEmpty(request.CustomerName))
                {
                    var customer = _mapper.Map<Customer>(request);
                    customer.UserId = createdUser.Id;
                    await customerRepository.AddAsync(customer);
                }
                else if (request.RoleId == 2 && !string.IsNullOrEmpty(request.DealerName))
                {
                    var dealer = _mapper.Map<Dealer>(request);
                    dealer.UserId = createdUser.Id;
                    await dealerRepository.AddAsync(dealer);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Get the complete user data
                var userWithIncludes = await userRepository.GetByIdAsync(
                    createdUser.Id, u => u.Role, u => u.Customer, u => u.Dealer);

                var userResponse = _mapper.Map<GetUserRespond>(userWithIncludes);

                return new ServiceResponse<GetUserRespond>
                {
                    Success = true,
                    Message = "User registered successfully.",
                    Data = userResponse
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResponse<GetUserRespond>
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse<GetUserRespond>> GetUserByIdAsync(int id)
        {
            try
            {
                var userRepository = _unitOfWork.GetRepository<User, int>();
                var user = await userRepository.GetByIdAsync(
                    id, u => u.Role, u => u.Customer, u => u.Dealer);

                if (user == null || user.IsDeleted)
                {
                    return new ServiceResponse<GetUserRespond>
                    {
                        Success = false,
                        Message = "User not found."
                    };
                }

                var userResponse = _mapper.Map<GetUserRespond>(user);

                return new ServiceResponse<GetUserRespond>
                {
                    Success = true,
                    Message = "User retrieved successfully.",
                    Data = userResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<GetUserRespond>
                {
                    Success = false,
                    Message = $"Failed to retrieve user: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse<GetUserRespond>> UpdateUserAsync(UpdateUserRequest request)
        {
            try
            {
                var userRepository = _unitOfWork.GetRepository<User, int>();
                var customerRepository = _unitOfWork.GetRepository<Customer, int>();
                var dealerRepository = _unitOfWork.GetRepository<Dealer, int>();

                var user = await userRepository.GetByIdAsync(
                    request.Id, u => u.Role, u => u.Customer, u => u.Dealer);

                if (user == null || user.IsDeleted)
                {
                    return new ServiceResponse<GetUserRespond>
                    {
                        Success = false,
                        Message = "User not found."
                    };
                }

                await _unitOfWork.BeginTransactionAsync();

                // Update user fields
                if (!string.IsNullOrEmpty(request.Username))
                {
                    // Check if new username already exists
                    var existingUser = await userRepository.FirstOrDefaultAsync(
                        u => u.Username == request.Username && u.Id != request.Id);
                    
                    if (existingUser != null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return new ServiceResponse<GetUserRespond>
                        {
                            Success = false,
                            Message = "Username already exists."
                        };
                    }
                    
                    user.Username = request.Username;
                }

                if (!string.IsNullOrEmpty(request.Password))
                {
                    user.PasswordHash = HashPassword(request.Password);
                }

                await userRepository.UpdateAsync(user);

                // Update customer or dealer information
                if (user.Customer != null)
                {
                    if (!string.IsNullOrEmpty(request.CustomerName))
                        user.Customer.Name = request.CustomerName;
                    if (!string.IsNullOrEmpty(request.CustomerPhone))
                        user.Customer.Phone = request.CustomerPhone;
                    if (!string.IsNullOrEmpty(request.CustomerEmail))
                        user.Customer.Email = request.CustomerEmail;
                    if (!string.IsNullOrEmpty(request.CustomerAddress))
                        user.Customer.Address = request.CustomerAddress;

                    await customerRepository.UpdateAsync(user.Customer);
                }

                if (user.Dealer != null)
                {
                    if (!string.IsNullOrEmpty(request.DealerName))
                        user.Dealer.DealerName = request.DealerName;
                    if (!string.IsNullOrEmpty(request.DealerAddress))
                        user.Dealer.Address = request.DealerAddress;
                    if (request.DealerQuantity.HasValue)
                        user.Dealer.Quantity = request.DealerQuantity.Value;

                    await dealerRepository.UpdateAsync(user.Dealer);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Get updated user data
                var updatedUser = await userRepository.GetByIdAsync(
                    request.Id, u => u.Role, u => u.Customer, u => u.Dealer);

                var userResponse = _mapper.Map<GetUserRespond>(updatedUser);

                return new ServiceResponse<GetUserRespond>
                {
                    Success = true,
                    Message = "User updated successfully.",
                    Data = userResponse
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResponse<GetUserRespond>
                {
                    Success = false,
                    Message = $"Failed to update user: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse> DeleteUserAsync(int id)
        {
            try
            {
                var userRepository = _unitOfWork.GetRepository<User, int>();
                var user = await userRepository.GetByIdAsync(id);

                if (user == null || user.IsDeleted)
                {
                    return new ServiceResponse
                    {
                        Success = false,
                        Message = "User not found."
                    };
                }

                // Soft delete
                var success = await userRepository.SoftDeleteAsync(id);

                if (success)
                {
                    await _unitOfWork.SaveChangesAsync();
                    return new ServiceResponse
                    {
                        Success = true,
                        Message = "User deleted successfully."
                    };
                }

                return new ServiceResponse
                {
                    Success = false,
                    Message = "Failed to delete user."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Success = false,
                    Message = $"Failed to delete user: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse<IEnumerable<GetUserRespond>>> GetAllUsersAsync()
        {
            try
            {
                var userRepository = _unitOfWork.GetRepository<User, int>();
                var users = await userRepository.GetAllAsync(
                    u => !u.IsDeleted,
                    null,
                    u => u.Role, u => u.Customer, u => u.Dealer);

                var userResponses = _mapper.Map<IEnumerable<GetUserRespond>>(users);

                return new ServiceResponse<IEnumerable<GetUserRespond>>
                {
                    Success = true,
                    Message = "Users retrieved successfully.",
                    Data = userResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<IEnumerable<GetUserRespond>>
                {
                    Success = false,
                    Message = $"Failed to retrieve users: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse<IEnumerable<GetUserRespond>>> GetUsersByRoleAsync(int roleId)
        {
            try
            {
                var userRepository = _unitOfWork.GetRepository<User, int>();
                var users = await userRepository.GetAllAsync(
                    u => u.RoleId == roleId && !u.IsDeleted,
                    null,
                    u => u.Role, u => u.Customer, u => u.Dealer);

                var userResponses = _mapper.Map<IEnumerable<GetUserRespond>>(users);

                return new ServiceResponse<IEnumerable<GetUserRespond>>
                {
                    Success = true,
                    Message = "Users retrieved successfully.",
                    Data = userResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<IEnumerable<GetUserRespond>>
                {
                    Success = false,
                    Message = $"Failed to retrieve users: {ex.Message}"
                };
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == hash;
        }
    }
}
