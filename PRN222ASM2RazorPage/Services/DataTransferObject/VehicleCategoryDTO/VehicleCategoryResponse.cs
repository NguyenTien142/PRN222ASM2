using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DataTransferObject.VehicleCategoryDTO
{
    public class VehicleCategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}