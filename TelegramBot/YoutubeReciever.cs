using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Common;

namespace YoutubeConnect
{
    public struct VideoInfo : IEnumerable
    {
        public string Title { get; set; }

        public string Channel { get; set; }

        public TimeSpan Duration { get; set; }

        public string Description { get; set; }
        public string Id { get; set; }

        public IEnumerator GetEnumerator()
        {
            yield return Title;
            yield return Channel;
            yield return Duration;
            yield return Description;
            yield return Id;
        }
    }

    public class YoutubeReciever
    {
        private readonly YoutubeClient _youtube;
        static private readonly HttpClient _httpClient;

        static YoutubeReciever()
        {
            _httpClient = new HttpClient();
        }

        public YoutubeReciever()
        {
            _youtube = new YoutubeClient();
        }

        public async Task<VideoInfo?> GetVideoInfoAsync(string url)
        {
            try
            {
                var video = await _youtube.Videos.GetAsync(url); // URL
                return new VideoInfo
                {
                    Title = video.Title ?? "",
                    Channel = video?.Author?.ChannelTitle ?? "",
                    Duration = video?.Duration.Value ?? TimeSpan.Zero,
                    Description = video?.Description.Length > 100 ? video.Description.Substring(0, 100) : video.Description ?? "",
                    Id = video.Id,
                };
            }
            catch (PlaylistUnavailableException)
            {
                return null;
            }
            catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
            {
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<MemoryStream?> GetVideoPreviewStreamAsync(string url)
        {
            try
            {
                var video = await _youtube.Videos.GetAsync(url);
                var thumbnail = video.Thumbnails.GetWithHighestResolution();

                // Creating stream thumbnail by url
                using var response = await _httpClient.GetAsync(thumbnail.Url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                var ms = new MemoryStream();
                await response.Content.CopyToAsync(ms);
                ms.Position = 0;
                return ms;
            }
            catch (PlaylistUnavailableException)
            {
                return null;
            }
            catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
            {
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<FileStream?> GetVideoMuxedStreamAsync(string url)
        {
            try
            {
                // Get manifest 
                var manifest = await _youtube.Videos.Streams.GetManifestAsync(url);
                // Select high quality
                var streamInfo = manifest.GetMuxedStreams().OrderByDescending(s => s.VideoQuality.MaxHeight).FirstOrDefault();

                if (streamInfo == null) return null;

                // Temporary file for downloading initialization
                var tempPath = Path.GetTempFileName();

                // Downloading video
                await _youtube.Videos.Streams.DownloadAsync(streamInfo, tempPath);

                // Gave the stream
                var fileStream = File.OpenRead(tempPath);
                fileStream.Position = 0;

                return fileStream;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
