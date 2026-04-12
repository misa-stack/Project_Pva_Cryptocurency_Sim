namespace Projekt_pva;

public static class HardwareStore
{
    public static MiningHardware CreateGpu(string modelName)
    {
        return modelName switch
        {
            "RTX 4090"     => new MiningHardware("RTX 4090",     120.0,     450.0, 5.0,  2,  4_000),
            "RTX 3080"     => new MiningHardware("RTX 3080",      95.0,     320.0, 4.0,  2,  2_500),
            "GTX 1080 Ti"  => new MiningHardware("GTX 1080 Ti",   45.0,     250.0, 3.5,  2,  1_000),
            "Antminer S19" => new MiningHardware("Antminer S19", 95_000.0, 3_250.0, 15.0, 5, 10_000),
            _ => throw new ArgumentException($"Hardware model '{modelName}' not found!")
        };
    }

    public static CoolingUnit CreateCooling(string modelName)
    {
        return modelName switch
        {
            "Basic Fan"     => new CoolingUnit("Basic Fan",      2.0,    50.0, 1,   200),
            "Industrial AC" => new CoolingUnit("Industrial AC", 20.0, 1200.0, 3, 5_000),
            _ => throw new ArgumentException($"Cooling model '{modelName}' not found!")
        };
    }

    public static List<string> GetAvailableModels() =>
        new() { "RTX 4090", "RTX 3080", "GTX 1080 Ti", "Antminer S19" };

    public static List<string> GetAvailableCooling() =>
        new() { "Basic Fan", "Industrial AC" };
}
