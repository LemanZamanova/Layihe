namespace Timoto.Models
{
    public class CarFeature
    {
        public int CarId { get; set; }
        public Car Car { get; set; }

        public int FeatureId { get; set; }
        public Feature Feature { get; set; }
    }
}
