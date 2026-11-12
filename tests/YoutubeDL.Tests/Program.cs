using System;
using System.Collections.Generic;
using System.Text.Json;
using FlyleafLib;
using FlyleafLib.Plugins;

class Program
{
    static int passed;

    static void Main()
    {
        var options = new JsonSerializerOptions { Converters = { new NullNumberConverter() } };
        var hlsLow = JsonSerializer.Deserialize<Format>("""
            {"url":"https://example.test/low.m3u8","protocol":"m3u8_native",
             "vcodec":"none","acodec":null,"abr":null,"audio_channels":null,
             "resolution":"audio only","audio_ext":"mp4","language":"en-US"}
            """, options);
        var hlsHigh = JsonSerializer.Deserialize<Format>("""
            {"url":"https://example.test/high.m3u8","protocol":"m3u8_native",
             "vcodec":"none","acodec":null,"abr":null,"audio_channels":null,
             "resolution":"audio only","audio_ext":"mp4","language":"en-US"}
            """, options);
        var opus = new Format { url = "https://example.test/audio.webm", protocol = "https", vcodec = "none", acodec = "opus", abr = 128, language = "en-US" };
        var english = new[] { Language.Get("en") };

        Check(YoutubeDL.HasAudio(hlsHigh), "HLS audio survives missing codec and bitrate metadata");
        Check(ReferenceEquals(Select([hlsLow, hlsHigh, opus], english, "m3u8_native"), hlsHigh), "HLS video selects matching HLS audio over direct Opus");
        Check(ReferenceEquals(Select([hlsLow, hlsHigh, opus], english, "https"), opus), "Direct video keeps direct audio");
        Check(ReferenceEquals(Select([hlsLow, hlsHigh], english, "m3u8_native"), hlsHigh), "Highest HLS variant wins with unknown bitrates");

        var spanish = new Format { url = "https://example.test/es.m3u8", protocol = "m3u8_native", vcodec = "none", resolution = "audio only", language = "es-US" };
        Check(ReferenceEquals(Select([spanish, opus], english, "m3u8_native"), opus), "Preferred language takes priority over transport");
        Check(ReferenceEquals(Select([hlsHigh, spanish], [Language.Get("es"), Language.Get("en")], "m3u8_native"), spanish), "Language order is respected");

        var unknown = new Format { url = "https://example.test/unknown.m3u8", protocol = "m3u8_native", vcodec = "none", audio_ext = "mp4" };
        Check(ReferenceEquals(Select([unknown], english, "m3u8_native"), unknown) && unknown.language == null, "Unknown language remains usable without being relabelled English");
        Check(!YoutubeDL.HasAudio(new Format { acodec = "none", audio_ext = "none", height = 1080 }), "Silent video is excluded");
        Check(!YoutubeDL.HasAudio(new Format { vcodec = "none", acodec = "none", protocol = "mhtml", height = 90 }), "Storyboards are excluded");

        var original = new Format { url = "https://example.test/original.m3u8", protocol = "m3u8_native", resolution = "audio only", language = "en-US", language_preference = 10 };
        var dubbed = new Format { url = "https://example.test/dubbed.m3u8", protocol = "m3u8_native", resolution = "audio only", language = "en-US", language_preference = -10 };
        Check(ReferenceEquals(Select([original, dubbed], english, "m3u8_native"), original), "Original audio beats a dub of the same language");
        Check(Select([], english, "m3u8_native") == null, "Empty format lists have no audio suggestion");
        Console.WriteLine($"Passed {passed} YouTube audio regression checks.");
    }

    static Format Select(IEnumerable<Format> formats, IEnumerable<Language> languages, string protocol)
        => YoutubeDL.SelectAudioFormat(formats, languages, protocol);

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        passed++;
        Console.WriteLine($"PASS: {message}");
    }
}
