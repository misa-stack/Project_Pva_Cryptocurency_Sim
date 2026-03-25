using Spectre.Console;
using Projekt_pva;

namespace Projekt_pva;

public class Program
{
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
            AnsiConsole.Write(new FigletText("Crypto Miner").Color(Color.Green));

            var stats = new Table().AddColumn("Status").AddColumn("Value");
            stats.AddRow("Location", mining.CurrentLocation.Name);
            stats.AddRow("Balance", $"[yellow]{mining.WalletBalance:N2} $[/]");
            stats.AddRow("Temp", $"[red]{mining.CurrentTemperature:F1} °C[/]");
            AnsiConsole.Write(stats);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Action:")
                    .AddChoices("Buy Hardware", "Manage Rigs", "Mine", "Sell All", "Exit"));

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

                case "Manage Rigs":
                    var mineables = new List<MiningHardware>();
                    foreach (var h in mining.CurrentLocation.Rigs)
                    {
                        if (h is MiningHardware m) mineables.Add(m);
                        if (h is RigHardware r) mineables.AddRange(r.Cards);
                    }

                    if (mineables.Count == 0) break;

                    var target = AnsiConsole.Prompt(new SelectionPrompt<MiningHardware>()
                        .UseConverter(h => $"{h.Name} (Coin: {h.SelectedCoin})")
                        .AddChoices(mineables));

                    var coin = AnsiConsole.Prompt(new SelectionPrompt<CryptoCurrency>().AddChoices(Enum.GetValues<CryptoCurrency>()));
                    target.SelectCoin(coin);
                    break;

                case "Mine":
                    AnsiConsole.Status().Start("Mining...", ctx => {
                        for (int i = 0; i < 5; i++) {
                            foreach (var h in mining.CurrentLocation.Rigs) {
                                if (h is MiningHardware m) cryptoWallet[m.SelectedCoin] += (m.Hashrate / mining.NetworkDifficulties[m.SelectedCoin]) / 5;
                                if (h is RigHardware r) foreach(var c in r.Cards) cryptoWallet[c.SelectedCoin] += (c.Hashrate / mining.NetworkDifficulties[c.SelectedCoin]) / 5;
                            }
                            Thread.Sleep(200);
                        }
                    });
                    break;

                case "Sell All":
                    double total = 0;
                    foreach (var c in cryptoWallet.Keys.ToList()) {
                        total += cryptoWallet[c] * mining.Prices[c];
                        cryptoWallet[c] = 0;
                    }
                    mining.WalletBalance += total;
                    break;
            }
        }
    }
}