namespace RustedWikiBot.Models;
// Характеристики для описания здания в JSON файле с общим списком зданий
public sealed class Building
{
    public required string Id { get; set; }            
    public required string Name { get; set; }   
    public string[] Aliases { get; set; } = Array.Empty<string>();
    public string Type { get; set; } = "Строение";     
    public int Cost { get; set; }
    public int HP { get; set; } = 0;
    public string Tips { get; set; } = "";
    public string? ImageUrl { get; set; }
}

