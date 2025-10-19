using Repositories.Model;
using Services.DataTransferObject.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IOrderServices 
    {
        Task<ServiceResponse> CreateOrder(int customerId, int vehicleId, decimal amount);
    }
}
