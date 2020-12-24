using System;
using System.Net;

namespace Pro4Soft.iErpIntegration.Infrastructure
{
    public class BusinessWebException : Exception
    {
        public HttpStatusCode? Code { get; }
        public object Payload { get; }

        public BusinessWebException(HttpStatusCode? code) : this(code, null, null, null) { }
        public BusinessWebException(string message = null, object payload = null) : this(HttpStatusCode.NotAcceptable, message, payload, null) { }
        public BusinessWebException(HttpStatusCode? code, string message = null) : this(code, message, null, null)
        {
            Code = code;
        }

        public BusinessWebException(HttpStatusCode? code, string message, object payload, Exception innerException) : base(message, innerException)
        {
            Payload = payload;
            Code = code;
        }
    }
}
