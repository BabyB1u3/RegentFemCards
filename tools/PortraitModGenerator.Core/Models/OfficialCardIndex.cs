namespace PortraitModGenerator.Core.Models;

public sealed class OfficialCardIndex
{
    public string Version { get; set; } = string.Empty;

    public List<OfficialCardEntry> Cards { get; set; } = [];
}
