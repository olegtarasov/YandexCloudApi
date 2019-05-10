using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace YandexCloudApi
{
    public static class ApiHelper
    {
        public static Task<byte[]> MakeByteRequest(
            this HttpClient client,
            Func<HttpClient, Task<HttpResponseMessage>> requestFunc)
        {
            return MakeRequest(client, requestFunc, response => response.Content.ReadAsByteArrayAsync());
        }

        public static Task<string> MakeStringRequest(
            this HttpClient client,
            Func<HttpClient, Task<HttpResponseMessage>> requestFunc)
        {
            return MakeRequest(client, requestFunc, response => response.Content.ReadAsStringAsync());
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
                    try
                    {
                        content = await response.Content.ReadAsStringAsync();
                    }
                    catch
                    {
                        content = "<There was an exception reading response stream>";
                    }

                    throw new ApiException(response.StatusCode, content);
                }

                return await contentFunc(response);
            }
            catch (Exception e)
            {
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