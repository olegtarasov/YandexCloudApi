using System;
using System.IO;
using System.Text;
using System.Threading;
using CLAP;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using Serilog;
using YandexCloudApi;

namespace ApiTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.InputEncoding = Encoding.Unicode;

            var config = new LoggerConfiguration()
                         .MinimumLevel.Debug()
                         .WriteTo.Console();

            Log.Logger = config.CreateLogger();

            Parser.Run(args, new App());

#if DEBUG
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
#endif
        }
    }

    public class App
    {
        private static readonly ILogger _log = Log.ForContext<App>();

        private readonly IConfigurationRoot _configuration;
        private readonly FullAccountAuthorizer _fullAuthorizer;

        public App()
        {
            _configuration = new ConfigurationBuilder().AddUserSecrets<App>().Build();
            _fullAuthorizer = new FullAccountAuthorizer(_configuration["OAuthToken"]);
        }

        [Verb]
        public void SpeechRecognition()
        {
            _log.Information("Testing speech recognition.");
            _log.Information("Press any key when ready to speak. Then press any key again to stop recording.");
            using var stream = new MemoryStream();
            var waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(48000, 16, 1);
            waveIn.DataAvailable += (sender, args) => stream.Write(args.Buffer, 0, args.BytesRecorded);

            Console.ReadKey();
            _log.Information("Recording. Press any key when ready.");
            waveIn.StartRecording();
            Console.ReadKey();

            waveIn.StopRecording();
            waveIn.Dispose();
            _log.Information("Recording stopped.");

            var data = stream.ToArray();
            var speechKit = new SpeechKitApi(_fullAuthorizer, _configuration["FolderId"]);

            _log.Information("Testing with PCM format.");

            try
            {
                string text = speechKit.RecognizeTextAsync(data, AudioFormat.PCM).Result;
                _log.Information($"Recognized text: {text}");
            }
            catch (Exception e)
            {
                _log.Error(e, "Recognition failed");
            }

            _log.Information("Converting to OPUS");

            byte[] oggData;
            try
            {
                oggData = new AudioConverter().ConvertPcmToOpusAsync(data, waveIn.WaveFormat).Result;
            }
            catch (Exception e)
            {
                _log.Error(e, "Conversion failed");
                return;
            }

            try
            {
                string text = speechKit.RecognizeTextAsync(oggData, AudioFormat.OggOpus).Result;
                _log.Information($"Recognized text: {text}");
            }
            catch (Exception e)
            {
                _log.Error(e, "Recognition failed");
            }
        }

        [Verb]
        public void TextToSpeech()
        {
            _log.Information("Testing speech synthesis. WARNING! Synthesised text wull be played out on default audio device!");
            _log.Information("Enter text to synthesize, then press Enter.");
            string text = Console.ReadLine();

            if (string.IsNullOrEmpty(text))
            {
                _log.Error("Empty text!");
                return;
            }

            var speechKit = new SpeechKitApi(_fullAuthorizer, _configuration["FolderId"]);
            var data = speechKit.SynthesizeSpeechAsync(
                text, 
                AudioFormat.PCM,
                speed: 0.8f,
                emotion: Emotion.Good).Result;

            _log.Information("Playing back synthesised text.");

            var evt = new ManualResetEventSlim();
            var waveOut = new WaveOutEvent();
            var provider = new RawSourceWaveStream(data, 0, data.Length, new WaveFormat(48000, 16, 1));
            waveOut.PlaybackStopped += (sender, args) => evt.Set();
            waveOut.Init(provider);
            waveOut.Play();

            evt.Wait();

            _log.Information("Playback finished.");
        }
    }
}
