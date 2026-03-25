namespace Projekt_pva;

public static class LocationStore
{
    public static Location BuyLocation(string locationName)
    {
        return locationName switch
        {
            "Garage" => new Location("Garage", 10.5, 0.15, 5, 4000),
            "BeachHouse" => new Location("Beach House", 25.0, 0.25, 12, 25000),
            "WareHouse" => new Location("Warehouse", 200.0, 0.10, 100, 150000),
            "SuperDuperUltimateLagreUnlimitedHouse" => new Location("Ultimate Mansion", 999.0, 0.05, 500, 1000000),
            _ => throw new ArgumentException($"Location '{locationName}' not found!")
        };
    }

    public static List<string> GetAvailableLocations() 
    {
        return new List<string> { "Garage", "BeachHouse", "WareHouse", "SuperDuperUltimateLagreUnlimitedHouse" };
    }
}