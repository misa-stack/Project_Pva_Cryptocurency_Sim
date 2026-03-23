namespace Projekt_pva;

public class MarketEngine
{
    private Random _rng = new Random();
    public Dictionary<CryptoCurrency, double> Prices { get; private set; } = new()
    {
        { CryptoCurrency.BTC, 65000.0 },
        { CryptoCurrency.ETH, 3500.0 },
        { CryptoCurrency.HawkTuahCoin, 0.00069 }
    };

    public bool IsRunning { get; private set; }

    public void Start()
    {
        IsRunning = true;
        Task.Run(() => MarketLoop());
    }

    private void MarketLoop()
    {
        while (IsRunning)
        {
            foreach (var coin in Prices.Keys.ToList())
            {
                if (coin == CryptoCurrency.HawkTuahCoin)
                { 
                    double nextDouble = 1 + (_rng.NextDouble() * 0.7 - 0.7); 
                    Prices[coin] *= nextDouble;
                    continue;
                }
                double change = 1 + (_rng.NextDouble() * 0.04 - 0.02); // +/- 2%
                Prices[coin] *= change;
            }
            Thread.Sleep(5000);
        }
    }
}