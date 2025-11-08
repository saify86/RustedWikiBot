using RustedWikiBot.Models;
using System.Text;

namespace RustedWikiBot.Services;

public static class TextFormatter
{
    // Формирование текстово карточки отдельно юнита.
    public static string UnitCard(Unit u)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{u.Name} — {u.Attack}");
        sb.AppendLine($"Место производства: {u.Tier}");
        sb.AppendLine($"Стоимость: {u.Cost}");
        if (!string.IsNullOrWhiteSpace(u.Time)) sb.AppendLine($"Время производства: {u.Time}");
        if (!string.IsNullOrWhiteSpace(u.Tips)) sb.AppendLine($"\nСоветы: {u.Tips}");
        return sb.ToString();
    }
    // Формирование текстовой карточки отдельно здания.
    public static string BuildingCard(Building b)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{b.Name} — {b.Type}");
        sb.AppendLine($"Cost: {b.Cost} | HP: {b.HP}");
        if (!string.IsNullOrWhiteSpace(b.Tips)) sb.AppendLine($"\nСоветы: {b.Tips}");
        return sb.ToString();
    }
}

