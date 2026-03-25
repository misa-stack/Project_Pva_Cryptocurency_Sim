namespace Projekt_pva;

public class Location
{
        public string Name { get; set; }
        public double PowerLimit { get; set; } 
        public double ElectricityPrice { get; set; } 
        public double CoolingCapacity { get; set; } 
        public int Size { get; set; } 
        
        public int Price { get; set; }
        public List<Hardware> Rigs { get; set; } = new();

        public Location(string name, double powerLimit, double electricityPrice, int size, int price)
        {
                Name = name;
                PowerLimit = powerLimit;
                ElectricityPrice = electricityPrice;
                Size = size;
                Price = price;
        }

        public void AddRig()
        {
                
        }
}