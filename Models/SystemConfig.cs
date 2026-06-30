using System;
using System.ComponentModel.DataAnnotations;

namespace SmartRecyclingRewardsSystem.Models
{
    public class SystemConfig
    {
        public int SystemConfigId { get; set; }

        [Required, StringLength(100)]
        public string Key { get; set; }

        [Required, StringLength(500)]
        public string Value { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime LastUpdated { get; set; }
        public string UpdatedByAdminId { get; set; }

        public SystemConfig()
        {
            LastUpdated = DateTime.Now;
        }
    }
}
