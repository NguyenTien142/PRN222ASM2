using Services.DataTransferObject.Common;
using Services.DataTransferObject.UserDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IUserServices
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<ServiceResponse> LogoutAsync(int userId);
        Task<ServiceResponse<GetUserRespond>> RegisterAsync(RegisterRequest request);
        Task<ServiceResponse<GetUserRespond>> GetUserByIdAsync(int id);
        Task<ServiceResponse<GetUserRespond>> UpdateUserAsync(UpdateUserRequest request);
        Task<ServiceResponse> DeleteUserAsync(int id);
        Task<ServiceResponse<IEnumerable<GetUserRespond>>> GetAllUsersAsync();
        Task<ServiceResponse<IEnumerable<GetUserRespond>>> GetUsersByRoleAsync(int roleId);
    }
}
