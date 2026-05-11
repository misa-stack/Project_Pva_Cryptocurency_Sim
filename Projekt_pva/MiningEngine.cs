namespace Projekt_pva;

public class MiningEngine
{

    public double WalletBalance      { get; set; } = 10_000.0;
    public double CurrentTemperature { get; private set; } = 20.0;
    public Location CurrentLocation  { get; set; }

    private readonly MarketEngine _market;
    private readonly object _bufferLock = new();

    public Dictionary<CryptoCurrency, double> CoinsMinedBuffer { get; private set; } =
        Enum.GetValues<CryptoCurrency>().ToDictionary(c => c, _ => 0.0);

    public Dictionary<CryptoCurrency, double> NetworkDifficulties { get; private set; } = new()
    {
        { CryptoCurrency.BTC,          100_000.0 },
        { CryptoCurrency.ETH,            5_000.0 },
        { CryptoCurrency.SOL,            2_000.0 },
        { CryptoCurrency.DOGE,             500.0 },
        { CryptoCurrency.HawkTuahCoin,      10.0 },
    };

    public Dictionary<CryptoCurrency, double> Prices => _market.Prices;
    public bool IsRunning { get; private set; }

    public MiningEngine(MarketEngine market)
    {
        _market         = market;
        CurrentLocation = LocationStore.BuyLocation("Garage");
    }

    public void Start()
    {
        IsRunning = true;
        Task.Run(GameLoop);
    }

    public void FlushCoinsIntoWallet(Dictionary<CryptoCurrency, double> wallet)
    {
        lock (_bufferLock)
        {
            foreach (var coin in CoinsMinedBuffer.Keys.ToList())
            {
                wallet[coin]          += CoinsMinedBuffer[coin];
                CoinsMinedBuffer[coin] = 0;
            }
        }
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
        double totalConsumptionWh = 0;
        foreach (var hardware in CurrentLocation.Rigs.ToList())
        {
            totalConsumptionWh += hardware.Consumption * (hardware is MiningHardware { IsOverclocked: true } ? 1.5 : 1.0);
        }

        double currentLoadKW = totalConsumptionWh / 1000.0;
        bool powerTripped = currentLoadKW > CurrentLocation.PowerLimit;
        
        double totalHeatGen       = 0;
        double totalCoolingPower  = CurrentLocation.CoolingCapacity;

        var tickCoins = Enum.GetValues<CryptoCurrency>().ToDictionary(c => c, _ => 0.0);
        if (!powerTripped)
        {
            foreach (var hardware in CurrentLocation.Rigs.ToList())
            {
                if (hardware is MiningHardware mhw)
                {
                    ProcessHardware(mhw, tickCoins, ref totalConsumptionWh, ref totalHeatGen);
                }
                else if (hardware is RigHardware rhw)
                {
                    totalConsumptionWh += rhw.Consumption;
                    totalHeatGen += rhw.HeatOutput;
                    foreach (var card in rhw.Cards)
                        ProcessHardware(card, tickCoins, ref totalConsumptionWh, ref totalHeatGen);
                }
                else if (hardware is CoolingUnit cu)
                {
                    totalCoolingPower += cu.CoolingPower;
                    totalConsumptionWh += cu.Consumption;
                }
            }

            lock (_bufferLock)
            {
                foreach (var coin in tickCoins.Keys)
                    CoinsMinedBuffer[coin] += tickCoins[coin];
            }
        }

        double thermalMass = CurrentLocation.Size * 1.5;
        double heatDelta   = (totalHeatGen - totalCoolingPower) / thermalMass;
        CurrentTemperature = Math.Clamp(
            CurrentTemperature + heatDelta - (CurrentTemperature - 20) * 0.05,
            20, 120
        );

        WalletBalance -= (totalConsumptionWh / 10.0) * (CurrentLocation.ElectricityPrice / 3600.0);

        foreach (var coin in NetworkDifficulties.Keys.ToList())
            NetworkDifficulties[coin] *= (1 + Prices[coin] * 0.0000001);
    }

    private void ProcessHardware(MiningHardware hw, Dictionary<CryptoCurrency, double> tickCoins,
                                  ref double consumption, ref double heat)
    {
        double throttle = CurrentTemperature > 80
            ? Math.Max(0.1, 1.0 - (CurrentTemperature - 80) / 40.0)
            : 1.0;

        double ocMult = hw.IsOverclocked ? 1.4 : 1.0;

        double coinAmount = (hw.Hashrate * ocMult * throttle * (hw.Condition / 100.0))
                            / (NetworkDifficulties[hw.SelectedCoin] * 3600.0);

        tickCoins[hw.SelectedCoin] += coinAmount * 10;

        consumption += hw.Consumption * (hw.IsOverclocked ? 1.5 : 1.0);
        heat        += hw.HeatOutput  * (hw.IsOverclocked ? 2.0 : 1.0) * throttle;

        hw.Condition -= CurrentTemperature > 95 ? 0.05 : 0.001;
        hw.Condition  = Math.Max(0, hw.Condition);
    }
}
