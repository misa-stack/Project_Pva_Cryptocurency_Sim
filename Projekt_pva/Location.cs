namespace Projekt_pva;

public class Location
{
        public string Name { get; set; }
        public double PowerLimit { get; set; } // Max Wattů, než vypadnou pojistky
        public double ElectricityPrice { get; set; } // Cena za kWh
        public double CoolingCapacity { get; set; } // Kolik tepla dokáže odvést nez bude muset se nainstalovat AC
        public int Size { get; set; } //kolik se tam vejde rigu
        public List<Hardware> Rigs { get; set; } = new();
}