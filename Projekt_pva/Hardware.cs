namespace Projekt_pva;

public abstract class Hardware
{
    public string Name { get; set; } 
    public double Consumption { get; set; }
    public double HeatOutput { get; set; }
    public double Condition { get; set; } = 100.0;
    public int Size { get; set; }
    public int Price { get; set; }

    protected Hardware(string name, double consumption, double heatOutput, int size, int price)
    {
        Name = name;
        Consumption = consumption;
        HeatOutput = heatOutput;
        Size = size;
        Price = price;
    }


}

public class MiningHardware : Hardware
{
    public double Hashrate { get; set; }
    public CryptoCurrency SelectedCoin { get; set; } = CryptoCurrency.BTC;
    public MiningHardware(string name, double hashrate, double consumption, double heatOutput, int size, int price) 
        : base(name, consumption, heatOutput, size, price)
    {
        Hashrate = hashrate;
    }
    public void SelectCoin(CryptoCurrency cryptoCurrency)
    {
        SelectedCoin = cryptoCurrency;
    }
}

public class RigHardware : Hardware
{
    public List<MiningHardware> Cards { get; set; } = new();

    public RigHardware(string name, double consumption, double heatOutput, int size, int price) : base(name,
        consumption, heatOutput, size, price)
    {
    }
    public void AddCard(MiningHardware card)
    {
        Cards.Add(card);
    }
}