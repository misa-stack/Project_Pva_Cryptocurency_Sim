# Crypto Miner Tycoon — Kontrolní bod
## DISCLAIMER!!
   Pro formátování readme a pro psaní kódu byla v určitých momentech použita AI, a to z toho duvodu že se v tradingu ani celkově v kryptoměnách nevyznám, a tudíž pro celkovou kvalitu projektu bylo výhodnější se AI v tento moment zeptat na pomoc, než se vzdělávát o tom jak funguje trh, což je velice komplexní. To avšak neznamená, že kódu nerozumím. AI mi pomohla s principem, avšak to jak funguje herní smyčka či obchod na lokace jsem vytvářel já. 
## O projektu

Konzolový ekonomicko-technický simulátor těžby kryptoměn v C# (.NET 10) s terminálovým UI postaveným na knihovně **Spectre.Console**.

Hráč začíná v garáži s počátečním kapitálem 10 000 $, kupuje hardware, spravuje chlazení, sleduje volatilní trh a postupně rozrůstá impérium až do Ultimate Mansion s 500 sloty.

---

## Technologie — Spectre.Console

Spectre.Console je open-source knihovna pro C# umožňující vytvářet bohaté terminálové aplikace s barvami, tabulkami, grafy a interaktivními prvky bez nutnosti pracovat přímo s ANSI escape sekvencemi konzole.

### FigletText

Vykreslí ASCII art nadpis "CRYPTO TYCOON" při každém překreslení obrazovky. Font je generován automaticky z textu.

```csharp
AnsiConsole.Write(new FigletText("CRYPTO TYCOON").Color(Color.Gold1));
```

### Table + Grid

Hlavní dashboard se skládá ze tří tabulek vložených do `Grid` se třemi sloupci — simulační statistiky, peněženka a ceny coinů. Každá tabulka má zaoblený rámeček (`TableBorder.Rounded`) a vlastní záhlaví.

```csharp
var grid = new Grid().AddColumn().AddColumn().AddColumn();
var stats = new Table().Border(TableBorder.Rounded).Title("[b]SIMULATION[/]");
stats.AddColumn("Metric").AddColumn("Value");
stats.AddRow("Balance", $"[yellow]{mining.WalletBalance:N2} $[/]");
// ...
grid.AddRow(stats, walletTable, priceTable);
AnsiConsole.Write(grid);
```

### Markup

Spectre.Console podporuje inline značkování podobné BBCode. Barvy, tučné písmo a styly se zapisují přímo do řetězců pomocí hranatých závorek.

```csharp
AnsiConsole.MarkupLine($"[yellow]{mining.WalletBalance:N2} $[/]");
AnsiConsole.MarkupLine("[red]Location is full![/]");
AnsiConsole.MarkupLine($"[bold]{market.GetMarketTrend()}[/]");
```

Každá kryptoměna má přiřazenou barvu ve slovníku `CoinColor` a pomocná funkce `CC(coin)` vrací odpovídající otevírací markup tag — to zajišťuje konzistentní obarvení coinů v celém UI bez opakování kódu.

```csharp
private static readonly Dictionary<CryptoCurrency, Color> CoinColor = new()
{
    { CryptoCurrency.BTC,          Color.Yellow },
    { CryptoCurrency.ETH,          Color.Blue   },
    { CryptoCurrency.SOL,          Color.Cyan1  },
    { CryptoCurrency.DOGE,         Color.Green  },
    { CryptoCurrency.HawkTuahCoin, Color.Red    },
};

private static string CC(CryptoCurrency c) => $"[{CoinColor[c].ToString().ToLower()}]";
```

### SelectionPrompt

Interaktivní menu, ve kterém hráč pohybuje šipkami a potvrzuje Enterem. Používá se pro hlavní menu, výběr hardwaru, přepínání těžené mince i akce ve správě hardwaru.

```csharp
var choice = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("[bold]What do you want to do?[/]")
        .AddChoices("Buy Hardware", "Buy Cooling", "Sell All Crypto", "Exit"));
```

### Panel + Columns

Grafy cen jsou zabaleny do `Panel` s nadpisem a barevným rámečkem. Dva panely jsou zobrazeny vedle sebe pomocí `Columns`.

```csharp
AnsiConsole.Write(new Columns(
    btcPanel,
    new Panel(sparkTable)
        .Header("MARKET OVERVIEW")
        .BorderColor(Color.Grey)
        .Expand()));
```

### ASCII grafy — SmoothGraph a Sparkline

Grafy jsou implementovány ručně pomocí Unicode blokových znaků `▁▂▃▄▅▆▇█`.

`SmoothGraph` renderuje víceřádkový sloupcový graf — používá se pro BTC na hlavní obrazovce a pro všechny mince v market charts. Výška každého sloupce v každém řádku se počítá normalizací hodnoty do rozsahu 0–1 a převodem na osminu znaku.

`Sparkline` vrací jednořádkový mini-graf pro přehledovou tabulku — každá datová hodnota se zobrazí jako jeden blokový znak podle své výšky v rozsahu min–max.

```csharp
const string BLOCKS = " ▁▂▃▄▅▆▇█";
double norm   = (slice[x] - min) / rng;       // normalizace 0..1
double height = norm * rows;                   // přepočet na počet řádků
int eighth    = (int)((height - row) * 8);     // osmina pro plynulý přechod
sb.Append($"{col}{BLOCKS[Math.Clamp(eighth, 1, 8)]}[/]");
```

---

## Vysvětlení klíčových pojmů

### Hashrate

Výkon těžebního hardware — počet výpočetních operací (hashů) za sekundu. Čím vyšší hashrate, tím více mincí hardware vytěží za jednotku času. GPU karty mají hashrate v MH/s (megahashů za sekundu), ASIC minery řádově vyšší. V simulaci se hashrate přímo dělí síťovou obtížností a dobou ticku, výsledek je počet vytěžených coinů.

```csharp
double coinAmount = (hw.Hashrate * ocMult * throttle * (hw.Condition / 100.0))
                    / (NetworkDifficulties[hw.SelectedCoin] * 3600.0);
```

### Síťová obtížnost (Network Difficulty)

Číslo regulující, jak těžké je najít platný hash v dané kryptoměnové síti. Čím více těžařů soutěží, tím vyšší obtížnost — síť se automaticky přizpůsobuje, aby průměrný čas nalezení bloku zůstal konstantní (u Bitcoinu ~10 minut). V projektu obtížnost pomalu roste spolu s cenou mince, čímž se simuluje příliv nových těžařů během bull marketu.

```csharp
foreach (var coin in NetworkDifficulties.Keys.ToList())
    NetworkDifficulties[coin] *= (1 + Prices[coin] * 0.0000001);
```

### Volatilita

Míra kolísání ceny aktiva v čase. Vysoká volatilita znamená, že cena může v krátkém čase prudce vzrůst i spadnout — a obojí je stejně pravděpodobné. Bitcoin má relativně nízkou volatilitu (~1,5 % za tick) a chová se předvídatelněji. HawkTuahCoin má volatilitu ~15 % — jeho cena se mění chaoticky a může se za chvíli zdvojnásobit i zmizet.

```csharp
case CryptoCurrency.BTC:
    volatility = 0.015 * _marketVolatility;  // stabilní, nízká
    break;
case CryptoCurrency.HawkTuahCoin:
    volatility = 0.15 * _marketVolatility;   // extrémní, nepředvídatelná
    break;
```

### Tržní sentiment

Souhrnná "nálada trhu" — číslo ovlivňující směr pohybu cen. Sentiment > 1 tlačí ceny nahoru (bull market), sentiment < 1 dolů (bear market). Sentiment se náhodně mění každých 20–50 ticků. Občas nastane extrémní událost, která ho vyšroubuje na 5,0 (masivní pump) nebo srazí na 0,1 (crash). Tato hodnota je skrytá — hráč ji vidí pouze jako textový stav (BULLISH / BEARISH / CRASHING / MEGA BULLISH).

```csharp
_marketSentiment = 0.5 + (_rng.NextDouble() * 1.2);  // normální cyklus

// náhodná extrémní událost (0,5 % šance za tick):
bool isPositive      = _rng.NextDouble() > 0.5;
_marketSentiment     = isPositive ? 5.0 : 0.1;
_cycleTicksRemaining = 3;
```

### Korelace altcoinů s BTC

ETH, SOL a DOGE se nepohybují zcela nezávisle — 70 % jejich pohybu kopíruje pohyb Bitcoinu a zbývajících 30 % je náhodné. Toto odpovídá reálnému chování kryptotrhů, kde altcoiny obecně sledují BTC trend a divergují jen částečně.

```csharp
double correlation     = 0.7;
double independentMove = (_rng.NextDouble() * 2 - 1) * volatility;
baseChange = (btcMovement * correlation) + (independentMove * (1 - correlation));
```

### Overclocking

Přetaktování hardware nad výrobní specifikace — GPU běží rychleji a těží více coinů, ale spotřebovává více elektřiny a generuje výrazně více tepla. V projektu OC přidává +40 % hashrate za cenu +50 % spotřeby elektřiny a zdvojnásobení tepelného výkonu. Dlouhodobé přetaktování v kombinaci s vysokou teplotou způsobuje rychlejší degradaci.

```csharp
double ocMult   = hw.IsOverclocked ? 1.4 : 1.0;
consumption    += hw.Consumption * (hw.IsOverclocked ? 1.5 : 1.0);
heat           += hw.HeatOutput  * (hw.IsOverclocked ? 2.0 : 1.0) * throttle;
```

### Throttling (škrcení výkonu)

Když teplota místnosti překročí 80 °C, hardware automaticky snižuje výkon, aby se ochránil před poškozením. Při 80 °C je throttle 100 % (plný výkon), při 120 °C klesne na minimum 10 %. Throttling je lineárně závislý na teplotě.

```csharp
double throttle = CurrentTemperature > 80
    ? Math.Max(0.1, 1.0 - (CurrentTemperature - 80) / 40.0)
    : 1.0;
```

### Termodynamický model

Teplota místnosti se počítá každý tick z rozdílu generovaného tepla (součet `HeatOutput` všech minerů) a odváděného tepla (pasivní chlazení lokace + výkon aktivních chladicích jednotek). Výsledek se tlumí tepelnou hmotou místnosti odvíjející se od velikosti lokace — větší prostor se ohřívá pomaleji. Navíc je přidáno přirozené ochlazování prostředím (−5 % rozdílu od 20 °C za tick).

```csharp
double thermalMass = CurrentLocation.Size * 1.5;
double heatDelta   = (totalHeatGen - totalCoolingPower) / thermalMass;
CurrentTemperature = Math.Clamp(
    CurrentTemperature + heatDelta - (CurrentTemperature - 20) * 0.05,
    20, 120
);
```

### Opotřebení hardware (Condition)

Každý kus hardware má stav v procentech (100 % = nový, 0 % = zničený). Stav se snižuje každý tick — pomalu za normálního provozu (−0,001 %/tick) a rychle při přehřátí nad 95 °C (−0,05 %/tick). Nižší stav snižuje efektivní hashrate a zároveň i prodejní hodnotu při prodeji hardwaru.

```csharp
hw.Condition -= CurrentTemperature > 95 ? 0.05 : 0.001;
hw.Condition  = Math.Max(0, hw.Condition);

// vliv na výkon při těžbě:
double coinAmount = (hw.Hashrate * ... * (hw.Condition / 100.0)) / ...;

// vliv na prodejní cenu:
int resale = (int)(hw.Price * 0.5 * (hw.Condition / 100.0));
```

---

## Co je hotovo

### Jádro simulace

| Modul | Popis |
|---|---|
| `MiningEngine` | Herní smyčka na pozadí (1 tick/s). Počítá vytěžené coiny, spotřebu elektřiny, opotřebení. Thread-safe buffer `CoinsMinedBuffer` odděluje těžbu od UI vlákna. |
| `MarketEngine` | Paralelní vlákno aktualizující ceny každé 4 s. Implementuje sentiment, volatilitu, korelaci altcoinů s BTC a náhodné cenové šoky. |
| Termodynamický model | Teplota se počítá z tepelného výkonu minerů, kapacity chlazení a tepelné hmoty prostoru. Přehřátí throttluje výkon a poškozuje hardware. |
| Opotřebení | Hardware degraduje v závislosti na teplotě, snižuje výkon těžby i prodejní hodnotu. |

### Hardware

- **4 modely** těžebního hardware: RTX 4090, RTX 3080, GTX 1080 Ti, Antminer S19 (ASIC)
- **2 chladicí jednotky**: Basic Fan, Industrial AC
- Hierarchie tříd: `Hardware` → `MiningHardware` / `CoolingUnit` / `RigHardware`
- Každý kus má: Hashrate, Spotřeba (W), Tepelný výkon (W), Stav (%), Cena

### Lokace (Farmy)

| Lokace | Sloty | Limit kW | Cena kWh | Chlazení kW | Cena |
|---|---|---|---|---|---|
| Garage | 5 | 10,5 | 0,15 $ | 0 | 4 000 $ |
| Beach House | 12 | 25 | 0,25 $ | 5 | 25 000 $ |
| Warehouse | 100 | 200 | 0,10 $ | 30 | 150 000 $ |
| Ultimate Mansion | 500 | 999 | 0,05 $ | 100 | 1 000 000 $ |

### Kryptoměny

| Coin | Počáteční cena | Obtížnost | Volatilita |
|---|---|---|---|
| BTC | 65 000 $ | 100 000 | nízká |
| ETH | 3 500 $ | 5 000 | střední |
| SOL | 145 $ | 2 000 | střední |
| DOGE | 0,12 $ | 500 | střední |
| HawkTuahCoin | 0,00069 $ | 10 | extrémní |

### Ostatní hotové funkce

- Multi-měnová peněženka, prodej všech coinů jedním příkazem
- Overclocking s přepínačem per-hardware
- Přepínání těžené mince s real-time kalkulačkou ziskovosti (Est $/hr)
- Přechod mezi lokacemi s přesunem hardware
- JSON save/load (`savegame.json`)
- ASCII grafy cen s historií 60 ticků

---

## Architektura

```
Projekt_pva/
├── main.cs          # Entry point, UI smyčka, menu handlers
├── MiningEngine.cs     # Herní simulace na pozadí
├── MarketEngine.cs     # Tržní simulace na pozadí
├── Hardware.cs         # Hardware, MiningHardware, CoolingUnit, RigHardware
├── HardwareStore.cs    # Factory pro hardware (katalog modelů)
├── Location.cs         # Lokace (farma)
├── LocationStore.cs    # Factory pro lokace
└── CryptoCurrency.cs   # Enum kryptoměn
```

