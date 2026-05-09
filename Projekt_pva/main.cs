using Spectre.Console;
using System.Text;
using System.Text.Json;
using Projekt_pva;

namespace Projekt_pva;

public class Program
{
    private static readonly Dictionary<CryptoCurrency, List<double>> PriceHistory =
        Enum.GetValues<CryptoCurrency>().ToDictionary(c => c, _ => new List<double>());

    private static readonly object _walletLock = new();

    private static readonly Dictionary<CryptoCurrency, Color> CoinColor = new()
    {
        { CryptoCurrency.BTC,          Color.Yellow },
        { CryptoCurrency.ETH,          Color.Blue   },
        { CryptoCurrency.SOL,          Color.Cyan1  },
        { CryptoCurrency.DOGE,         Color.Green  },
        { CryptoCurrency.HawkTuahCoin, Color.Red    },
    };

    private static string CC(CryptoCurrency c) => $"[{CoinColor[c].ToString().ToLower()}]";


    public static void Main(string[] args)
    {
        var market       = new MarketEngine();
        var mining       = new MiningEngine(market);
        var cryptoWallet = Enum.GetValues<CryptoCurrency>().ToDictionary(c => c, _ => 0.0);

        market.Start();
        mining.Start();

        _ = Task.Run(() =>
        {
            while (true)
            {
                lock (_walletLock)
                    mining.FlushCoinsIntoWallet(cryptoWallet);
                Thread.Sleep(1000);
            }
        });

        while (true)
        {
            RenderDashboard(mining, market, cryptoWallet);

            while (!Console.KeyAvailable)
            {
                Thread.Sleep(1000);
                RenderDashboard(mining, market, cryptoWallet);
            }
            Console.ReadKey(true);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]What do you want to do?[/]")
                    .AddChoices(
                        "Buy Hardware",
                        "Buy Cooling",
                        "Manage Hardware",
                        "Buy Location",
                        "View Market Charts",
                        "Sell All Crypto",
                        "Save Game",
                        "Load Game",
                        "Exit"
                    ));

            if (choice == "Exit") break;

            switch (choice)
            {
                case "Buy Hardware":       BuyHardwareMenu(mining);                          break;
                case "Buy Cooling":        BuyCoolingMenu(mining);                           break;
                case "Manage Hardware":    ManageHardwareMenu(mining);                       break;
                case "Buy Location":       BuyLocationMenu(mining);                          break;
                case "View Market Charts": ViewMarketCharts(market);                         break;
                case "Sell All Crypto":    SellAllCrypto(mining, cryptoWallet, market);      break;
                case "Save Game":          SaveGame(mining, cryptoWallet, market);           break;
                case "Load Game":          LoadGame(mining, ref cryptoWallet, market);       break;
            }
        }
    }


    private static void RenderDashboard(MiningEngine mining, MarketEngine market,
                                         Dictionary<CryptoCurrency, double> cryptoWallet)
    {
        foreach (var coin in Enum.GetValues<CryptoCurrency>())
        {
            PriceHistory[coin].Add(market.Prices[coin]);
            if (PriceHistory[coin].Count > 60) PriceHistory[coin].RemoveAt(0);
        }

        Console.Clear();
        AnsiConsole.Write(new FigletText("CRYPTO TYCOON").Color(Color.Gold1));

        var grid = new Grid();
        grid.AddColumn(new GridColumn());
        grid.AddColumn(new GridColumn());
        grid.AddColumn(new GridColumn());

        double currentLoadKW = mining.CurrentLocation.Rigs
            .Sum(h => h.Consumption * (h is MiningHardware { IsOverclocked: true } ? 1.5 : 1.0)) / 1000.0;
        bool isOverLimit = currentLoadKW > mining.CurrentLocation.PowerLimit;

        var stats = new Table().Border(TableBorder.Rounded).Title("[b]SIMULATION[/]").Expand();
        stats.AddColumn("Metric");
        stats.AddColumn("Value");
        stats.AddRow("Market",   $"[bold]{market.GetMarketTrend()}[/]");
        stats.AddRow("Balance",  $"[yellow]{mining.WalletBalance:N2} $[/]");
        stats.AddRow("Power",    $"{(isOverLimit ? "[red]" : "[green]")}{currentLoadKW:F1} / {mining.CurrentLocation.PowerLimit} kW[/]");
        stats.AddRow("Temp",     $"{(mining.CurrentTemperature > 85 ? "[red]" : "[green]")}{mining.CurrentTemperature:F1} °C[/]");
        stats.AddRow("Location", $"[cyan]{mining.CurrentLocation.Name}[/]");
        stats.AddRow("Rigs",     $"{mining.CurrentLocation.Rigs.Count} / {mining.CurrentLocation.Size}");

        var walletTable = new Table().Border(TableBorder.Rounded).Title("[b]WALLET[/]").Expand();
        walletTable.AddColumn("Coin");
        walletTable.AddColumn("Amount");
        walletTable.AddColumn("Value $");
        lock (_walletLock)
        {
            foreach (var (coin, amount) in cryptoWallet)
            {
                double val = amount * market.Prices[coin];
                walletTable.AddRow($"{CC(coin)}{coin}[/]", $"{amount:F6}", $"[green]{val:N2}[/]");
            }
        }

        var priceTable = new Table().Border(TableBorder.Rounded).Title("[b]PRICES[/]").Expand();
        priceTable.AddColumn("Coin");
        priceTable.AddColumn("Price $");
        priceTable.AddColumn("History");
        foreach (var coin in Enum.GetValues<CryptoCurrency>())
            priceTable.AddRow($"{CC(coin)}{coin}[/]", $"[white]{market.Prices[coin]:N4}[/]", Sparkline(coin, 10));

        grid.AddRow(stats, walletTable, priceTable);
        AnsiConsole.Write(grid);

        AnsiConsole.MarkupLine("[grey]⛏  Mining in progress... Press any key to open the menu.[/]");
    }


    private static void ViewMarketCharts(MarketEngine market)
    {
        Console.Clear();
        AnsiConsole.MarkupLine("[bold yellow]══════════════  MARKET CHARTS  ══════════════[/]\n");

        int termW = Console.WindowWidth > 0 ? Console.WindowWidth : 120;
        int pts   = Math.Min(55, termW / 2 - 6);
        int rows  = 10;

        var coins = Enum.GetValues<CryptoCurrency>().ToList();

        for (int i = 0; i < coins.Count; i += 2)
        {
            var left      = coins[i];
            var leftPanel = BuildChartPanel(left, market, pts, rows);

            if (i + 1 < coins.Count)
            {
                var right      = coins[i + 1];
                var rightPanel = BuildChartPanel(right, market, pts, rows);
                AnsiConsole.Write(new Columns(leftPanel, rightPanel));
            }
            else
            {
                AnsiConsole.Write(new Panel(SmoothGraph(left, pts * 2, rows))
                    .Header(CoinHeader(left, market))
                    .BorderColor(CoinColor[left])
                    .Expand());
            }
        }

        AnsiConsole.MarkupLine("\n[grey]Press any key to go back...[/]");
        Console.ReadKey(true);
    }

    private static Panel BuildChartPanel(CryptoCurrency coin, MarketEngine market, int pts, int rows)
    {
        var hist = PriceHistory[coin];
        double pct = hist.Count >= 2
            ? (hist[^1] - hist[0]) / hist[0] * 100
            : 0;
        string pctStr = pct >= 0 ? $"[green]+{pct:F2}%[/]" : $"[red]{pct:F2}%[/]";

        string content = SmoothGraph(coin, pts, rows)
                       + $"\n{CC(coin)}min[/] [grey]{hist.DefaultIfEmpty(0).Min():N6}[/]"
                       + $"  {CC(coin)}max[/] [grey]{hist.DefaultIfEmpty(0).Max():N6}[/]"
                       + $"  {pctStr}";

        return new Panel(content)
            .Header(CoinHeader(coin, market))
            .BorderColor(CoinColor[coin])
            .Expand();
    }

    private static string CoinHeader(CryptoCurrency coin, MarketEngine market)
    {
        var hist  = PriceHistory[coin];
        string tr = hist.Count >= 2 ? (hist[^1] > hist[^2] ? "▲" : "▼") : "–";
        return $"{CC(coin)} {coin} {tr} {market.Prices[coin]:N6} $[/]";
    }


    private static void BuyHardwareMenu(MiningEngine mining)
    {
        var loc = mining.CurrentLocation;

        var t = new Table().Border(TableBorder.Rounded).Title("[b]Available Hardware[/]").Expand();
        t.AddColumn("Model").AddColumn("Hashrate").AddColumn("Power W").AddColumn("Heat W").AddColumn("Slots").AddColumn("Price $");
        foreach (var name in HardwareStore.GetAvailableModels())
        {
            var hw = HardwareStore.CreateGpu(name);
            t.AddRow(name, $"{hw.Hashrate:N0}", $"{hw.Consumption:N0}", $"{hw.HeatOutput:N0}",
                     hw.Size.ToString(), $"[yellow]{hw.Price:N0}[/]");
        }
        AnsiConsole.Write(t);
        AnsiConsole.MarkupLine($"Balance: [yellow]{mining.WalletBalance:N0} $[/]  |  Free slots: {loc.Size - loc.Rigs.Count}");

        var models = HardwareStore.GetAvailableModels();
        models.Add("← Back");
        var model = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select:").AddChoices(models));
        if (model == "← Back") return;

        var item = HardwareStore.CreateGpu(model);
        if      (mining.WalletBalance < item.Price) AnsiConsole.MarkupLine($"[red]Need {item.Price:N0} $[/]");
        else if (loc.Rigs.Count >= loc.Size)        AnsiConsole.MarkupLine("[red]Location is full![/]");
        else
        {
            mining.WalletBalance -= item.Price;
            loc.Rigs.Add(item);
            AnsiConsole.MarkupLine($"[green]Bought {item.Name}![/]");
        }
        Thread.Sleep(1200);
    }

    private static void BuyCoolingMenu(MiningEngine mining)
    {
        var loc = mining.CurrentLocation;

        var t = new Table().Border(TableBorder.Rounded).Title("[b]Available Cooling[/]").Expand();
        t.AddColumn("Model").AddColumn("Cooling kW").AddColumn("Power W").AddColumn("Slots").AddColumn("Price $");
        foreach (var name in HardwareStore.GetAvailableCooling())
        {
            var cu = HardwareStore.CreateCooling(name);
            t.AddRow(name, $"{cu.CoolingPower:N0}", $"{cu.Consumption:N0}", cu.Size.ToString(), $"[yellow]{cu.Price:N0}[/]");
        }
        AnsiConsole.Write(t);
        AnsiConsole.MarkupLine($"Temp: {mining.CurrentTemperature:F1} °C  |  Balance: [yellow]{mining.WalletBalance:N0} $[/]");

        var models = HardwareStore.GetAvailableCooling();
        models.Add("← Back");
        var model = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select:").AddChoices(models));
        if (model == "← Back") return;

        var unit = HardwareStore.CreateCooling(model);
        if      (mining.WalletBalance < unit.Price) AnsiConsole.MarkupLine($"[red]Need {unit.Price:N0} $[/]");
        else if (loc.Rigs.Count >= loc.Size)        AnsiConsole.MarkupLine("[red]No room![/]");
        else
        {
            mining.WalletBalance -= unit.Price;
            loc.Rigs.Add(unit);
            AnsiConsole.MarkupLine($"[green]Installed {unit.Name} (+{unit.CoolingPower} kW cooling)[/]");
        }
        Thread.Sleep(1200);
    }

    private static void ManageHardwareMenu(MiningEngine mining)
    {
        var loc = mining.CurrentLocation;
        if (loc.Rigs.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No hardware installed.[/]");
            Thread.Sleep(1200);
            return;
        }

        var labels = loc.Rigs.Select(h => h switch
        {
            MiningHardware mh => $"[[GPU]] {h.Name} | {h.Condition:F1}% | {(mh.IsOverclocked ? "[red]OC[/]" : "stock")} | {mh.SelectedCoin}",
            CoolingUnit    cu => $"[[FAN]] {h.Name} | {h.Condition:F1}% | {cu.CoolingPower} kW",
            RigHardware    rh => $"[[RIG]] {h.Name} | {rh.Cards.Count} cards | {h.Condition:F1}%",
            _                 => h.Name
        }).ToList();
        labels.Add("← Back");

        var chosen = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title("Select hardware:").AddChoices(labels));
        if (chosen == "← Back") return;

        ManageSpecificHardware(mining, loc.Rigs[labels.IndexOf(chosen)]);
    }

    private static void ManageSpecificHardware(MiningEngine mining, Hardware hw)
    {
        int resale  = (int)(hw.Price * 0.5 * (hw.Condition / 100.0));
        var actions = new List<string>();

        if (hw is MiningHardware mhw)
        {
            actions.Add(mhw.IsOverclocked ? "Underclock" : "Overclock (+40% hash, +50% power)");
            actions.Add("Change Mined Coin");
        }
        if (hw is RigHardware) actions.Add("View Cards");
        actions.Add($"Sell — {resale:N0} $");
        actions.Add("← Back");

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title($"[bold]{hw.Name}[/]").AddChoices(actions));

        switch (action)
        {
            case var s when s.StartsWith("Over") || s.StartsWith("Under"):
                var mh = (MiningHardware)hw;
                mh.IsOverclocked = !mh.IsOverclocked;
                AnsiConsole.MarkupLine(mh.IsOverclocked ? "[red]Overclocked![/]" : "[green]Stock.[/]");
                Thread.Sleep(900);
                break;

            case "Change Mined Coin": ChangeCoinMenu((MiningHardware)hw, mining); break;
            case "View Cards":        ViewRigCards((RigHardware)hw);              break;

            case var s when s.StartsWith("Sell"):
                mining.CurrentLocation.Rigs.Remove(hw);
                mining.WalletBalance += resale;
                AnsiConsole.MarkupLine($"[green]Sold {hw.Name} for {resale:N0} $[/]");
                Thread.Sleep(1200);
                break;
        }
    }

    private static void ChangeCoinMenu(MiningHardware hw, MiningEngine mining)
    {
        var t = new Table().Border(TableBorder.Rounded).Title("[b]Coin Profitability[/]").Expand();
        t.AddColumn("Coin").AddColumn("Price $").AddColumn("Difficulty").AddColumn("Est $/hr");
        foreach (var coin in Enum.GetValues<CryptoCurrency>())
        {
            double rate = (hw.Hashrate / (mining.NetworkDifficulties[coin] * 3600.0)) * mining.Prices[coin];
            t.AddRow($"{CC(coin)}{coin}[/]", $"{mining.Prices[coin]:N6}",
                     $"{mining.NetworkDifficulties[coin]:N0}", $"[yellow]{rate:N8}[/]");
        }
        AnsiConsole.Write(t);

        var coins = Enum.GetValues<CryptoCurrency>().Select(c => c.ToString()).ToList();
        coins.Add("← Back");
        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title($"Mining: [yellow]{hw.SelectedCoin}[/]. Switch:").AddChoices(coins));
        if (pick == "← Back") return;

        hw.SelectedCoin = Enum.Parse<CryptoCurrency>(pick);
        AnsiConsole.MarkupLine($"[green]Now mining {hw.SelectedCoin}[/]");
        Thread.Sleep(900);
    }

    private static void ViewRigCards(RigHardware rig)
    {
        if (rig.Cards.Count == 0) { AnsiConsole.MarkupLine("[yellow]No cards.[/]"); Thread.Sleep(900); return; }

        var t = new Table().Border(TableBorder.Rounded).Title($"[b]{rig.Name}[/]").Expand();
        t.AddColumn("Name").AddColumn("Hashrate").AddColumn("Coin").AddColumn("OC").AddColumn("Cond.");
        foreach (var card in rig.Cards)
            t.AddRow(card.Name, $"{card.Hashrate:N0}", card.SelectedCoin.ToString(),
                     card.IsOverclocked ? "[red]OC[/]" : "stock", $"{card.Condition:F1}%");

        AnsiConsole.Write(t);
        AnsiConsole.MarkupLine("Press any key...");
        Console.ReadKey(true);
    }

    private static void BuyLocationMenu(MiningEngine mining)
    {
        var t = new Table().Border(TableBorder.Rounded).Title("[b]Locations[/]").Expand();
        t.AddColumn("Name").AddColumn("Slots").AddColumn("Power kW").AddColumn("Elec $/kWh").AddColumn("Cooling kW").AddColumn("Price $");
        foreach (var name in LocationStore.GetAvailableLocations())
        {
            var l = LocationStore.BuyLocation(name);
            t.AddRow(l.Name, l.Size.ToString(), $"{l.PowerLimit:N0}", $"{l.ElectricityPrice:F2}",
                     $"{l.CoolingCapacity:N0}", $"[yellow]{l.Price:N0}[/]");
        }
        AnsiConsole.Write(t);
        AnsiConsole.MarkupLine($"Balance: [yellow]{mining.WalletBalance:N0} $[/]");

        var available = LocationStore.GetAvailableLocations();
        available.Add("← Back");
        var pick = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Choose:").AddChoices(available));
        if (pick == "← Back") return;

        Location newLoc;
        try { newLoc = LocationStore.BuyLocation(pick); }
        catch (ArgumentException e) { AnsiConsole.MarkupLine($"[red]{e.Message}[/]"); Thread.Sleep(1200); return; }

        if (mining.WalletBalance < newLoc.Price)
        {
            AnsiConsole.MarkupLine("[red]Can't afford it.[/]");
            Thread.Sleep(1200);
            return;
        }

        if (!AnsiConsole.Confirm($"Buy {newLoc.Name} for {newLoc.Price:N0} $?")) return;

        mining.WalletBalance -= newLoc.Price;
        var old   = mining.CurrentLocation.Rigs.ToList();
        int moved = 0;
        foreach (var h in old)
            if (newLoc.Rigs.Count < newLoc.Size) { newLoc.Rigs.Add(h); moved++; }

        int lost = old.Count - moved;
        mining.CurrentLocation = newLoc;

        AnsiConsole.MarkupLine($"[green]Moved! {moved} rig(s) transferred.[/]");
        if (lost > 0) AnsiConsole.MarkupLine($"[red]{lost} rig(s) lost (didn't fit).[/]");
        Thread.Sleep(1800);
    }


    private static void SellAllCrypto(MiningEngine mining, Dictionary<CryptoCurrency, double> wallet, MarketEngine market)
    {
        double total;
        lock (_walletLock)
        {
            total = wallet.Keys.Sum(c => { double v = wallet[c] * market.Prices[c]; wallet[c] = 0; return v; });
        }
        mining.WalletBalance += total;
        AnsiConsole.MarkupLine($"[green]Sold all for {total:N4} $[/]");
        Thread.Sleep(1200);
    }


    private static void SaveGame(MiningEngine mining, Dictionary<CryptoCurrency, double> wallet, MarketEngine market)
    {
        SaveData snapshot;
        lock (_walletLock)
        {
            snapshot = new SaveData
            {
                Balance          = mining.WalletBalance,
                LocationKey      = mining.CurrentLocation.Name,
                Wallet           = new Dictionary<CryptoCurrency, double>(wallet),
                Difficulties     = new Dictionary<CryptoCurrency, double>(mining.NetworkDifficulties),
                Prices           = new Dictionary<CryptoCurrency, double>(market.Prices),
                MarketSentiment  = market.MarketSentiment,
                MarketVolatility = market.MarketVolatility,
                CycleTicks       = market.CycleTicks,
                Rigs             = mining.CurrentLocation.Rigs.Select(SerializeHardware).ToList()
            };
        }

        File.WriteAllText("savegame.json",
            JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));

        AnsiConsole.MarkupLine("[green]✔ Game saved![/]");
        Thread.Sleep(1000);
    }

    private static void LoadGame(MiningEngine mining, ref Dictionary<CryptoCurrency, double> wallet, MarketEngine market)
    {
        if (!File.Exists("savegame.json"))
        {
            AnsiConsole.MarkupLine("[red]No save file found.[/]");
            Thread.Sleep(1000);
            return;
        }

        SaveData? loaded;
        try
        {
            loaded = JsonSerializer.Deserialize<SaveData>(File.ReadAllText("savegame.json"));
        }
        catch
        {
            AnsiConsole.MarkupLine("[red]Save file is corrupted.[/]");
            Thread.Sleep(1000);
            return;
        }

        if (loaded == null) return;

        mining.WalletBalance = loaded.Balance;

        lock (_walletLock)
            wallet = loaded.Wallet;

        foreach (var kv in loaded.Difficulties)
            mining.NetworkDifficulties[kv.Key] = kv.Value;

        foreach (var kv in loaded.Prices)
            market.Prices[kv.Key] = kv.Value;

        market.MarketSentiment  = loaded.MarketSentiment;
        market.MarketVolatility = loaded.MarketVolatility;
        market.CycleTicks       = loaded.CycleTicks;

        string locKey = loaded.LocationKey switch
        {
            "Garage"          => "Garage",
            "Beach House"     => "BeachHouse",
            "Warehouse"       => "WareHouse",
            "Ultimate Mansion"=> "SuperDuperUltimateLagreUnlimitedHouse",
            _                 => "Garage"
        };

        try   { mining.CurrentLocation = LocationStore.BuyLocation(locKey); }
        catch { mining.CurrentLocation = LocationStore.BuyLocation("Garage"); }

        foreach (var hw in loaded.Rigs)
        {
            var restored = DeserializeHardware(hw);
            if (restored != null)
                mining.CurrentLocation.AddRig(restored);
        }

        AnsiConsole.MarkupLine("[green]✔ Game loaded![/]");
        Thread.Sleep(1000);
    }

    private static HardwareSaveData SerializeHardware(Hardware hw)
    {
        return hw switch
        {
            MiningHardware m => new HardwareSaveData
            {
                Type          = "MiningHardware",
                ModelName     = m.Name,
                Condition     = m.Condition,
                IsOverclocked = m.IsOverclocked,
                SelectedCoin  = m.SelectedCoin.ToString()
            },
            CoolingUnit c => new HardwareSaveData
            {
                Type      = "CoolingUnit",
                ModelName = c.Name,
                Condition = c.Condition
            },
            RigHardware r => new HardwareSaveData
            {
                Type      = "RigHardware",
                ModelName = r.Name,
                Condition = r.Condition,
                Cards     = r.Cards.Select(SerializeHardware).ToList()
            },
            _ => new HardwareSaveData()
        };
    }

    private static Hardware? DeserializeHardware(HardwareSaveData data)
    {
        try
        {
            switch (data.Type)
            {
                case "MiningHardware":
                {
                    var hw = HardwareStore.CreateGpu(data.ModelName);
                    hw.Condition     = data.Condition;
                    hw.IsOverclocked = data.IsOverclocked;
                    hw.SelectedCoin  = Enum.Parse<CryptoCurrency>(data.SelectedCoin);
                    return hw;
                }
                case "CoolingUnit":
                {
                    var cu = HardwareStore.CreateCooling(data.ModelName);
                    cu.Condition = data.Condition;
                    return cu;
                }
                case "RigHardware":
                {
                    var rig = new RigHardware(data.ModelName, 0, 0, 2, 0);
                    rig.Condition = data.Condition;
                    foreach (var card in data.Cards)
                        if (DeserializeHardware(card) is MiningHardware mhw)
                            rig.AddCard(mhw);
                    return rig;
                }
            }
        }
        catch { }
        return null;
    }


    private static string SmoothGraph(CryptoCurrency coin, int maxPts, int rows)
    {
        var hist = PriceHistory[coin];
        if (hist.Count < 2) return "  Gathering data...\n";

        var    slice = hist.TakeLast(maxPts).ToList();
        double max   = slice.Max();
        double min   = slice.Min();
        double rng   = max - min;

        if (rng < 1e-12) return $"  Flat at {max:N6} $\n";

        const string BLOCKS = " ▁▂▃▄▅▆▇█";
        string col = CC(coin);
        var    sb  = new StringBuilder();

        for (int row = rows - 1; row >= 0; row--)
        {
            for (int x = 0; x < slice.Count; x++)
            {
                double norm   = (slice[x] - min) / rng;
                double height = norm * rows;

                if (height >= row + 1)
                    sb.Append($"{col}█[/]");
                else if (height > row)
                {
                    int eighth = (int)((height - row) * 8);
                    sb.Append($"{col}{BLOCKS[Math.Clamp(eighth, 1, 8)]}[/]");
                }
                else
                    sb.Append(' ');
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }

    private static string Sparkline(CryptoCurrency coin, int maxPts)
    {
        var hist = PriceHistory[coin];
        if (hist.Count < 2) return "[grey]···[/]";

        var    slice = hist.TakeLast(maxPts).ToList();
        double max   = slice.Max();
        double min   = slice.Min();
        double rng   = max - min;
        string col   = CC(coin);
        const string B = " ▁▂▃▄▅▆▇█";

        if (rng < 1e-12) return $"{col}{'─'.ToString().PadRight(maxPts, '─')}[/]";

        var sb = new StringBuilder();
        foreach (var v in slice)
        {
            int idx = (int)((v - min) / rng * 7);
            sb.Append($"{col}{B[Math.Clamp(idx, 0, 7)]}[/]");
        }
        return sb.ToString();
    }
}


public class HardwareSaveData
{
    public string Type          { get; set; } = "";
    public string ModelName     { get; set; } = "";
    public double Condition     { get; set; }
    public bool   IsOverclocked { get; set; }
    public string SelectedCoin  { get; set; } = "BTC";
    public List<HardwareSaveData> Cards { get; set; } = new();
}

public class SaveData
{
    public double Balance                                  { get; set; }
    public string LocationKey                              { get; set; } = "Garage";
    public Dictionary<CryptoCurrency, double> Wallet      { get; set; } = new();
    public Dictionary<CryptoCurrency, double> Difficulties{ get; set; } = new();
    public Dictionary<CryptoCurrency, double> Prices      { get; set; } = new();
    public double MarketSentiment                          { get; set; } = 1.0;
    public double MarketVolatility                         { get; set; } = 1.0;
    public int    CycleTicks                               { get; set; } = 0;
    public List<HardwareSaveData> Rigs                    { get; set; } = new();
}