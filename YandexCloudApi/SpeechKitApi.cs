using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YandexCloudApi.Logging;

namespace YandexCloudApi
{
    /// <summary>
    /// SpeechKit API wrapper.
    /// </summary>
    public class SpeechKitApi
    {
        private static readonly ILog _log = LogProvider.For<SpeechKitApi>();

        private static readonly HashSet<int> _sampleRates = new HashSet<int> {48000, 16000, 8000};
        private static readonly Dictionary<AudioFormat, string> _formatMap =
            new Dictionary<AudioFormat, string>
            {
                {AudioFormat.PCM, "lpcm" },
                {AudioFormat.OggOpus, "oggopus" }
            };

        private static readonly Dictionary<Language, string> _languageMap =
            new Dictionary<Language, string>
            {
                {Language.Russian, "ru-RU" },
                {Language.English, "en-US" },
                {Language.Turkish, "tr-TR" }
            };


        private readonly IAuthorizer _authorizer;
        private readonly string _folderId;
        private readonly HttpClient _client = new HttpClient();

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="authorizer">Authorizer instance.</param>
        /// <param name="folderId">Cloud folder id.</param>
        public SpeechKitApi(IAuthorizer authorizer, string folderId)
        {
            _authorizer = authorizer;
            _folderId = folderId;
        }

        /// <summary>
        /// Recognizes text from audio data.
        /// </summary>
        /// <param name="data">Audio data.</param>
        /// <param name="format">Audio format.</param>
        /// <param name="language">Recognition language.</param>
        /// <param name="topic">Recognition topic.</param>
        /// <param name="filterProfanity">Filter out profanity.</param>
        /// <param name="sampleRate">Sample rate. Required when <see cref="format"/> is <see cref="AudioFormat.PCM"/>.</param>
        /// <returns>Recognized text.</returns>
        /// <exception cref="ApiException">API exception occured.</exception>
        public async Task<string> RecognizeTextAsync(
            byte[] data, 
            AudioFormat format, 
            Language language = Language.Russian,
            Topic topic = Topic.General,
            bool filterProfanity = false,
            int sampleRate = 48000)
        {
            if (format == AudioFormat.PCM && !_sampleRates.Contains(sampleRate))
            {
                throw new ArgumentException("Invalid sample rate!", nameof(sampleRate));
            }

            await RefreshToken();

            string url = $"https://stt.api.cloud.yandex.net/speech/v1/stt:recognize" +
                         $"?folderId={_folderId}" +
                         $"&lang={_languageMap[language]}" +
                         $"&topic={topic.ToString().ToLower()}" +
                         $"&profanityFilter={filterProfanity.ToString().ToLower()}" +
                         $"&format={_formatMap[format]}";

            if (format == AudioFormat.PCM)
            {
                url += $"&sampleRateHertz={sampleRate}";
            }

            _log.Debug($"Recognizing text: {url}");

            var watch = new Stopwatch();
            watch.Start();
            string response = await _client.MakeStringRequest(
                                client => client.PostAsync(url, new ByteArrayContent(data)));
            watch.Stop();

            string result = "";

            try
            {
                var jobj = JObject.Parse(response);
                result = jobj["result"].ToString();
            }
            catch (Exception e)
            {
                _log.Error(e, $"Failed to parse the response: {response}");
                throw new ApiException(e);
            }

            _log.Debug($"Recognized text: '{result}' in {watch.Elapsed.ToString()}");

            return result;
        }

        /// <summary>
        /// Synthesises speech from text.
        /// </summary>
        /// <param name="text">Text to synthesise.</param>
        /// <param name="format">Output audio format.</param>
        /// <param name="language">Language.</param>
        /// <param name="voice">Voice.</param>
        /// <param name="emotion">Emotion.</param>
        /// <param name="speed">Speed [0.1 .. 3.0].</param>
        /// <param name="sampleRate">Audio sample rate. Required when <see cref="format"/> is <see cref="AudioFormat.PCM"/>.</param>
        /// <returns>Synthesized audio data.</returns>
        /// <exception cref="ApiException">API exception occured.</exception>
        public async Task<byte[]> SynthesizeSpeechAsync(
            string text,
            AudioFormat format,
            Language language = Language.Russian,
            Voice voice = Voice.Oksana,
            Emotion emotion = Emotion.Neutral,
            float speed = 1.0f,
            int sampleRate = 48000
        )
        {
            if (speed < 0.1 || speed > 3.0)
            {
                throw new ArgumentException("Speed can be set in the range of [0.1 .. 3.0]", nameof(speed));
            }

            if (format == AudioFormat.PCM && !_sampleRates.Contains(sampleRate))
            {
                throw new ArgumentException("Invalid sample rate!", nameof(sampleRate));
            }

            await RefreshToken();

            var synthParams =
                new Dictionary<string, string>
                {
                    {"text", text},
                    {"lang", _languageMap[language]},
                    {"folderId", _folderId},
                    {"format", _formatMap[format]},
                    {"sampleRateHertz", sampleRate.ToString()},
                    {"voice", voice.ToString().ToLower()},
                    {"emotion", emotion.ToString().ToLower()},
                    {"speed", speed.ToString(NumberFormatInfo.InvariantInfo)}
                };
            var content = new FormUrlEncodedContent(
                synthParams);

            _log.Debug($"Synthesizing with params: {JsonConvert.SerializeObject(synthParams, Formatting.None)}");

            var watch = new Stopwatch();
            watch.Start();

            var result = await _client.MakeByteRequest(
                client => client.PostAsync("https://tts.api.cloud.yandex.net/speech/v1/tts:synthesize",
                    content));

            watch.Stop();

            if (result.Length == 0)
            {
                throw new ApiException("Speech synthesiser returned 0 bytes!");
            }

            _log.Debug($"Synthesized audio in {watch.Elapsed.ToString()}");

            return result;
        }

        private async Task RefreshToken()
        {
            if (string.IsNullOrEmpty(_client.DefaultRequestHeaders.Authorization?.Parameter)
                || _authorizer.NeedNewToken)
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _authorizer.GetAuthTokenAsync());
            }
        }
    }
}
