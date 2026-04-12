namespace Projekt_pva;

public class Location
{
    public string Name             { get; set; }
    public double PowerLimit       { get; set; }
    public double ElectricityPrice { get; set; }
    public double CoolingCapacity  { get; set; }
    public int    Size             { get; set; }
    public int    Price            { get; set; }
    public List<Hardware> Rigs     { get; set; } = new();

    public Location(string name, double powerLimit, double electricityPrice,
                    int size, int price, double coolingCapacity = 0)
    {
        Name             = name;
        PowerLimit       = powerLimit;
        ElectricityPrice = electricityPrice;
        Size             = size;
        Price            = price;
        CoolingCapacity  = coolingCapacity;
    }

    public bool AddRig(Hardware hardware)
    {
        if (Rigs.Count >= Size) return false;
        Rigs.Add(hardware);
        return true;
    }
}
