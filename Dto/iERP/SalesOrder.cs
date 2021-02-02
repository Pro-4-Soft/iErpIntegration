using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pro4Soft.iErpIntegration.Dto.iERP
{
    public class SalesOrder
    {
        [JsonProperty("CZ_Numero")]
        public string SalesOrderNumber { get; set; }

        [JsonProperty("CZ_Referencia")]
        public string ReferenceNumber { get; set; }

        [JsonProperty("CZ_Fecha_Emision")]
        public DateTime? DateCreated { get; set; }

        [JsonProperty("EN_Razon_Social")]
        public string CustomerName { get; set; }

        [JsonProperty("Email")]
        public string Email { get; set; }

        [JsonProperty("Detalles")]
        public List<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
    }

    public class SalesOrderLine
    {
        [JsonProperty("PR_SKU")]
        public string Sku { get; set; }

        [JsonProperty("CL_Cantidad")]
        public decimal OrderedQuantity { get; set; }
    }
}
