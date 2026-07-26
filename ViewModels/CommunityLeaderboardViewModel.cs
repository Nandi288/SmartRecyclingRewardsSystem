using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SmartRecyclingRewardsSystem.ViewModels
{
    public class CommunityLeaderboardViewModel
    {
        public string FirstName { get; set; }   
        public string LastName { get; set; }
        public int TotalPoints { get; set; }    
    
        public decimal TotalRecycledkg { get; set; }
        public int Rank { get; set; }   
        public bool IsCurrentUser { get; set; } // Indicates if this entry belongs to the current user  
    }
}