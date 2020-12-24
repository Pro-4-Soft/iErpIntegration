using Newtonsoft.Json;

namespace Pro4Soft.iErpIntegration.Dto.iERP
{
    public class Product
    {
        [JsonProperty("PR_Codigo")]
        public string Code { get; set; }

        [JsonProperty("PR_Nombre_Producto")]
        public string Description { get; set; }

        [JsonProperty("PR_SKU")]
        public string Sku { get; set; }

        [JsonProperty("PR_Status_Producto")]
        public string Status { get; set; }

        [JsonProperty("PC_Categoria")]
        public string Category { get; set; }
    }
}
