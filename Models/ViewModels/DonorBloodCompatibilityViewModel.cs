using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class DonorBloodCompatibilityViewModel
    {
        public List<BloodGroupCompatibilityItem> BloodGroups { get; set; } = new();
    }

    // UNIQUE NAME to avoid any conflicts with existing models
    public class BloodGroupCompatibilityItem
    {
        public string BloodGroup { get; set; }
        public List<string> CanDonateTo { get; set; } = new();
        public List<string> CanReceiveFrom { get; set; } = new();
        public string ColorTheme { get; set; } // Bootstrap color class (e.g., danger, primary)
        public string SpecialRole { get; set; } // e.g., Universal Donor, Universal Receiver
    }
}
