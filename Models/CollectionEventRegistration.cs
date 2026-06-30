using System;
using System.ComponentModel.DataAnnotations;

namespace SmartRecyclingRewardsSystem.Models
{
    public class CollectionEventRegistration
    {
        public int CollectionEventRegistrationId { get; set; }

        [Required]
        public string ResidentId { get; set; }
        public virtual ApplicationUser Resident { get; set; }

        [Required]
        public int CollectionEventId { get; set; }
        public virtual CollectionEvent CollectionEvent { get; set; }

        public DateTime RegisteredAt { get; set; }
        public bool Attended { get; set; }

        public CollectionEventRegistration()
        {
            RegisteredAt = DateTime.Now;
            Attended = false;
        }
    }
}
