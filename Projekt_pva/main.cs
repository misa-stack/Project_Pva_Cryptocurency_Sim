using Spectre.Console;
using System.Text.Json;
using Projekt_pva;

namespace Projekt_pva;

public class Program
{
    private static List<double> btcHistory = new();

    public static void Main(string[] args)
    {
        var market = new MarketEngine();
        var mining = new MiningEngine(market);
        var cryptoWallet = Enum.GetValues<CryptoCurrency>().ToDictionary(c => c, _ => 0.0);

        market.Start();
        mining.Start();

        while (true)
        {
            Console.Clear();
            AnsiConsole.Write(new FigletText("CRYPTO TYCOON").Color(Color.Gold1));

            btcHistory.Add(market.Prices[CryptoCurrency.BTC]);
            if (btcHistory.Count > 30) btcHistory.RemoveAt(0);

            var grid = new Grid().AddColumn().AddColumn();

            var stats = new Table().Border(TableBorder.Rounded).Title("[b]SIMULATION[/]");
            stats.AddColumn("Metric").AddColumn("Value");
            stats.AddRow("Market", $"[bold]{market.GetMarketTrend()}[/]");
            stats.AddRow("Balance", $"[yellow]{mining.WalletBalance:N2} $[/]");
            stats.AddRow("Temp", $"{(mining.CurrentTemperature > 85 ? "[red]" : "[green]")}{mining.CurrentTemperature:F1} °C[/]");

            var walletTable = new Table().Border(TableBorder.Rounded).Title("[b]WALLET[/]");
            walletTable.AddColumn("Coin").AddColumn("Value");
            foreach (var coin in cryptoWallet.Keys)
                walletTable.AddRow(coin.ToString(), $"[green]{(cryptoWallet[coin] * market.Prices[coin]):N2} $[/]");

            grid.AddRow(stats, walletTable);
            AnsiConsole.Write(grid);

            AnsiConsole.Write(new Panel(DrawMarketGraph()).Header("BTC PRICE TREND (Last 30 ticks)").BorderColor(Color.Blue));

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .AddChoices("Buy Hardware", "Buy Cooling", "Manage Hardware", "Sell All", "Save Game", "Load Game", "Exit"));

            if (choice == "Exit") break;

            switch (choice)
            {
                case "Buy Hardware":
                    var model = AnsiConsole.Prompt(new SelectionPrompt<string>().AddChoices(HardwareStore.GetAvailableModels()));
                    var item = HardwareStore.CreateGpu(model);
                    if (mining.WalletBalance >= item.Price && mining.CurrentLocation.Rigs.Count < mining.CurrentLocation.Size)
                    {
                        mining.WalletBalance -= item.Price;
                        mining.CurrentLocation.Rigs.Add(item);
                    }
                    break;

                case "Sell All":
                    foreach (var c in cryptoWallet.Keys.ToList()) {
                        mining.WalletBalance += cryptoWallet[c] * market.Prices[c];
                        cryptoWallet[c] = 0;
                    }
                    break;

                case "Save Game":
                    var saveData = new SaveData { Balance = mining.WalletBalance, Wallet = cryptoWallet };
                    File.WriteAllText("savegame.json", JsonSerializer.Serialize(saveData));
                    AnsiConsole.MarkupLine("[green]Game Saved![/]");
                    Thread.Sleep(1000);
                    break;

                case "Load Game":
                    if (File.Exists("savegame.json")) {
                        var loaded = JsonSerializer.Deserialize<SaveData>(File.ReadAllText("savegame.json"));
                        mining.WalletBalance = loaded.Balance;
                        cryptoWallet = loaded.Wallet;
                        AnsiConsole.MarkupLine("[green]Game Loaded![/]");
                    }
                    Thread.Sleep(1000);
                    break;
            }
        }
    }

    private static string DrawMarketGraph()
    {
        if (btcHistory.Count < 2) return "Gathering data...";
        double max = btcHistory.Max();
        double min = btcHistory.Min();
        double range = max - min;
        
        string graph = "";
        for (int y = 5; y >= 0; y--)
        {
            for (int x = 0; x < btcHistory.Count; x++)
            {
                double threshold = min + (range / 5.0 * y);
                graph += btcHistory[x] >= threshold ? "[green]█[/]" : " ";
            }
            graph += "\n";
        }
        return graph;
    }
}

public class SaveData
{
    public double Balance { get; set; }
    public Dictionary<CryptoCurrency, double> Wallet { get; set; }
}