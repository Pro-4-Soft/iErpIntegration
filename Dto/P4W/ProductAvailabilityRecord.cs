using System;

namespace Pro4Soft.iErpIntegration.Dto.P4W
{
    public class ProductAvailabilityRecord
    {
        public Guid ProductId { get; set; }
        public string Sku { get; set; }
        public string Upc { get; set; }
        public string Category { get; set; }

        public string ReferenceNumber { get; set; }
        public string Reference1 { get; set; }
        public string Reference2 { get; set; }
        public string Reference3 { get; set; }

        public string SubstituteGroup { get; set; }
        public string Description { get; set; }
        public string Client { get; set; }
        public Guid? ClientId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string WarehouseCode { get; set; }
        public decimal? OpenQuantity { get; set; }
        public decimal? TotalQuantity { get; set; }
        public decimal? AllocatedQuantity { get; set; }
        public string UnitOfMeasure { get; set; }

        public string BinCode { get; set; }
        public string LicensePlateCode { get; set; }

        public string GetPrintText(Func<string, string> translate)
        {
            var result = BinCode;
            if (!string.IsNullOrEmpty(LicensePlateCode))
            {
                result = LicensePlateCode;
                if (!string.IsNullOrEmpty(BinCode))
                    result = $"{result}@{BinCode}";
            }

            return $"{result} - {translate($"Total [{TotalQuantity}], OnHand [{TotalQuantity - AllocatedQuantity}]")}";
        }
    }
}