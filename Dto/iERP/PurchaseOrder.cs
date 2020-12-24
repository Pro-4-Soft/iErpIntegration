using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pro4Soft.iErpIntegration.Dto.iERP
{
    public class PurchaseOrder
    {
        [JsonProperty("OC_Numero")]
        public string PurchaseOrderNumber { get; set; }

        [JsonProperty("OC_Id_OrdenCompra")]
        public string ReferenceNumber { get; set; }

        [JsonProperty("OC_Fecha_Emision")]
        public DateTime? DateModified { get; set; }

        [JsonProperty("EN_Razon_Social")]
        public string VendorName { get; set; }

        [JsonProperty("Detalles")]
        public List<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
    }

    public class PurchaseOrderLine
    {
        [JsonProperty("PR_SKU")]
        public string Sku { get; set; }

        [JsonProperty("OD_Cantidad")]
        public decimal OrderedQuantity { get; set; }
    }
}
