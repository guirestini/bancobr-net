using System;

namespace BancoBr.API.Core.Errors
{
    public abstract class BancoApiException : Exception
    {
        public int? HttpStatusCode { get; }

        protected BancoApiException(string message, int? httpStatusCode = null)
            : base(message)
        {
            HttpStatusCode = httpStatusCode;
        }

        protected BancoApiException(string message, Exception innerException, int? httpStatusCode = null)
            : base(message, innerException)
        {
            HttpStatusCode = httpStatusCode;
        }
    }
}
