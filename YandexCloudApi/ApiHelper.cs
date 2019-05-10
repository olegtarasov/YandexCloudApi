using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using YandexCloudApi.Logging;

namespace YandexCloudApi
{
    public static class ApiHelper
    {
        private static readonly ILog _log = LogProvider.GetLogger(typeof(ApiHelper));

        public static Task<byte[]> MakeByteRequest(
            this HttpClient client,
            Func<HttpClient, Task<HttpResponseMessage>> requestFunc)
        {
            return MakeRequest(
                client, 
                requestFunc, 
                response => response.Content?.ReadAsByteArrayAsync());
        }

        public static Task<string> MakeStringRequest(
            this HttpClient client,
            Func<HttpClient, Task<HttpResponseMessage>> requestFunc)
        {
            return MakeRequest(client, requestFunc, response => response.Content?.ReadAsStringAsync());
        }

        public static async Task<T> MakeRequest<T>(
            this HttpClient client,
            Func<HttpClient, Task<HttpResponseMessage>> requestFunc,
            Func<HttpResponseMessage, Task<T>> contentFunc)
        {
            try
            {
                var response = await requestFunc(client);

                if (!response.IsSuccessStatusCode)
                {
                    string content;
                    if (response.Content == null)
                    {
                        content = "<Content is empty>";
                    }
                    else
                    {
                        try
                        {
                            content = await response.Content.ReadAsStringAsync();
                        }
                        catch
                        {
                            content = "<There was an exception reading response stream>";
                        }
                    }

                    _log.Debug($"Request failed with code: {response.StatusCode}.\nFull request:\n{response.RequestMessage.ToString()}.\nFull response:\n{response.ToString()}\nContent:\n{content}");

                    throw new ApiException(response.StatusCode, content);
                }

                return await contentFunc(response);
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception e)
            {
                _log.Debug(e, "Request failed with exception");
                throw new ApiException(e);
            }
        }

        public static StringContent AppJsonContent(string content)
        {
            return new StringContent(
                content,
                Encoding.UTF8,
                "application/json");
        }
    }
}