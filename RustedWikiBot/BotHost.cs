using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using IOFile = System.IO.File;
using IOPath = System.IO.Path;

using RustedWikiBot.Services;
using RustedWikiBot.Models;

namespace RustedWikiBot;


public sealed class BotHost
{
    // Поля для связи с клиентом телеграмма и репозиториями юнитов и зданий.
    private readonly ITelegramBotClient _bot;
    private readonly UnitRepository _units;
    private readonly BuildingRepository _buildings;
    // Создание клиента по токену бота и сохранение ссылок на репозитории.
    public BotHost(string token, UnitRepository unitRepo, BuildingRepository buildingRepo)
    {
        _bot = new TelegramBotClient(token);
        _units = unitRepo;
        _buildings = buildingRepo;
    }
    // Запуск бота для работы с событиями и принудительной остановкой.
    public void Run(CancellationToken ct)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery, UpdateType.InlineQuery }
        };

        _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, ct);
        Console.WriteLine("Bot started. Press Ctrl+C to stop.");
    }
    // Обработчик ошибок с выводом ошибки в консоль.
    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine($"[HANDLE ERROR] {ex.Message}");
        return Task.CompletedTask;
    }
    // Делит события по типам (сообщения и кнопки)
    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message when update.Message is { } m:
                    await OnMessage(m, ct);
                    break;

                case UpdateType.CallbackQuery when update.CallbackQuery is { } cq:
                    await OnCallback(cq, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
        }
    }

    // Обработчик текстовых сообщений при работе с ботом.

    private async Task OnMessage(Message m, CancellationToken ct)
    {
        var text = m.Text?.Trim() ?? string.Empty;

        if (text.StartsWith("/start") || text.StartsWith("/help") || text.StartsWith("/menu"))
        {
            await ShowMenu(m.Chat.Id, ct);
            return;
        }

        if (text.StartsWith("/unit"))
        {
            var q = text["/unit".Length..].Trim();
            if (string.IsNullOrWhiteSpace(q))
            {
                await _bot.SendTextMessageAsync(m.Chat.Id, "Укажи имя любого доступного юнита: \n" +  "/unit builder(строитель, билдер)\n" + "/unit tank(танк, лёгкий танк, легкий танк)\n" 
                    + "/unit hovertank(ховер танк)\n" + "/unit landingcraft(десантник, десантный корабль)\n" + "/unit basicmech(шагоход, мех шагоход)\n" 
                    + "/unit scout(разведчик, скаут)\n" + "/unit heavytank(тяжелый танк,тяжёлый танк)\n" + "/unit artillery(артиллерия, САУ)\n" 
                    + "/unit aamech(ПВО  шагаход, ПВО мех)\n" + "/unit heavyhovertank(тяжелый ховер танк, тяжёлый ховер танк)\n" + "/unit plasmatank(плазма танк)\n" 
                    + "/unit lasertank(лазерный танк)\n" + "/unit martillery(артиллерия шагоход, САУ шагоход) \n" + "/unit rockettank(ракетный танк)\n" 
                    + "/unit heavyartillery(тяжёлая артиллерия, тяжелая артиллерия)\n" + "/unit combatengineer(боевой инженер, боевой строитель)\n" + "/unit mammothtank(танк мамонт, мамонт)\n" 
                    + "/unit mobileturret(мобильная турель)\n" + "/unit mechengineer(мех инженер, мех строитель)\n" + "/unit minigunmech(мех миниган)\n" 
                    + "/unit teslamech(мех тесла, электрический мех)\n" + "/unit plasmamech(плазма мех)\n" + "/unit heavyaamech(тяжелый ПВО мех, тяжёлый ПВО мех)\n" 
                    + "/unit flamemech(пламенный мех, огненный мех)\n" + "/unit exptank(экспериментальный танк)\n" + "/unit shieldhovertank(бронированный ховер танк)\n" 
                    + "/unit expspider(экспериментальный паук, паук)\n" + "/unit modspider(модульный паук)\n", cancellationToken: ct);
                return;
            }

            var unit = _units.FindByNameOrAlias(q);
            if (unit is null)
            {
                await _bot.SendTextMessageAsync(m.Chat.Id, "Юнит не найден.", cancellationToken: ct);
                return;
            }

            var idx = _units.All().ToList().FindIndex(u => u.Id.Equals(unit.Id, StringComparison.OrdinalIgnoreCase));
            await SendUnitCard(m.Chat.Id, idx < 0 ? 0 : idx, ct);
            return;
        }

        if (text.StartsWith("/randomunit"))
        {
            var idx = Random.Shared.Next(0, _units.All().Count);
            await SendUnitCard(m.Chat.Id, idx, ct);
            return;
        }

        if (text.StartsWith("/building"))
        {
            var q = text["/building".Length..].Trim();
            if (string.IsNullOrWhiteSpace(q))
            {
                await _bot.SendTextMessageAsync(m.Chat.Id, "Укажи имя: /building AAT1", cancellationToken: ct);
                return;
            }

            var b = _buildings.FindByNameOrAlias(q);
            if (b is null)
            {
                await _bot.SendTextMessageAsync(m.Chat.Id, "Здание не найдено.", cancellationToken: ct);
                return;
            }

            var idx = _buildings.All().ToList().FindIndex(x => x.Id.Equals(b.Id, StringComparison.OrdinalIgnoreCase));
            await SendBuildingCard(m.Chat.Id, idx < 0 ? 0 : idx, ct);
            return;
        }

        if (text.StartsWith("/randombuilding"))
        {
            var idx = Random.Shared.Next(0, _buildings.All().Count);
            await SendBuildingCard(m.Chat.Id, idx, ct);
            return;
        }
    }

    // Реализация логики работы кнопок.

    private async Task OnCallback(CallbackQuery cq, CancellationToken ct)
    {
        var data = cq.Data ?? string.Empty;
        var chatId = cq.Message!.Chat.Id;

        switch (data)
        {
            case "menu":
                await ShowMenu(chatId, ct);
                return;

            case "u_rnd":
                await SendUnitCard(chatId, Random.Shared.Next(0, _units.All().Count), ct, edit: false);
                return;

            case "b_rnd":
                await SendBuildingCard(chatId, Random.Shared.Next(0, _buildings.All().Count), ct, edit: false);
                return;
        }

        var parts = data.Split(':');
        if (parts.Length == 2 && int.TryParse(parts[1], out var idx))
        {
            if (parts[0] == "u")
                await SendUnitCard(chatId, idx, ct, edit: true, cq.Message);
            else if (parts[0] == "b")
                await SendBuildingCard(chatId, idx, ct, edit: true, cq.Message);
        }
    }

    // Вывод меню и кнопок для случайного юнита или здания.

    private async Task ShowMenu(long chatId, CancellationToken ct)
    {
        var text = "RustedWikiBot — это мини-справочник по юнитам и зданиям из игры Rusted Warfare, стратегии в рельном времени.\n" +
                   "Команды:\n" +
                   "/unit <имя>\n" +
                   "/randomunit\n" +
                   "/building <имя>\n" +
                   "/randombuilding";

        var kb = new InlineKeyboardMarkup(new[]
        {
        new []
        {
            InlineKeyboardButton.WithCallbackData("Случайный юнит", "u_rnd"),
            InlineKeyboardButton.WithCallbackData("Случайное здание", "b_rnd")
        }
    });

        await _bot.SendTextMessageAsync(chatId, text, replyMarkup: kb, cancellationToken: ct);
    }

    // Формирование кнопок навигации при нахождении в карточке с юнитом(переход между юнитами зацикленный).
    private InlineKeyboardMarkup UnitNav(int idx, int total)
    {
        int prev = (idx - 1 + total) % total;
        int next = (idx + 1) % total;

        return new InlineKeyboardMarkup(new[]
        {
        new []
        {
            InlineKeyboardButton.WithCallbackData("◀️ Предыдущий", $"u:{prev}"),
            InlineKeyboardButton.WithCallbackData("Меню", "menu"),
            InlineKeyboardButton.WithCallbackData("Следующий ▶️", $"u:{next}")
        }
    });
    }
    // Формирование кнопок навигации при нахождении в карточке со зданием(переход между зданиями зацикленный).
    private InlineKeyboardMarkup BuildingNav(int idx, int total)
    {
        int prev = (idx - 1 + total) % total;
        int next = (idx + 1) % total;

        return new InlineKeyboardMarkup(new[]
        {
        new []
        {
            InlineKeyboardButton.WithCallbackData("◀️ Предыдущий", $"b:{prev}"),
            InlineKeyboardButton.WithCallbackData("Меню", "menu"),
            InlineKeyboardButton.WithCallbackData("Следующий ▶️", $"b:{next}")
        }
    });
    }
    // Отправка карточки с отдельным юнитом.
    private async Task SendUnitCard(long chatId, int idx, CancellationToken ct, bool edit = false, Message? origin = null)
    {
        var list = _units.All();
        if (list.Count == 0)
        {
            await _bot.SendTextMessageAsync(chatId, "База юнитов пуста.", cancellationToken: ct);
            return;
        }

        idx = (idx % list.Count + list.Count) % list.Count;
        var u = list[idx];
        var caption = TextFormatter.UnitCard(u);
        var nav = UnitNav(idx, list.Count);
        // Если перешли по кнопке, то удаляем предыдущее сообщение, чтобы обновить информацию и фото.
        if (edit && origin != null)
        {
            try { await _bot.DeleteMessageAsync(chatId, origin.MessageId, cancellationToken: ct); } catch { }
        }

        var img = u.ImageUrl;

        if (!string.IsNullOrWhiteSpace(img))
        {
            if (img.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                img = img.Substring("file:".Length);

            if (img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                await _bot.SendPhotoAsync(chatId: chatId, photo: img, caption: caption, replyMarkup: nav, cancellationToken: ct);
                return;
            }

            var fullPath = IOPath.IsPathRooted(img) ? img : IOPath.Combine(AppContext.BaseDirectory, img);
            if (IOFile.Exists(fullPath))
            {
                await using var stream = IOFile.OpenRead(fullPath);
                var photo = new InputOnlineFile(stream, IOPath.GetFileName(fullPath));
                await _bot.SendPhotoAsync(chatId: chatId, photo: photo, caption: caption, replyMarkup: nav, cancellationToken: ct);
                return;
            }
        }
        // Если фото не загрузилось, отправляется просто текст.
        await _bot.SendTextMessageAsync(chatId, caption, replyMarkup: nav, cancellationToken: ct);
    }

    // Отправка карточки с отдельным зданием.
    private async Task SendBuildingCard(long chatId, int idx, CancellationToken ct, bool edit = false, Message? origin = null)
    {
        var list = _buildings.All();
        if (list.Count == 0)
        {
            await _bot.SendTextMessageAsync(chatId, "База зданий пуста.", cancellationToken: ct);
            return;
        }

        idx = (idx % list.Count + list.Count) % list.Count;
        var b = list[idx];
        var caption = TextFormatter.BuildingCard(b);
        var nav = BuildingNav(idx, list.Count);
        // Если перешли по кнопке, то удаляем предыдущее сообщение, чтобы обновить информацию и фото.
        if (edit && origin != null)
        {
            try { await _bot.DeleteMessageAsync(chatId, origin.MessageId, cancellationToken: ct); } catch { }
        }

        var img = b.ImageUrl;

        if (!string.IsNullOrWhiteSpace(img))
        {
            if (img.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                img = img.Substring("file:".Length);

            if (img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                await _bot.SendPhotoAsync(chatId: chatId, photo: img, caption: caption, replyMarkup: nav, cancellationToken: ct);
                return;
            }

            var fullPath = IOPath.IsPathRooted(img) ? img : IOPath.Combine(AppContext.BaseDirectory, img);
            if (IOFile.Exists(fullPath))
            {
                await using var stream = IOFile.OpenRead(fullPath);
                var photo = new InputOnlineFile(stream, IOPath.GetFileName(fullPath));
                await _bot.SendPhotoAsync(chatId: chatId, photo: photo, caption: caption, replyMarkup: nav, cancellationToken: ct);
                return;
            }
        }
        // Если фото не загрузилось, отправляется просто текст.
        await _bot.SendTextMessageAsync(chatId, caption, replyMarkup: nav, cancellationToken: ct);
    }
}

