namespace Projekt_pva;

public class MiningEngine
{
    public double WalletBalance { get; set; } = 10000.0;
    public double CurrentTemperature { get; private set; } = 20.0;
    public Location CurrentLocation { get; set; }
    private readonly MarketEngine _market;

    public Dictionary<CryptoCurrency, double> NetworkDifficulties { get; private set; } = new()
    {
        { CryptoCurrency.BTC, 100000.0 },
        { CryptoCurrency.ETH, 5000.0 },
        { CryptoCurrency.SOL, 2000.0 },
        { CryptoCurrency.DOGE, 500.0 },
        { CryptoCurrency.HawkTuahCoin, 10.0 }
    };

    public Dictionary<CryptoCurrency, double> Prices => _market.Prices;
    public bool IsRunning { get; private set; }

    public MiningEngine(MarketEngine market)
    {
        _market = market;
        CurrentLocation = LocationStore.BuyLocation("Garage");
    }

    public void Start()
    {
        IsRunning = true;
        Task.Run(() => GameLoop());
    }

    private void GameLoop()
    {
        while (IsRunning)
        {
            if (CurrentLocation != null) UpdateSimulation();
            Thread.Sleep(1000);
        }
    }

    private void UpdateSimulation()
    {
        double totalMinedUsd = 0;
        double totalConsumptionWh = 0;
        double totalHeatGen = 0;
        double totalCoolingPower = CurrentLocation.CoolingCapacity;

        foreach (var hardware in CurrentLocation.Rigs.ToList())
        {
            if (hardware is MiningHardware mhw)
            {
                ProcessHardware(mhw, ref totalMinedUsd, ref totalConsumptionWh, ref totalHeatGen);
            }
            else if (hardware is RigHardware rhw)
            {
                totalConsumptionWh += rhw.Consumption;
                totalHeatGen += rhw.HeatOutput;
                foreach (var card in rhw.Cards) ProcessHardware(card, ref totalMinedUsd, ref totalConsumptionWh, ref totalHeatGen);
            }
            else if (hardware is CoolingUnit cu)
            {
                totalCoolingPower += cu.CoolingPower;
                totalConsumptionWh += cu.Consumption;
            }
        }

        double thermalMass = CurrentLocation.Size * 1.5;
        double heatDelta = (totalHeatGen - totalCoolingPower) / thermalMass;
        CurrentTemperature = Math.Clamp(CurrentTemperature + heatDelta - (CurrentTemperature - 20) * 0.05, 20, 120);

        WalletBalance -= (totalConsumptionWh / 1000.0) * (CurrentLocation.ElectricityPrice / 3600.0);
        WalletBalance += totalMinedUsd;

        foreach (var coin in NetworkDifficulties.Keys.ToList())
            NetworkDifficulties[coin] *= (1 + (Prices[coin] * 0.0000001));
    }

    private void ProcessHardware(MiningHardware hw, ref double minedUsd, ref double consumption, ref double heat)
    {
        double throttle = CurrentTemperature > 80 ? Math.Max(0.1, 1.0 - (CurrentTemperature - 80) / 40.0) : 1.0;
        double ocMult = hw.IsOverclocked ? 1.4 : 1.0;
        
        minedUsd += (hw.Hashrate * ocMult * throttle * (hw.Condition / 100.0)) / (NetworkDifficulties[hw.SelectedCoin] * 3600) * Prices[hw.SelectedCoin];
        consumption += hw.Consumption * (hw.IsOverclocked ? 1.5 : 1.0);
        heat += hw.HeatOutput * (hw.IsOverclocked ? 2.0 : 1.0) * throttle;
        hw.Condition -= (CurrentTemperature > 95 ? 0.05 : 0.001);
    }
}