namespace RustedWikiBot.Models;
// Характеристики для описания юнита в JSON файле с общим списком юнитов
public sealed class Unit
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string[] Aliases { get; set; } = Array.Empty<string>();
    public required string Tier { get; set; }
    public required string Attack { get; set; }
    public int Cost { get; set; }
    public string Time { get; set; } = string.Empty;
    public string Tips { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}



