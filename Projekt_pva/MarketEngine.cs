using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Projekt_pva;

public class MarketEngine
{
    private readonly Random _rng = new Random();
    public Dictionary<CryptoCurrency, double> Prices { get; private set; } = new();

    private double _marketSentiment = 1.0;
    private double _marketVolatility = 1.0;
    private int _cycleTicksRemaining = 0;
    public double MarketSentiment  { get => _marketSentiment;  set => _marketSentiment  = value; }
    public double MarketVolatility { get => _marketVolatility; set => _marketVolatility = value; }
    public int    CycleTicks       { get => _cycleTicksRemaining; set => _cycleTicksRemaining = value; }

    public bool IsRunning { get; private set; }

    public MarketEngine()
    {
        Prices[CryptoCurrency.BTC]          = 65000.0;
        Prices[CryptoCurrency.ETH]          = 3500.0;
        Prices[CryptoCurrency.SOL]          = 145.0;
        Prices[CryptoCurrency.DOGE]         = 0.12;
        Prices[CryptoCurrency.HawkTuahCoin] = 0.00069;
    }

    public void Start()
    {
        IsRunning = true;
        Task.Run(() => MarketLoop());
    }

    private void MarketLoop()
    {
        while (IsRunning)
        {
            UpdateMarketConditions();
            ApplyPriceChanges();
            Thread.Sleep(4000);
        }
    }

    private void UpdateMarketConditions()
    {
        if (_cycleTicksRemaining <= 0)
        {
            _cycleTicksRemaining = _rng.Next(20, 50);
            _marketSentiment     = 0.5 + (_rng.NextDouble() * 1.2);
            _marketVolatility    = 0.8 + (_rng.NextDouble() * 1.5);
        }
        _cycleTicksRemaining--;

        if (_rng.NextDouble() < 0.005)
        {
            bool isPositive      = _rng.NextDouble() > 0.5;
            _marketSentiment     = isPositive ? 5.0 : 0.1;
            _cycleTicksRemaining = 3;
        }
    }

    private void ApplyPriceChanges()
    {
        double btcMovement = 0;

        foreach (var coin in Prices.Keys.ToList())
        {
            double baseChange;
            double volatility;

            switch (coin)
            {
                case CryptoCurrency.BTC:
                    volatility  = 0.015 * _marketVolatility;
                    baseChange  = (_rng.NextDouble() * 2 - 1) * volatility + (_marketSentiment - 1) * 0.01;
                    btcMovement = baseChange;
                    break;

                case CryptoCurrency.HawkTuahCoin:
                    volatility = 0.15 * _marketVolatility;
                    baseChange = (_rng.NextDouble() * 2 - 1) * volatility + (_rng.NextDouble() * 0.1 - 0.05);
                    break;

                default:
                    volatility = 0.03 * _marketVolatility;
                    double correlation     = 0.7;
                    double independentMove = (_rng.NextDouble() * 2 - 1) * volatility;
                    baseChange = (btcMovement * correlation) + (independentMove * (1 - correlation));
                    break;
            }

            double finalPrice = Prices[coin] * (1 + baseChange);
            Prices[coin] = Math.Max(0.00000001, finalPrice);
        }
    }

    public string GetMarketTrend()
    {
        if (_marketSentiment > 1.3)  return "MEGA BULLISH";
        if (_marketSentiment > 1.05) return "BULLISH";
        if (_marketSentiment < 0.7)  return "CRASHING";
        if (_marketSentiment < 0.95) return "BEARISH";
        return "SIDEWAYS";
    }
}
