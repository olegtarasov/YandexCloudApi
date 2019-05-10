using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using YandexCloudApi.Logging;

namespace YandexCloudApi
{
    /// <summary>
    /// An authorizer used to obtain IAM token for full
    /// (not service) account.
    /// </summary>
    public class FullAccountAuthorizer : IAuthorizer
    {
        private static readonly ILog _log = LogProvider.For<FullAccountAuthorizer>();
        private static readonly TimeSpan _tokenTimeout = TimeSpan.FromHours(11.9);

        private readonly HttpClient _client; 
        private readonly string _oauthToken;

        private string _iamToken = null;
        private DateTime _tokenRefreshTime = DateTime.MinValue;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="oauthToken">Account OAuth token.</param>
        public FullAccountAuthorizer(string oauthToken)
        {
            _oauthToken = oauthToken;
            _client = new HttpClient();
        }

        /// <inheritdoc />
        public async Task<string> GetAuthTokenAsync()
        {
            var now = DateTime.Now;
            if (NeedNewToken)
            {
                _tokenRefreshTime = DateTime.Now;
                _iamToken = await RefreshToken();
            }

            if (string.IsNullOrEmpty(_iamToken))
            {
                throw new ApiException("Failed to refresh IAM token!");
            }

            return _iamToken;
        }

        public bool NeedNewToken => DateTime.Now - _tokenRefreshTime >= _tokenTimeout || _iamToken == null;

        private async Task<string> RefreshToken()
        {
            _log.Info("Refreshing IAM token for full account.");
            string response = await _client.MakeStringRequest(
                       client => client.PostAsync(
                           "https://iam.api.cloud.yandex.net/iam/v1/tokens",
                           ApiHelper.AppJsonContent(
                               $"{{\"yandexPassportOauthToken\": \"{_oauthToken}\"}}"
                           )
                       )
                   );

            try
            {
                var jobj = JObject.Parse(response);
                return jobj["iamToken"].ToString();
            }
            catch (Exception e)
            {
                throw new ApiException(e);
            }
        }
    }
}