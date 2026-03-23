namespace Projekt_pva;

public class MiningEngine
{
    public double WalletBalance { get; set; } = 10000.0;
    public double CurrentTemperature { get; private set; } = 20.0;
    public Location? CurrentLocation { get; set; }
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
            if (CurrentLocation != null)
            {
                UpdateSimulation();
            }
            Thread.Sleep(1000);
        }
    }

    private void UpdateSimulation()
    {
        double totalProfit = 0;
        double totalConsumption = 0;
        double totalHeat = 0;

        foreach (var rig in CurrentLocation!.Rigs)
        {
            double difficulty = NetworkDifficulties[rig.SelectedCoin];
            double effectiveHashrate = rig.Hashrate * (rig.Condition / 100.0);
            
            double minedAmount = effectiveHashrate / difficulty; 
            totalProfit += minedAmount * Prices[rig.SelectedCoin];
            
            totalConsumption += rig.Consumption;
            totalHeat += rig.HeatOutput;
            rig.Condition -= 0.001;
        }

        double electricityCost = (totalConsumption / 1000.0) * (CurrentLocation.ElectricityPrice / 3600.0);
        WalletBalance += (totalProfit - electricityCost);

        double heatDiff = totalHeat - CurrentLocation.CoolingCapacity;
        CurrentTemperature += heatDiff * 0.01;
        if (CurrentTemperature > 22) CurrentTemperature -= 0.05;

        foreach (var coin in NetworkDifficulties.Keys.ToList())
        {
            NetworkDifficulties[coin] += (Prices[coin] * 0.00001);
        }
    }
}