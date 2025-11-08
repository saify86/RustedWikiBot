using Microsoft.Extensions.Configuration;
using RustedWikiBot;
using RustedWikiBot.Services;
// Чтение конфигурации(считывание токена)
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .Build();
var token = config["Telegram:BotToken"];
if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("Укажи токен в appsettings.json -> Telegram:BotToken");
    return;
}
// Создание репозиториев и выбор базовой директории
var baseDir = AppContext.BaseDirectory;
var unitsRepo = new UnitRepository(Path.Combine(baseDir, "data", "units.json"));
var buildingsRepo = new BuildingRepository(Path.Combine(baseDir, "data", "buildings.json"));
// Создание хоста для запуска бота и добавление возможности принудительной остановки бота.
var host = new BotHost(token, unitsRepo, buildingsRepo);
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
host.Run(cts.Token);
await Task.Delay(Timeout.Infinite, cts.Token).ContinueWith(_ => { });



