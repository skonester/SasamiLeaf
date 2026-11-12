using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;

using FlyleafLib.MediaFramework.MediaPlaylist;
using FlyleafLib.MediaFramework.MediaStream;

using static FlyleafLib.Utils;

namespace FlyleafLib.Plugins;

public class YoutubeDL : PluginBase, IOpen, ISuggestExternalAudio, ISuggestExternalVideo
{
    /* TODO
     * 1) Check Audio streams if we need to add also video streams with audio
     * 2) Check Best Audio bitrates/quality (mainly for audio only player)
     * 3) Dispose ytdl and not tag it to every item (use only format if required)
     * 4) Use playlist_index to set the default playlist item
     * 5) Video with audio has the language of the audio (this makes the GetBestMatch video to take count also the audio but we don't know if it is enabled/required)
     * 6) Review supported subtitles formats (allow more/other than vtt?) - when they will work again
     * 
     * Lot of issues with yt-dlp currently:
     * - Cookies can't be exported anymore (?)
     * - Brings playlists when it shouldn't
     * - Double entries for formats (w/o "default" - youtube only?) (clean-up here based on format's main-unique fields / urls?)
     * - Subtitles 429 errors - https://github.com/yt-dlp/yt-dlp/issues/13831
     * - Audio languages are new thing? Creates different streams for those (instead of single with embedded streams?)
     */

    public new int      Priority        { get; set; } = 1999;
    static string       plugin_path     = "yt-dlp.exe";
    static JsonSerializerOptions
                        jsonSettings    = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, Converters = { new NullNumberConverter() } };

    Process             proc;
    object              procLocker = new();
    long                sessionId = -1; // same for playlists

    public override Dictionary<string, string> GetDefaultOptions()
        => new()
        {
            { "ExtraArguments", "" } // TBR: Restore default functionality with --cookies-from-browser {defaultBrowser} || https://github.com/yt-dlp/yt-dlp/issues/7271
        };

    public override void OnInitializing()
        => DisposeInternal();

    public override void Dispose()
        => DisposeInternal();

    private Format GetAudioOnly(YoutubeDLJson ytdl)
    {
        string videoProtocol = Selected != null && ReferenceEquals(GetTag(Selected), ytdl)
            ? Selected.ExternalVideoStream?.Protocol : null;
        videoProtocol ??= GetBestMatch(ytdl)?.protocol;
        return SelectAudioFormat(ytdl.formats, Config.Audio.Languages, videoProtocol);
    }

    internal static Format SelectAudioFormat(IEnumerable<Format> formats, IEnumerable<Language> languages, string videoProtocol)
    {
        // yt-dlp orders formats from worst to best. Keep that order for formats
        // with unknown bitrates, including YouTube's low/high HLS audio variants.
        var candidates = formats.Where(x => !string.IsNullOrEmpty(x.url) && HasAudio(x)).Reverse().ToList();
        bool IsHls(string protocol) => protocol?.StartsWith("m3u8", StringComparison.OrdinalIgnoreCase) == true;
        var ranked = candidates.OrderByDescending(x => !HasVideo(x))
            .ThenByDescending(x => videoProtocol != null && IsHls(x.protocol) == IsHls(videoProtocol))
            .ThenByDescending(x => x.language_preference).ToList();

        foreach (var language in languages)
        {
            var match = ranked.FirstOrDefault(x => Language.Get(x.language) == language);
            if (match != null)
                return match;
        }
        return ranked.FirstOrDefault();
    }
    private Format GetBestMatch(YoutubeDLJson ytdl)
    {
        // Video Streams Order based on Screen Resolution
        var iresults =
            from    format in ytdl.formats

            where
                HasVideo(format) &&
                format.height <= Config.Video.MaxVerticalResolution &&
                (!HasAudio(format) || Config.Audio.Languages.Contains(Language.Get(format.language))) && // TBR: Does not respect audio language priority
                (!Regex.IsMatch(format.protocol, "dash", RegexOptions.IgnoreCase) || format.vcodec.Equals("vp9", StringComparison.OrdinalIgnoreCase))

            orderby format.width    descending,
                    format.height   descending,
                    format.protocol descending, // prefer m3u8 over https
                    format.vcodec,              // prefer avc over vp09 (because YT can't seek vp09 at all)
                    format.tbr      descending,
                    format.fps      descending

            select  format;

        if (iresults == null || !iresults.Any())
        {
            // Fall-back to any
            iresults =
                from    format in ytdl.formats
                where   HasVideo(format)
                orderby format.width    descending,
                        format.height   descending,
                        format.protocol descending,
                        format.vcodec,
                        format.tbr      descending,
                        format.fps      descending
                select  format;

            if (iresults == null || !iresults.Any())
                return null;
        }

        List<Format> results = [.. iresults];

        // Best Resolution
        double bestWidth = results[0].width;
        double bestHeight = results[0].height;

        // Choose from the best resolution (0. with acodec and not blacklisted 1. not blacklisted 2. any)
        int priority = 0;
        while (priority < 3)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].width != bestWidth || results[i].height != bestHeight)
                    break;

                if (priority == 0 && results[i].acodec != "none")
                    return results[i];
                else if (priority == 1)
                    return results[i];
                else if (priority == 2)
                    return results[i];
            }

            priority++;
        }

        return results[^1]; // Fall-back to any
    }
    
    private static bool HasVideo(Format fmt)
    {
        if (fmt.height > 0 || fmt.vbr > 0 || (!string.IsNullOrEmpty(fmt.vcodec) && fmt.vcodec != "none"))
            return true;

        return false;
    }
    internal static bool HasAudio(Format fmt)
    {
        // An explicit "none" means silent video or a storyboard. Missing codec
        // metadata is different: HLS audio can still be labelled "audio only".
        if (string.Equals(fmt.acodec, "none", StringComparison.OrdinalIgnoreCase))
            return false;
        if (fmt.abr > 0 || (!string.IsNullOrEmpty(fmt.acodec) && fmt.acodec != "none"))
            return true;

        return fmt.audio_channels > 0
            || (!string.IsNullOrEmpty(fmt.audio_ext) && fmt.audio_ext != "none")
            || string.Equals(fmt.resolution, "audio only", StringComparison.OrdinalIgnoreCase);
    }

    private void DisposeInternal()
    {
        lock (procLocker)
        {
            if (Disposed)
                return;

            sessionId       = -1;
            if (proc != null)
            {
                try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception e) { Log.Warn($"Unable to stop yt-dlp ({e.Message})"); }
                proc.Dispose();
                proc = null;
            }

            Disposed = true;
        }
    }

    private void NewPlaylistItem(string json)
    {
        YoutubeDLJson ytdl = null;

        try
        {
            ytdl = JsonSerializer.Deserialize<YoutubeDLJson>(json, jsonSettings);
        } catch (Exception e)
        {
            Log.Error($"[JsonSerializer] {e.Message}");
        }

        if (sessionId != Handler.OpenCounter) return;

        if (ytdl == null)
            return;

        if (ytdl._type == "playlist")
            return;

        PlaylistItem item = new();

        if (Playlist.ExpectingItems == 0)
            Playlist.ExpectingItems = (int)ytdl.playlist_count;

        if (Playlist.Title == null)
        {
            if (!string.IsNullOrEmpty(ytdl.playlist_title))
            {
                Playlist.Title = ytdl.playlist_title;
                Log.Debug($"Playlist Title -> {Playlist.Title}");
            }
            else if (!string.IsNullOrEmpty(ytdl.playlist))
            {
                Playlist.Title = ytdl.playlist;
                Log.Debug($"Playlist Title -> {Playlist.Title}");
            }
        }

        item.Title = ytdl.title;
        Log.Debug($"Adding {item.Title}");

        item.DirectUrl = ytdl.webpage_url;

        // If no formats still could have a single format attched to the main root class
        if (ytdl.formats == null)
            ytdl.formats = [ytdl];

        // Audio / Video Streams
        for (int i = 0; i < ytdl.formats.Count; i++)
        {
            if (sessionId != Handler.OpenCounter)
                return;

            Format fmt = ytdl.formats[i];

            if (fmt.vcodec == null)
                fmt.vcodec = "";

            if (fmt.acodec == null)
                fmt.acodec = "";

            if (fmt.protocol == null)
                fmt.protocol = "";

            bool hasAudio = HasAudio(fmt);
            bool hasVideo = HasVideo(fmt);

            if (!hasVideo && !hasAudio)
                continue;

            ExternalStream extStream;

            if (hasVideo)
            {
                extStream = new ExternalVideoStream()
                {
                    Url         = fmt.url,
                    UrlFallback = string.IsNullOrEmpty(fmt.manifest_url) ? ytdl.manifest_url : fmt.manifest_url,
                    Protocol    = fmt.protocol,
                    HasAudio    = hasAudio,
                    BitRate     = (long)fmt.vbr,
                    Codec       = fmt.vcodec,
                    //Language = Language.Get(fmt.language), // TBR: this is audio language
                    Width       = (int)fmt.width,
                    Height      = (int)fmt.height,
                    FPS         = fmt.fps
                };
            }
            else
            {
                extStream = new ExternalAudioStream()
                {
                    Url         = fmt.url,
                    UrlFallback = string.IsNullOrEmpty(fmt.manifest_url) ? ytdl.manifest_url : fmt.manifest_url,
                    Protocol    = fmt.protocol,
                    BitRate     = (long)fmt.abr,
                    Codec       = fmt.acodec,
                    Language    = Language.Get(fmt.language)
                };
            }

            AddHeaders(extStream, fmt);
            AddExternalStream(extStream, fmt, item);
        }

        if (GetBestMatch(ytdl) == null && GetAudioOnly(ytdl) == null)
        {
            Log.Warn("No streams found");
            return;
        }

        // Subtitles Streams
        try
        {
            if (ytdl.automatic_captions != null)
            {
                bool found = false;
                Language lang;

                foreach (var subtitle1 in ytdl.automatic_captions)
                {
                    if (sessionId != Handler.OpenCounter)
                        return;

                    lang = Language.Get(subtitle1.Key);
                    if (!Config.Subtitles.Languages.Contains(lang))
                        continue;

                    foreach (var subtitle in subtitle1.Value)
                    {
                        if (!subtitle.ext.Equals("vtt", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (Language.Get(subtitle1.Key) == Config.Subtitles.Languages[0])
                            found = true;

                        AddExternalStream(new ExternalSubtitlesStream()
                        {
                            Downloaded  = true,
                            Converted   = true,
                            Protocol    = subtitle.ext,
                            Language    = lang,
                            Url         = subtitle.url
                        }, null, item);
                    }
                }

                if (!found) // TBR: There are still subtitles converted from one language to another (eg. Spanish from English, es-en)
                {
                    foreach (var subtitle1 in ytdl.automatic_captions)
                    {
                        var minusPos = subtitle1.Key.IndexOf('-');
                        lang = minusPos > 1 ? Language.Get(subtitle1.Key[..minusPos]) : null;
                        if (lang != Config.Subtitles.Languages[0])
                            continue;

                        foreach (var subtitle in subtitle1.Value)
                        {
                            if (sessionId != Handler.OpenCounter)
                                return;

                            if (!subtitle.ext.Equals("vtt", StringComparison.OrdinalIgnoreCase))
                                continue;

                            AddExternalStream(new ExternalSubtitlesStream()
                            {
                                Downloaded  = true,
                                Converted   = true,
                                Protocol    = subtitle.ext,
                                Language    = lang,
                                Url         = subtitle.url
                            }, null, item);
                        }
                    }
                }
            }
        } catch (Exception e) { Log.Warn($"Failed to add subtitles ({e.Message})"); }

        AddPlaylistItem(item, ytdl);
    }
    public void AddHeaders(ExternalStream extStream, Format fmt)
    {
        if (fmt.http_headers != null)
        {
            if (fmt.http_headers.TryGetValue("User-Agent", out string value))
            {
                extStream.UserAgent = value;
                fmt.http_headers.Remove("User-Agent");
            }

            if (fmt.http_headers.TryGetValue("Referer", out value))
            {
                extStream.Referrer = value;
                fmt.http_headers.Remove("Referer");
            }

            extStream.HTTPHeaders = new(fmt.http_headers, StringComparer.OrdinalIgnoreCase);
        }
        if (!string.IsNullOrEmpty(fmt.cookies))
        {
            extStream.HTTPHeaders ??= new();
            extStream.HTTPHeaders["Cookie"] = fmt.cookies;
        }
    }

    public bool CanOpen()
    {
        try
        {
            if (Playlist.IOStream != null)
                return false;

            Uri uri = new(Playlist.Url);
            string scheme = uri.Scheme.ToLower();

            if (scheme != "http" && scheme != "https")
                return false;

            string ext = GetUrlExtention(uri.AbsolutePath);

            if (new[] { "m3u8", "m3u", "mpd", "pls", "mp4", "mkv", "webm", "avi", "mov", "ts", "mp3", "m4a", "aac", "flac", "wav", "ogg", "opus" }.Contains(ext, StringComparer.OrdinalIgnoreCase))
                return false;

            // TBR: try to avoid processing radio stations
            if (string.IsNullOrEmpty(uri.PathAndQuery) || uri.PathAndQuery.Length < 5)
                return false;

        } catch (Exception) { return false; }

        return true;
    }
    public OpenResults Open()
    {
        long currentSession = Handler.OpenCounter;
        bool outputDone = false;
        string extractionError = null;
        try
        {
            Process process;
            lock (procLocker)
            {
                string executable = Path.Combine(Engine.Plugins.Folder, Name, plugin_path);
                if (!File.Exists(executable))
                    return new("Web playback requires yt-dlp.exe. Rebuild with tools/Get-WebPlaybackTools.ps1 or reinstall SasamiLeaf.");

                Disposed = false;
                sessionId = currentSession;
                Playlist.InputType = InputType.Web;
                process = proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = executable,
                        // JSON on stdout avoids filename, file-watcher and process-exit races.
                        Arguments = $"{Options["ExtraArguments"]} --ignore-config --skip-download --dump-json --no-playlist --socket-timeout 20 --retries 1 -- {QuoteArgument(Playlist.Url)}",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8,
                    }
                };
                process.OutputDataReceived += (o, e) =>
                {
                    if (e.Data == null)
                    {
                        Volatile.Write(ref outputDone, true);
                        return;
                    }
                    if (sessionId != currentSession || currentSession != Handler.OpenCounter)
                        return;
                    try { NewPlaylistItem(e.Data); }
                    catch (Exception error) { Log.Warn($"Invalid yt-dlp result ({error.Message})"); }
                };
                process.ErrorDataReceived += (o, e) =>
                {
                    if (e.Data == null || sessionId != currentSession)
                        return;
                    Log.Debug($"[yt-dlp] {e.Data}");
                    if (e.Data.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
                        extractionError = e.Data;
                };
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
            }

            while (Playlist.Items.Count == 0 && !Volatile.Read(ref outputDone) && sessionId == currentSession && currentSession == Handler.OpenCounter)
                Thread.Sleep(35);

            if (sessionId != currentSession || currentSession != Handler.OpenCounter)
                return null;

            if (Playlist.Items.Count == 0)
            {
                // Drain stderr before reporting the extractor's actual failure.
                process.WaitForExit();
                if (extractionError?.Contains("Unsupported URL", StringComparison.OrdinalIgnoreCase) == true)
                    return null; // Let FFmpeg try URLs yt-dlp does not handle.
                return new(extractionError ?? "yt-dlp returned no playable streams. Update the web playback tools and try again.");
            }
            Handler.OnPlaylistCompleted();
            return new();
        }
        catch (Exception e)
        {
            if (sessionId != currentSession || currentSession != Handler.OpenCounter)
                return null;
            Log.Error($"Open ({e.Message})");
            return new($"Web playback failed: {e.Message}");
        }
    }

    // Quote one Windows argv value while retaining the user's ExtraArguments option.
    internal static string QuoteArgument(string value)
    {
        var result = new System.Text.StringBuilder("\"");
        int slashes = 0;
        foreach (char ch in value)
        {
            if (ch == '\\') { slashes++; continue; }
            result.Append('\\', ch == '\"' ? slashes * 2 + 1 : slashes).Append(ch);
            slashes = 0;
        }
        return result.Append('\\', slashes * 2).Append('\"').ToString();
    }

    public OpenResults OpenItem()
        => new();

    public ExternalAudioStream SuggestExternalAudio()
    {
        if (Handler.OpenedPlugin == null || Handler.OpenedPlugin.Name != Name)
            return null;

        var fmt = GetAudioOnly((YoutubeDLJson)GetTag(Selected));
        if (fmt == null)
            return null;

        foreach (var extStream in Selected.ExternalAudioStreams)
            if (fmt.url == extStream.Url)
                return extStream;

        return null;
    }
    public ExternalVideoStream SuggestExternalVideo()
    {
        if (Handler.OpenedPlugin == null || Handler.OpenedPlugin.Name != Name)
            return null;

        Format fmt = GetBestMatch((YoutubeDLJson)GetTag(Selected));
        if (fmt == null)
            return null;

        foreach (var extStream in Selected.ExternalVideoStreams)
            if (fmt.url == extStream.Url)
                return extStream;

        return null;
    }
}
