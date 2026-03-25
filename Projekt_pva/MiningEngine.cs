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
        double totalConsumption = 0;
        double totalHeat = 0;

        foreach (var hardware in CurrentLocation.Rigs)
        {
            if (hardware is MiningHardware mhw)
            {
                totalMinedUsd += CalculateHardwareMining(mhw);
                totalConsumption += mhw.Consumption;
                totalHeat += mhw.HeatOutput;
                mhw.Condition = Math.Max(0, mhw.Condition - 0.001);
            }
            else if (hardware is RigHardware rhw)
            {
                totalConsumption += rhw.Consumption;
                totalHeat += rhw.HeatOutput;
                foreach (var card in rhw.Cards)
                {
                    totalMinedUsd += CalculateHardwareMining(card);
                    totalConsumption += card.Consumption;
                    totalHeat += card.HeatOutput;
                    card.Condition = Math.Max(0, card.Condition - 0.001);
                }
            }
        }

        double electricityCost = (totalConsumption / 1000.0) * (CurrentLocation.ElectricityPrice / 3600.0);
        WalletBalance += (totalMinedUsd - electricityCost);

        double heatEffect = (totalHeat - CurrentLocation.CoolingCapacity) * 0.01;
        CurrentTemperature = Math.Max(20.0, CurrentTemperature + heatEffect);
        if (CurrentTemperature > 22) CurrentTemperature -= 0.05;

        foreach (var coin in NetworkDifficulties.Keys.ToList())
        {
            NetworkDifficulties[coin] += (Prices[coin] * 0.00001);
        }
    }

    private double CalculateHardwareMining(MiningHardware hw)
    {
        double difficulty = NetworkDifficulties[hw.SelectedCoin];
        double minedAmount = (hw.Hashrate * (hw.Condition / 100.0)) / difficulty;
        return minedAmount * Prices[hw.SelectedCoin];
    }
}