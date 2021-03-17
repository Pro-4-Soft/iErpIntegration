using System;
using System.Collections.Generic;

namespace Pro4Soft.iErpIntegration.Dto.P4W
{
    public class CustomerReturn : IdObject
    {
        public string CustomerReturnNumber { get; set; }
        public string ReferenceNumber { get; set; }
        public string CustomerReturnState { get; set; }
        public Client Client { get; set; }
        public Guid? ClientId { get; set; }
        public List<CustomerReturnLine> Lines { get; set; } = new List<CustomerReturnLine>();
        public DateTimeOffset? UploadDate { get; set; }
    }

    public class CustomerReturnLine : IdObject
    {
        public int LineNumber { get; set; }
        public string ReferenceNumber { get; set; }
        public Product Product { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public List<CustomerReturnLineDetail> LineDetails { get; set; } = new List<CustomerReturnLineDetail>();
    }

    public class CustomerReturnLineDetail : IdObject
    {
        public int? PacksizeEachCount { get; set; }
        public int? ReceivedQuantity { get; set; }
        public string LotNumber { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public string SerialNumber { get; set; }
    }
}