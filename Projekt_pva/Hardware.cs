namespace Projekt_pva;

public abstract class Hardware
{
    public string Name { get; set; } = "";
    public double Hashrate { get; set; }
    public double Consumption { get; set; }
    public double HeatOutput { get; set; }
    public double Condition { get; set; } = 100.0;
    public int Size { get; set; }
    public int Price { get; set; }
    public CryptoCurrency SelectedCoin { get; set; } = CryptoCurrency.BTC;

    protected Hardware(string name, double hashrate, double consumption, double heatOutput, int size, int price)
    {
        Name = name;
        Hashrate = hashrate;
        Consumption = consumption;
        HeatOutput = heatOutput;
        Size = size;
        Price = price;
    }

    // Přidáme metodu přímo sem, aby byla dostupná pro všechny podtřídy
    public void SelectCoin(CryptoCurrency cryptoCurrency)
    {
        SelectedCoin = cryptoCurrency;
    }
}

public class MiningHardware : Hardware
{
    public MiningHardware(string name, double hashrate, double consumption, double heatOutput, int size, int price) 
        : base(name, hashrate, consumption, heatOutput, size, price)
    {
    }
}