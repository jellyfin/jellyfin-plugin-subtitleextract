#pragma warning disable CA1819 // Properties should not return arrays

using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SubtitleExtract.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    private static readonly CheckboxItem[] _allSubtitleCodecs =
    [
        new("ass", "ASS / SSA"),
        new("DVDSUB", "DVD (VOBSUB)"),
        new("subrip", "SubRip (SRT)"),
        new("PGSSUB", "PGS (Blu-ray)"),
        new("DVBSUB", "DVB"),
        new("eia_608", "EIA-608 (CC)"),
        new("jacosub", "JACOsub"),
        new("microdvd", "MicroDVD"),
        new("mov_text", "MOV Text"),
        new("mpl2", "MPL2"),
        new("pjs", "PJS"),
        new("realtext", "RealText"),
        new("sami", "SAMI"),
        new("stl", "Spruce STL"),
        new("subviewer", "SubViewer"),
        new("subviewer1", "SubViewer v1"),
        new("text", "Raw Text"),
        new("vplayer", "VPlayer"),
        new("webvtt", "WebVTT"),
        new("xsub", "XSUB"),
    ];

    private static readonly CheckboxItem[] _allLanguages =
    [
        new("und", "Undefined / Untagged"),
        new("afr", "Afrikaans"),
        new("alb", "Albanian"),
        new("ara", "Arabic"),
        new("arm", "Armenian"),
        new("aze", "Azerbaijani"),
        new("baq", "Basque"),
        new("bel", "Belarusian"),
        new("ben", "Bengali"),
        new("bos", "Bosnian"),
        new("bul", "Bulgarian"),
        new("bur", "Burmese"),
        new("cat", "Catalan"),
        new("chi", "Chinese"),
        new("hrv", "Croatian"),
        new("cze", "Czech"),
        new("dan", "Danish"),
        new("dut", "Dutch"),
        new("eng", "English"),
        new("est", "Estonian"),
        new("fil", "Filipino"),
        new("fin", "Finnish"),
        new("fre", "French"),
        new("geo", "Georgian"),
        new("ger", "German"),
        new("gre", "Greek"),
        new("guj", "Gujarati"),
        new("heb", "Hebrew"),
        new("hin", "Hindi"),
        new("hun", "Hungarian"),
        new("ice", "Icelandic"),
        new("ind", "Indonesian"),
        new("gle", "Irish"),
        new("ita", "Italian"),
        new("jpn", "Japanese"),
        new("kan", "Kannada"),
        new("kaz", "Kazakh"),
        new("khm", "Khmer"),
        new("kor", "Korean"),
        new("kur", "Kurdish"),
        new("lao", "Lao"),
        new("lav", "Latvian"),
        new("lit", "Lithuanian"),
        new("mac", "Macedonian"),
        new("may", "Malay"),
        new("mal", "Malayalam"),
        new("mar", "Marathi"),
        new("mon", "Mongolian"),
        new("nep", "Nepali"),
        new("nor", "Norwegian"),
        new("nob", "Norwegian Bokmal"),
        new("nno", "Norwegian Nynorsk"),
        new("per", "Persian"),
        new("pol", "Polish"),
        new("por", "Portuguese"),
        new("pan", "Punjabi"),
        new("rum", "Romanian"),
        new("rus", "Russian"),
        new("srp", "Serbian"),
        new("sin", "Sinhala"),
        new("slo", "Slovak"),
        new("slv", "Slovenian"),
        new("spa", "Spanish"),
        new("swa", "Swahili"),
        new("swe", "Swedish"),
        new("tam", "Tamil"),
        new("tel", "Telugu"),
        new("tha", "Thai"),
        new("tur", "Turkish"),
        new("ukr", "Ukrainian"),
        new("urd", "Urdu"),
        new("uzb", "Uzbek"),
        new("vie", "Vietnamese"),
        new("wel", "Welsh"),
        new("yid", "Yiddish"),
        new("zul", "Zulu"),
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether to extract subtitles and attachments during library scan.
    /// </summary>
    public bool ExtractionDuringLibraryScan { get; set; } = false;

    /// <summary>
    /// Gets or sets the list of selected libraries to extract subtitles from (empty means all).
    /// </summary>
    public string[] SelectedSubtitlesLibraries { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of selected libraries to extract attachments from (empty means all).
    /// </summary>
    public string[] SelectedAttachmentsLibraries { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether to extract all languages regardless of selection.
    /// </summary>
    public bool ExtractAllLanguages { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of selected subtitle languages to extract.
    /// </summary>
    public string[] SelectedLanguages { get; set; } = [];

    /// <summary>
    /// Gets all available subtitle languages.
    /// </summary>
    public CheckboxItem[] AllLanguages => _allLanguages;

    /// <summary>
    /// Gets or sets a value indicating whether to extract all codec types regardless of selection.
    /// </summary>
    public bool ExtractAllCodecTypes { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of selected subtitle codec types to extract.
    /// </summary>
    public string[] SelectedCodecTypes { get; set; } = [];

    /// <summary>
    /// Gets all available subtitle codecs.
    /// </summary>
    public CheckboxItem[] AllSubtitleCodecs => _allSubtitleCodecs;

    /// <summary>
    /// Gets or sets the regex pattern for accepting subtitles by title. Only subtitles matching this pattern are extracted.
    /// </summary>
    public string AcceptTitleRegex { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the regex pattern for rejecting subtitles by title. Subtitles matching this pattern are skipped (takes precedence over accept).
    /// </summary>
    public string RejectTitleRegex { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the accept override expression is enabled.
    /// </summary>
    public bool AcceptOverrideEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the accept override expression. When enabled and evaluates to true, the subtitle is extracted regardless of other filters.
    /// Uses C# syntax with variables: LANGUAGE, TYPE, TITLE.
    /// </summary>
    public string AcceptOverrideExpression { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the reject override expression is enabled.
    /// </summary>
    public bool RejectOverrideEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the reject override expression. When enabled and evaluates to true, the subtitle is skipped regardless of other filters.
    /// Uses C# syntax with variables: LANGUAGE, TYPE, TITLE.
    /// </summary>
    public string RejectOverrideExpression { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of worker threads for parallel subtitle extraction.
    /// </summary>
    public int WorkerThreads { get; set; } = 1;
}
