using System;

namespace Pro4Soft.iErpIntegration.Dto.P4W
{
    public class IdObject
    {
        public Guid Id { get; set; }
    }

    public class Client : IdObject
    {
        public string Name { get; set; }
    }

    public class TenantDetails: IdObject
    {
        public string CompanyName { get; set; }
        public string Alias { get; set; }
        public string Phone { get; set; }
        public string LanguageId { get; set; }
        public string WelcomeMessage { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string ZipPostalCode { get; set; }
        public string StateProvince { get; set; }
        public string Country { get; set; }

        public DateTimeOffset? LicenseStart { get; set; }
        public DateTimeOffset? LicenseEnd { get; set; }
        public int ConcurrentConnections { get; set; }
    }
}
