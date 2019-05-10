using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace YandexCloudApi
{
    /// <summary>
    /// Converts between different sound formats.
    /// </summary>
    public class SoundConverter
    {
        /// <summary>
        /// Converts raw PCM data without WAV header to
        /// OPUS stream packed into Ogg container.
        /// </summary>
        /// <param name="pcmData">Raw PCM data without WAV header.</param>
        /// <param name="format">PCM data format.</param>
        /// <returns>Ogg container with OPUS-encoded stream.</returns>
        public Task<byte[]> ConvertPcmToOpusAsync(byte[] pcmData, WaveFormat format)
        {
            return Task.Run(() =>
            {
                string pcmFile = Path.GetTempFileName();
                string oggFile = Path.GetTempFileName();

                using (var writer = new WaveFileWriter(pcmFile, format))
                {
                    writer.Write(pcmData, 0, pcmData.Length);
                }

                var info = new ProcessStartInfo(GetOpusPath(), $"{pcmFile} {oggFile}")
                           {
                               UseShellExecute = false,
                               RedirectStandardError = true
                           };

                try
                {
                    var errorBuilder = new StringBuilder();
                    var process = Process.Start(info);
                    process.ErrorDataReceived += (sender, args) => errorBuilder.AppendLine(args.Data);
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new ConverterException($"opusenc.exe exited with code {process.ExitCode}", errorBuilder.ToString());
                    }

                    var oggData = File.ReadAllBytes(oggFile);
                    if (oggData.Length == 0)
                    {
                        throw new ConverterException($"opusenc.exe produced an empty file!", errorBuilder.ToString());
                    }

                    return oggData;
                }
                catch (ConverterException e)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new ConverterException("Error occured while running opusenc.exe", e);
                }
                finally
                {
                    File.Delete(pcmFile);
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