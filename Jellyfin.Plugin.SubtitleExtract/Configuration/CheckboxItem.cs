namespace Jellyfin.Plugin.SubtitleExtract.Configuration;

/// <summary>
/// Container for the checkbox model.
/// </summary>
public class CheckboxItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CheckboxItem"/> class.
    /// </summary>
    public CheckboxItem()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckboxItem"/> class.
    /// </summary>
    /// <param name="value">The checkbox value.</param>
    /// <param name="text">The checkbox text.</param>
    public CheckboxItem(string value, string text)
    {
        Value = value;
        Text = text;
    }

    /// <summary>
    /// Gets or sets the checkbox value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the checkbox text.
    /// </summary>
    public string? Text { get; set; }
}
