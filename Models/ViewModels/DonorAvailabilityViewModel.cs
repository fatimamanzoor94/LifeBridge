using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Khoon_e_Hayat.ViewModels
{
    public class DonorAvailabilityViewModel
    {
        [Display(Name = "Donation Status")]
        public bool IsAvailable { get; set; }

        [Display(Name = "Last Donation Date")]
        [DataType(DataType.Date)]
        public DateTime? LastDonationDate { get; set; }

        [Display(Name = "Preferred Area / City")]
        public string PreferredArea { get; set; }

        // Populated by the controller to render the dropdown options
        public List<string> AvailableAreas { get; set; } = new();
    }
}