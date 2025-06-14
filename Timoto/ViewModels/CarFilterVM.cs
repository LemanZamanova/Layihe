using Timoto.Models;

namespace Timoto.ViewModels
{
    public class CarFilterVM
    {
        public List<int> SelectedBodyTypeIds { get; set; } = new();
        public List<int> SelectedSeatCounts { get; set; } = new();
        public List<string> SelectedEngineRanges { get; set; } = new(); // optional
        public List<int> SelectedVehicleTypeIds { get; set; } = new();

        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }

        // Car list to display
        public List<Car> Cars { get; set; }
    }
}
