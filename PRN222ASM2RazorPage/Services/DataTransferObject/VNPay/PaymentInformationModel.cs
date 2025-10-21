using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DataTransferObject.VNPay
{
    public class PaymentInformationModel
    {
        public int OrderId { get; set; }
        public string OrderType { get; set; }
        public decimal Amount { get; set; }
        public string OrderDescription { get; set; }

    }
}
