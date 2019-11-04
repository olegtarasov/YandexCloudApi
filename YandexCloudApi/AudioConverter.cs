using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using YandexCloudApi.Logging;

namespace YandexCloudApi
{
    /// <summary>
    /// Converts between different audio formats.
    /// </summary>
    public class AudioConverter
    {
        private static readonly ILog _log = LogProvider.For<AudioConverter>();

        /// <summary>
        /// Converts raw PCM data without WAV header to
        /// OPUS stream packed into Ogg container.
        /// </summary>
        /// <param name="pcmData">Raw PCM data without WAV header.</param>
        /// <param name="format">PCM data format.</param>
        /// <returns>Ogg container with OPUS-encoded stream.</returns>
        public async Task<byte[]> ConvertPcmToOpusAsync(byte[] pcmData, WaveFormat format)
        {
            string pcmFile = Path.GetTempFileName();

            try
            {
                using (var writer = new WaveFileWriter(pcmFile, format))
                {
                    writer.Write(pcmData, 0, pcmData.Length);
                }

                return await ConvertWavToOpusAsync(pcmFile);
            }
            finally
            {
                File.Delete(pcmFile);
            }
        }

        /// <summary>
        /// Converts WAV file to OPUS stream packed into Ogg container.
        /// </summary>
        /// <param name="wavFile">Path to WAV file.</param>
        /// <returns>Ogg container with OPUS-encoded stream.</returns>
        public Task<byte[]> ConvertWavToOpusAsync(string wavFile)
        {
            return Task.Run(() =>
            {
                string oggFile = Path.GetTempFileName();

                var info = new ProcessStartInfo(GetOpusPath(), $"\"{wavFile}\" \"{oggFile}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardError = true
                };

                _log.Debug($"{info.FileName} {info.Arguments}");

                try
                {
                    var errorBuilder = new StringBuilder();
                    var process = Process.Start(info);
                    process.ErrorDataReceived += (sender, args) => errorBuilder.AppendLine(args.Data);
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        string err = errorBuilder.ToString();
                        _log.Debug($"opusenc.exe exited with code {process.ExitCode}. stderr:\n{err}");
                        throw new ConverterException($"opusenc.exe exited with code {process.ExitCode}", err);
                    }

                    var oggData = File.ReadAllBytes(oggFile);
                    if (oggData.Length == 0)
                    {
                        string err = errorBuilder.ToString();
                        _log.Debug($"opusenc.exe produced an empty file! stderr:\n{err}");
                        throw new ConverterException($"opusenc.exe produced an empty file!", errorBuilder.ToString());
                    }

                    _log.Debug("Converted successfully.");

                    return oggData;
                }
                catch (ConverterException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new ConverterException("Error occured while running opusenc.exe", e);
                }
                finally
                {
                    File.Delete(oggFile);
                }
            });
        }

        private string GetOpusPath()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(location))
            {
                location = Environment.CurrentDirectory;
            }
            else
            {
                location = Path.GetDirectoryName(location);
            }

            if (string.IsNullOrEmpty(location))
            {
                return "opusenc.exe"; // Pray for the best
            }

            return Path.Combine(location, "opusenc.exe");
        }
    }
}