using System;

namespace Pro4Soft.iErpIntegration.Dto.P4W
{
    public class Adjustment:IdObject
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Client { get; set; }
        public string SubType { get; set; }

        public string FromWarehouse { get; set; }
        public string ToWarehouse { get; set; }
        public string Sku { get; set; }

        public int? EachCount { get; set; }
        public int? NumberOfPacks { get; set; }
        public string LotNumber { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public string SerialNumber { get; set; }

        public decimal Quantity { get; set; }
        public string Reason { get; set; }
    }
}
