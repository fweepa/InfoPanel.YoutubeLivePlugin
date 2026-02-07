using InfoPanel.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace YouTubeLivePlugin
{
    /// <summary>
    /// Plugin for displaying YouTube Live Streams in InfoPanel via the image item's URL type.
    /// Extracts HLS stream URL using yt-dlp and provides it for HttpImageDisplayItem binding.
    /// </summary>
    public class YouTubeLivePlugin : BasePlugin
    {
        private readonly PluginText _streamUrl = new("stream-url", "Stream URL", "");
        private readonly PluginText _status = new("status", "Status", "Not configured");

        private string? _cachedHlsUrl;
        private DateTime _lastUrlFetchUtc = DateTime.MinValue;
        private static readonly TimeSpan UrlCacheDuration = TimeSpan.FromMinutes(30);

        private const string SectionYouTube = "YouTube";
        private const string KeyUrl = "Url";

        public YouTubeLivePlugin()
            : base("youtube-live", "YouTube Live Stream", "Provides YouTube Live Stream HLS URL for display in InfoPanel")
        {
        }

        public override string? ConfigFilePath =>
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "config.ini");

        public override TimeSpan UpdateInterval => TimeSpan.FromMinutes(5);

        public override void Initialize()
        {
            // Ensure config exists with defaults
            EnsureConfigExists();
        }

        public override void Load(List<IPluginContainer> containers)
        {
            var container = new PluginContainer("youtube", "YouTube Live");
            container.Entries.Add(_streamUrl);
            container.Entries.Add(_status);
            containers.Add(container);
        }

        public override void Update()
        {
            throw new NotImplementedException();
        }

        public override async Task UpdateAsync(CancellationToken cancellationToken)
        {
            try
            {
                string? youtubeUrl = LoadConfigUrl();
                if (string.IsNullOrWhiteSpace(youtubeUrl))
                {
                    _streamUrl.Value = "";
                    _status.Value = "Not configured - add URL to config.ini";
                    return;
                }

                if (!IsValidYouTubeUrl(youtubeUrl))
                {
                    _streamUrl.Value = "";
                    _status.Value = "Invalid YouTube URL";
                    return;
                }

                // Use cached URL if still valid
                if (!string.IsNullOrEmpty(_cachedHlsUrl) && DateTime.UtcNow - _lastUrlFetchUtc < UrlCacheDuration)
                {
                    _streamUrl.Value = _cachedHlsUrl;
                    _status.Value = "Playing";
                    return;
                }

                string? hlsUrl = await ExtractHlsUrlAsync(youtubeUrl, cancellationToken);
                if (!string.IsNullOrEmpty(hlsUrl))
                {
                    _cachedHlsUrl = hlsUrl;
                    _lastUrlFetchUtc = DateTime.UtcNow;
                    _streamUrl.Value = hlsUrl;
                    _status.Value = "Playing";
                }
                else
                {
                    // Keep last known URL if extraction failed (stream might be briefly unavailable)
                    if (!string.IsNullOrEmpty(_cachedHlsUrl))
                    {
                        _streamUrl.Value = _cachedHlsUrl;
                        _status.Value = "Playing (cached)";
                    }
                    else
                    {
                        _streamUrl.Value = "";
                        _status.Value = _lastError ?? "Failed to extract stream URL";
                    }
                }
            }
            catch (Exception ex)
            {
                _streamUrl.Value = _cachedHlsUrl ?? "";
                _status.Value = $"Error: {ex.Message}";
            }
        }

        private string? _lastError;

        private async Task<string?> ExtractHlsUrlAsync(string youtubeUrl, CancellationToken cancellationToken)
        {
            _lastError = null;

            try
            {
                // Try HLS format first (best for live streams), then fallback to best
                var formatArg = "best[protocol^=m3u8_native]/best[protocol^=m3u8]/best";
                var result = await RunYtDlpAsync(youtubeUrl, formatArg, cancellationToken);
                if (!string.IsNullOrEmpty(result))
                {
                    return result.Trim();
                }

                // Fallback: any best format
                result = await RunYtDlpAsync(youtubeUrl, null, cancellationToken);
                return !string.IsNullOrEmpty(result) ? result.Trim() : null;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
            {
                _lastError = "yt-dlp not found - please install (winget install yt-dlp.yt-dlp)";
                return null;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return null;
            }
        }

        private static async Task<string?> RunYtDlpAsync(string url, string? formatFilter, CancellationToken cancellationToken)
        {
            var args = "-g --no-warnings";
            if (!string.IsNullOrEmpty(formatFilter))
            {
                args += $" -f \"{formatFilter}\"";
            }
            args += $" \"{url}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);
            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"yt-dlp failed: {error.Trim()}");
            }

            return string.IsNullOrWhiteSpace(output) ? null : output;
        }

        private static bool IsValidYouTubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return Regex.IsMatch(url, @"^(https?://)?(www\.)?(youtube\.com/live/|youtube\.com/watch\?v=|youtu\.be/)[\w-]+", RegexOptions.IgnoreCase);
        }

        private void EnsureConfigExists()
        {
            var path = ConfigFilePath;
            if (path == null || File.Exists(path)) return;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var content = $"[{SectionYouTube}]\r\n{KeyUrl}=\r\n";
            File.WriteAllText(path, content);
        }

        private string? LoadConfigUrl()
        {
            var path = ConfigFilePath;
            if (path == null || !File.Exists(path)) return null;

            try
            {
                var lines = File.ReadAllLines(path);
                string? currentSection = null;
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        currentSection = trimmed[1..^1];
                        continue;
                    }

                    if (currentSection == SectionYouTube && trimmed.Contains('='))
                    {
                        var eq = trimmed.IndexOf('=');
                        var key = trimmed[..eq].Trim();
                        var value = trimmed[(eq + 1)..].Trim();
                        if (key.Equals(KeyUrl, StringComparison.OrdinalIgnoreCase))
                        {
                            return string.IsNullOrWhiteSpace(value) ? null : value;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        public override void Close()
        {
            _cachedHlsUrl = null;
        }
    }
}
