using System.Threading.Tasks;

namespace YandexCloudApi
{
    /// <summary>
    /// An interface that describes a token provider.
    /// </summary>
    public interface IAuthorizer
    {
        /// <summary>
        /// Gets an IAM auth token.
        /// </summary>
        Task<string> GetAuthTokenAsync();

        /// <summary>
        /// Returns <code>True</code> when it's time to get
        /// a new auth token.
        /// </summary>
        bool NeedNewToken { get; }
    }
}