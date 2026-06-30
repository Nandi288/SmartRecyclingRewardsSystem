using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartRecyclingRewardsSystem.Models
{
    public class CollectionEvent
    {
        public int CollectionEventId { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; }

        public DateTime? EndTime { get; set; }

        [Required]
        public int DropOffPointId { get; set; }
        public virtual DropOffPoint DropOffPoint { get; set; }

        public int? MaxRegistrations { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public CollectionEvent()
        {
            IsActive = true;
            CreatedAt = DateTime.Now;
        }

        public virtual ICollection<CollectionEventRegistration> Registrations { get; set; }
    }
}
