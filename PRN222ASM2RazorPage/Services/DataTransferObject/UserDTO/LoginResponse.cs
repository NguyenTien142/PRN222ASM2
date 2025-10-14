using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DataTransferObject.UserDTO
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public GetUserRespond? User { get; set; }
        public string? Token { get; set; } // For JWT token if implemented
    }
}