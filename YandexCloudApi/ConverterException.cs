using System;

namespace YandexCloudApi
{
    /// <summary>
    /// Sound converter exception.
    /// </summary>
    public class ConverterException : Exception
    {
        /// <summary>
        /// Ctor.
        /// </summary>
        public ConverterException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ConverterException(string message, string errorOutput) : base(message)
        {
            ErrorOutput = errorOutput;
        }

        /// <summary>
        /// Converter error output.
        /// </summary>
        public string ErrorOutput { get; set; }
    }
}