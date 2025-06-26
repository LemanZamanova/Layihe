public class CarDetailVM
{
    public string MainImage { get; set; }
    public List<string> AdditionalImages { get; set; }

    public string Name { get; set; }
    public decimal DailyPrice { get; set; }
    public int Year { get; set; }
    public string Description { get; set; }

    public int Seats { get; set; }
    public int Doors { get; set; }
    public int LuggageVolume { get; set; }
    public int EngineSize { get; set; }
    public int Mileage { get; set; }
    public double FuelEconomy { get; set; }

    public string ExteriorColor { get; set; }
    public string InteriorColor { get; set; }

    public string Location { get; set; }
    public string Latitude { get; set; }
    public string Longitude { get; set; }

    public string FuelTypeName { get; set; }
    public string TransmissionTypeName { get; set; }
    public string DriveTypeName { get; set; }
    public string BodyTypeName { get; set; }
    public string VehicleTypeName { get; set; }

    public List<string> Features { get; set; }
}
