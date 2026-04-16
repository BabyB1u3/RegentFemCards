using System.Text.Json;
using PortraitModGenerator.Core.Models;

namespace PortraitModGenerator.Core.Services;

public sealed class OfficialCardIndexLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OfficialCardIndex Load(string indexPath)
    {
        if (string.IsNullOrWhiteSpace(indexPath))
        {
            throw new ArgumentException("Official card index path is required.", nameof(indexPath));
        }

        string fullPath = Path.GetFullPath(indexPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Official card index file was not found.", fullPath);
        }

        string json = File.ReadAllText(fullPath);
        OfficialCardIndex? index = JsonSerializer.Deserialize<OfficialCardIndex>(json, SerializerOptions);
        if (index is null)
        {
            throw new InvalidOperationException($"Failed to deserialize official card index '{fullPath}'.");
        }

        if (string.IsNullOrWhiteSpace(index.Version))
        {
            throw new InvalidOperationException("Official card index is missing a version.");
        }

        if (index.Cards.Count == 0)
        {
            throw new InvalidOperationException("Official card index does not contain any cards.");
        }

        return index;
    }
}
