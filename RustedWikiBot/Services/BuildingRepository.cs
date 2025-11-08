using System.Text.Json;
using RustedWikiBot.Models;

namespace RustedWikiBot.Services;

public sealed class BuildingRepository
{
    // Поле для сохранения массива из JSON с юнитами и осуществления быстрого доступа к зданию по его айди
    private readonly List<Building> _all;
    private readonly Dictionary<string, Building> _byId;
    // Конструктор для загрузки JSON файла и постройка хеш карты для ее последующей нормализации
    public BuildingRepository(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"Buildings file not found: {jsonPath}");

        var json = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _all = JsonSerializer.Deserialize<List<Building>>(json, options) ?? new List<Building>();
        _byId = _all.ToDictionary(b => Normalize(b.Id), b => b);
    }

    // Ссылка на внутренний список для чтения, отсюда берется индекс здания. 
    public IReadOnlyList<Building> All() => _all;
    // Поиск конкретного здания по имени или синониму.
    public Building? FindByNameOrAlias(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        query = Normalize(query);

        if (_byId.TryGetValue(query, out var byId))
            return byId;

        foreach (var b in _all)
            if (b.Aliases.Any(a => Normalize(a) == query))
                return b;

        var starts = _all.FirstOrDefault(b => Normalize(b.Name).StartsWith(query, StringComparison.Ordinal));
        if (starts != null) return starts;

        return _all.FirstOrDefault(b => Normalize(b.Name).Contains(query, StringComparison.Ordinal));
    }
    // Нормализация т.е. возможность найти совпадение более точно, убирание пробелов, приводит к одному регистру.
    private static string Normalize(string s)
        => new string(s.Trim().ToLowerInvariant().Where(ch => !char.IsWhiteSpace(ch)).ToArray());
}

