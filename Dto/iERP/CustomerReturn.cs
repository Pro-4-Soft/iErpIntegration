using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pro4Soft.iErpIntegration.Dto.iERP
{
    public class CustomerReturn
    {
        [JsonProperty("DV_Numero")]
        public string CustomerReturnNumber { get; set; }

        [JsonProperty("DV_Id_Devolucion")]
        public string ReferenceNumber { get; set; }

        [JsonProperty("DV_Fecha_Emision")]
        public DateTime? DateModified { get; set; }

        [JsonProperty("EN_Razon_Social")]
        public string CustomerName { get; set; }

        [JsonProperty("EN_Id_Cliente")]
        public string CustomerCode { get; set; }

        [JsonProperty("ViewModelState")]
        public int State { get; set; }

        [JsonProperty("ViewModelMessagge")]
        public string Messagge { get; set; }

        [JsonProperty("Detalles")]
        public List<CustomerReturnLine> Lines { get; set; } = new List<CustomerReturnLine>();
    }

    public class CustomerReturnLine
    {
        [JsonProperty("DDV_SKU")]
        public string Sku { get; set; }

        [JsonProperty("DDV_Cantidad")]
        public decimal Quantity { get; set; }
    }
}