using System.Text.Json;
using RustedWikiBot.Models;

namespace RustedWikiBot.Services;

public sealed class UnitRepository
{
    // Поля для сохранения массива из JSON с юнитами и осуществления быстрого доступа к юниту по его айди
    private readonly Dictionary<string, Unit> _byId;
    private readonly List<Unit> _all;
    // Конструктор для загрузки JSON файла и постройка хеш карты для ее последующей нормализации
    public UnitRepository(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"Units file not found: {jsonPath}");

        var json = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _all = JsonSerializer.Deserialize<List<Unit>>(json, options) ?? new List<Unit>();
        _byId = _all.ToDictionary(u => u.Id, u => u, StringComparer.OrdinalIgnoreCase);
    }
    // Ссылка на внутренний список для чтения, отсюда берется индекс юнита. 
    public IReadOnlyList<Unit> All() => _all;
    // Поиск конкретного юнита по имени, айди или синониму.
    public Unit? FindById(string id)
        => _byId.TryGetValue(id, out var u) ? u : null;
    public Unit? FindByNameOrAlias(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        query = Normalize(query);

        foreach (var u in _all)
        {
            if (Normalize(u.Id) == query) return u;
            if (u.Aliases.Any(a => Normalize(a) == query)) return u;
        }

        var starts = _all.FirstOrDefault(u => Normalize(u.Name).StartsWith(query, StringComparison.Ordinal));
        if (starts != null) return starts;
        return _all.FirstOrDefault(u => Normalize(u.Name).Contains(query, StringComparison.Ordinal));
    }
    // Нормализация т.е. возможность найти совпадение более точно, убирание пробелов, приводит к одному регистру.
    private static string Normalize(string s)
        => new string(s.Trim().ToLowerInvariant().Where(ch => !char.IsWhiteSpace(ch)).ToArray());
}
