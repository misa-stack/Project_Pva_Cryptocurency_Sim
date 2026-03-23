using Spectre.Console;
using Projekt_pva;

namespace Projekt_pva;

public class Program
{
    public static void Main(string[] args)
    {
        // 1. Inicializace enginů
        var market = new MarketEngine();
        var mining = new MiningEngine(market);

        // 2. Nastavení výchozí lokace
        var pocatecniGaraz = new Location
        {
            Name = "Maminčina garáž",
            ElectricityPrice = 4.5,
            PowerLimit = 2000,
            CoolingCapacity = 500,
            Size = 5
        };
        mining.CurrentLocation = pocatecniGaraz;

        // 3. Inicializace krypto peněženky (množství mincí)
        var cryptoWallet = new Dictionary<CryptoCurrency, double>();
        foreach (CryptoCurrency coin in Enum.GetValues(typeof(CryptoCurrency)))
        {
            cryptoWallet[coin] = 0;
        }

        // 4. Start simulace na pozadí
        market.Start();
        mining.Start();

        bool running = true;
        while (running)
        {
            Console.Clear();
            AnsiConsole.Write(new FigletText("Crypto Miner").Color(Color.Green));

            // STATISTIKY (USD, Teplota)
            var stats = new Table()
                .AddColumn("Položka")
                .AddColumn("Hodnota");
            
            stats.AddRow("Dolarový zůstatek", $"[yellow]{mining.WalletBalance:N2} $[/]");
            stats.AddRow("Teplota v hale", $"[red]{mining.CurrentTemperature:F1} °C[/]");
            AnsiConsole.Write(stats);

            // TABULKA PORTFOLIA A TRHU
            var cryptoTable = new Table()
                .Title("[bold blue]Moje Portfolio & Aktuální Trh[/]")
                .AddColumn("Měna")
                .AddColumn("Vlastněno (ks)")
                .AddColumn("Cena za kus")
                .AddColumn("Hodnota v $");

            foreach (var coin in mining.Prices.Keys)
            {
                double amount = cryptoWallet[coin];
                double price = mining.Prices[coin];
                double valueInUsd = amount * price;

                cryptoTable.AddRow(
                    coin.ToString(),
                    $"[blue]{amount:F6}[/]",
                    $"[green]{price:F4} $[/]",
                    $"[yellow]{valueInUsd:N2} $[/]"
                );
            }
            AnsiConsole.Write(cryptoTable);

            // HLAVNÍ MENU
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Co chceš udělat?")
                    .AddChoices(new[] {
                        "Koupit nový hardware",
                        "Správa rigů (změna mince)",
                        "Nedělat nic (těžit)",
                        "Prodat vše do USD",
                        "Konec"
                    }));

            switch (choice)
            {
                case "Koupit nový hardware":
                    var modelName = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Vyber model:")
                            .AddChoices(HardwareStore.GetAvailableModels()));

                    var tempGpu = HardwareStore.CreateGpu(modelName);

                    if (mining.WalletBalance >= tempGpu.Price)
                    {
                        if (mining.CurrentLocation.Rigs.Count < mining.CurrentLocation.Size)
                        {
                            mining.WalletBalance -= tempGpu.Price;
                            mining.CurrentLocation.Rigs.Add(tempGpu);
                            AnsiConsole.MarkupLine($"[green]Koupeno {modelName} za {tempGpu.Price}$![/]");
                        }
                        else { AnsiConsole.MarkupLine("[red]Chyba: V lokaci není místo![/]"); }
                    }
                    else { AnsiConsole.MarkupLine("[red]Chyba: Nedostatek peněz![/]"); }
                    Thread.Sleep(1500);
                    break;

                case "Správa rigů (změna mince)":
                    if (mining.CurrentLocation.Rigs.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]Nemáš žádný hardware k nastavení.[/]");
                        Thread.Sleep(1000);
                        break;
                    }

                    var rigToChange = AnsiConsole.Prompt(
                        new SelectionPrompt<Hardware>()
                            .Title("Vyber rig pro nastavení:")
                            .UseConverter(h => $"{h.Name} (Těží: {h.SelectedCoin})")
                            .AddChoices(mining.CurrentLocation.Rigs));

                    var newCoin = AnsiConsole.Prompt(
                        new SelectionPrompt<CryptoCurrency>()
                            .Title("Vyber měnu k těžbě:")
                            .AddChoices(Enum.GetValues<CryptoCurrency>().Cast<CryptoCurrency>()));

                    rigToChange.SelectCoin(newCoin);
                    AnsiConsole.MarkupLine($"[green]{rigToChange.Name} nyní těží {newCoin}[/]");
                    Thread.Sleep(1000);
                    break;

                case "Nedělat nic (těžit)":
                    AnsiConsole.Status().Start("Probíhá těžba a aktualizace trhu...", ctx => {
                        // Simulace těžby do peněženky (2 sekundy reálného času)
                        for (int i = 0; i < 2; i++)
                        {
                            foreach (var rig in mining.CurrentLocation.Rigs)
                            {
                                double difficulty = mining.NetworkDifficulties[rig.SelectedCoin];
                                // Výpočet vytěženého množství (hashrate / obtížnost)
                                double mined = (rig.Hashrate * (rig.Condition / 100.0)) / difficulty;
                                cryptoWallet[rig.SelectedCoin] += mined;
                            }
                            Thread.Sleep(1000);
                        }
                    });
                    break;

                case "Prodat vše do USD":
                    double totalRevenue = 0;
                    foreach (var coin in cryptoWallet.Keys.ToList())
                    {
                        double revenue = cryptoWallet[coin] * mining.Prices[coin];
                        totalRevenue += revenue;
                        cryptoWallet[coin] = 0;
                    }
                    mining.WalletBalance += totalRevenue;
                    AnsiConsole.MarkupLine($"[green]Úspěšně jsi prodal mince za {totalRevenue:N2} $![/]");
                    Thread.Sleep(1500);
                    break;

                case "Konec":
                    running = false;
                    break;
            }
        }
    }
}