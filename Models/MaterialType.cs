using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartRecyclingRewardsSystem.Models
{
    public class MaterialType
    {
        public int MaterialTypeId { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Material Name")]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Points Per Kg")]
        [Column(TypeName = "decimal")]
        public decimal PointsPerKg { get; set; }

        [Required]
        [Display(Name = "CO2 Saving Per Kg")]
        [Column(TypeName = "decimal")]
        public decimal CO2SavingPerKg { get; set; }

        [StringLength(20)]
        [Display(Name = "Chart Colour")]
        public string ColourCode { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public MaterialType()
        {
            IsActive = true;
            CreatedAt = DateTime.Now;
        }

        public virtual ICollection<RecyclingSubmission> RecyclingSubmissions { get; set; }
    }
}
