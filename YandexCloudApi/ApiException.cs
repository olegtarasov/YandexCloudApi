using System;
using System.Net;

namespace YandexCloudApi
{
    /// <summary>
    /// REST API exception.
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// Ctor.
        /// </summary>
        public ApiException(HttpStatusCode code, string content) 
            : base($"Request failed with code {code.ToString()}")
        {  
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        public ApiException(Exception innerException) 
            : base($"Request failed with exception: {innerException.Message}", 
                innerException)
        { 
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        public ApiException(string message) : base(message)
        {
        }

        /// <summary>
        /// HTTP status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Response content.
        /// </summary>
        public string Content { get; set; }
    }
}