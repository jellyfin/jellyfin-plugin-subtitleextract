using System;
using System.Linq;
using System.Text.RegularExpressions;
using DynamicExpresso;
using Jellyfin.Plugin.SubtitleExtract.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.SubtitleExtract;

/// <summary>
/// Provides filtering logic for subtitle streams based on plugin configuration.
/// </summary>
public static class SubtitleStreamFilter
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Determines whether a subtitle stream should be extracted based on configuration.
    /// </summary>
    /// <param name="stream">The subtitle stream to evaluate.</param>
    /// <param name="config">The plugin configuration.</param>
    /// <returns>True if the stream should be extracted.</returns>
    public static bool ShouldExtractStream(MediaStream stream, PluginConfiguration config)
    {
        var language = string.IsNullOrEmpty(stream.Language) ? "und" : stream.Language;
        var codec = stream.Codec ?? string.Empty;
        var title = stream.Title ?? string.Empty;

        // Override expressions take priority over all other filters
        // Reject override: if enabled and matches, skip immediately
        if (config.RejectOverrideEnabled && !string.IsNullOrWhiteSpace(config.RejectOverrideExpression))
        {
            if (EvaluateExpression(config.RejectOverrideExpression, language, codec, title))
            {
                return false;
            }
        }

        // Accept override: if enabled and matches, accept immediately (bypass normal filters)
        if (config.AcceptOverrideEnabled && !string.IsNullOrWhiteSpace(config.AcceptOverrideExpression))
        {
            if (EvaluateExpression(config.AcceptOverrideExpression, language, codec, title))
            {
                return true;
            }
        }

        // Normal filters (all must pass)

        // Language filter
        if (!config.ExtractAllLanguages)
        {
            if (config.SelectedLanguages.Length == 0)
            {
                return false;
            }

            if (!config.SelectedLanguages.Contains(language, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Codec/type filter
        if (!config.ExtractAllCodecTypes)
        {
            if (config.SelectedCodecTypes.Length == 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(codec)
                || !config.SelectedCodecTypes.Contains(codec, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Regex title filter (reject takes precedence over accept)
        if (!string.IsNullOrWhiteSpace(config.RejectTitleRegex))
        {
            try
            {
                if (Regex.IsMatch(title, config.RejectTitleRegex, RegexOptions.IgnoreCase, RegexTimeout))
                {
                    return false;
                }
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern from user input; ignore filter
            }
            catch (RegexMatchTimeoutException)
            {
                // Regex took too long; ignore filter
            }
        }

        if (!string.IsNullOrWhiteSpace(config.AcceptTitleRegex))
        {
            try
            {
                if (!Regex.IsMatch(title, config.AcceptTitleRegex, RegexOptions.IgnoreCase, RegexTimeout))
                {
                    return false;
                }
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern from user input; ignore filter
            }
            catch (RegexMatchTimeoutException)
            {
                // Regex took too long; ignore filter
            }
        }

        return true;
    }

    /// <summary>
    /// Filters a media source's subtitle streams based on configuration.
    /// Returns null if no subtitle streams match the filter criteria.
    /// </summary>
    /// <param name="source">The media source to filter.</param>
    /// <param name="config">The plugin configuration.</param>
    /// <returns>The media source with filtered streams, or null if no subtitles match.</returns>
    public static MediaSourceInfo? FilterMediaSource(MediaSourceInfo source, PluginConfiguration config)
    {
        var nonSubStreams = source.MediaStreams
            .Where(s => s.Type != MediaStreamType.Subtitle)
            .ToList();

        var filteredSubStreams = source.MediaStreams
            .Where(s => s.Type == MediaStreamType.Subtitle && ShouldExtractStream(s, config))
            .ToList();

        if (filteredSubStreams.Count == 0)
        {
            return null;
        }

        source.MediaStreams = [.. nonSubStreams, .. filteredSubStreams];
        return source;
    }

    private static bool EvaluateExpression(string expression, string language, string codec, string title)
    {
        try
        {
            var interpreter = new Interpreter();
            interpreter.SetVariable("LANGUAGE", language);
            interpreter.SetVariable("TYPE", codec);
            interpreter.SetVariable("TITLE", title);
            return interpreter.Eval<bool>(expression);
        }
        catch (Exception)
        {
            // Invalid expression from user input; treat as non-matching
            return false;
        }
    }
}
