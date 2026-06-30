using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartRecyclingRewardsSystem.Models
{
    public class DropOffPoint
    {
        public int DropOffPointId { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Location Name")]
        public string Name { get; set; }

        [Required, StringLength(300)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(500)]
        [Display(Name = "Operating Hours")]
        public string OperatingHours { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public string AssignedOfficerId { get; set; }
        public virtual ApplicationUser AssignedOfficer { get; set; }

        public DropOffPoint()
        {
            IsActive = true;
            CreatedAt = DateTime.Now;
        }

        public virtual ICollection<RecyclingSubmission> RecyclingSubmissions { get; set; }
        public virtual ICollection<CollectionEvent> CollectionEvents { get; set; }
    }
}
