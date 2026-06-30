using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using SmartRecyclingRewardsSystem.Models;
using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;

namespace SmartRecyclingRewardsSystem.Data
{
    public class ApplicationDbInitializer : CreateDatabaseIfNotExists<ApplicationDbContext>
    {
        protected override void Seed(ApplicationDbContext context)
        {
            SeedRoles(context);
            SeedAdminUser(context);
            SeedMaterialTypes(context);
            SeedBadges(context);
            SeedSystemConfig(context);
            SeedDropOffPoints(context);

            base.Seed(context);
        }

        private void SeedRoles(ApplicationDbContext context)
        {
            var roleStore = new RoleStore<IdentityRole>(context);
            var roleManager = new RoleManager<IdentityRole>(roleStore);

            string[] roles = { "Admin", "CollectionOfficer", "Resident" };
            foreach (var role in roles)
            {
                if (!roleManager.RoleExists(role))
                    roleManager.Create(new IdentityRole(role));
            }
        }

        // Email: admin@smartrecycling.co.za / Password: Admin@123
        private void SeedAdminUser(ApplicationDbContext context)
        {
            var userStore = new UserStore<ApplicationUser>(context);
            var userManager = new UserManager<ApplicationUser>(userStore);

            const string adminEmail = "admin@smartrecycling.co.za";
            const string adminPassword = "Admin@123";

            if (userManager.FindByEmail(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    Role = "Admin",
                    IsActive = true,
                    EmailConfirmed = true,
                    ReceiveEmailNotifications = true,
                    ReceiveSmsNotifications = false
                };

                var result = userManager.Create(admin, adminPassword);
                if (result.Succeeded)
                    userManager.AddToRole(admin.Id, "Admin");
            }
        }

        private void SeedMaterialTypes(ApplicationDbContext context)
        {
            context.MaterialTypes.AddOrUpdate(m => m.Name,
                new MaterialType { Name = "Paper", PointsPerKg = 3m, CO2SavingPerKg = 1.0842m, ColourCode = "#f59e0b", IsActive = true },
                new MaterialType { Name = "Plastic", PointsPerKg = 4m, CO2SavingPerKg = 1.5300m, ColourCode = "#3b82f6", IsActive = true },
                new MaterialType { Name = "Glass", PointsPerKg = 5m, CO2SavingPerKg = 0.3140m, ColourCode = "#10b981", IsActive = true },
                new MaterialType { Name = "Metal", PointsPerKg = 7m, CO2SavingPerKg = 4.0000m, ColourCode = "#6b7280", IsActive = true },
                new MaterialType { Name = "E-Waste", PointsPerKg = 10m, CO2SavingPerKg = 2.5000m, ColourCode = "#ef4444", IsActive = true }
            );
            context.SaveChanges();
        }

        private void SeedBadges(ApplicationDbContext context)
        {
            context.Badges.AddOrUpdate(b => b.Name,
                new Badge { Name = "First Drop", Description = "Submitted your first recycling entry", Criteria = "submissions_1", IconClass = "fa fa-recycle" },
                new Badge { Name = "5-Week Streak", Description = "Recycled for 5 consecutive weeks", Criteria = "streak_5", IconClass = "fa fa-fire" },
                new Badge { Name = "100kg Club", Description = "Recycled a total of 100 kg", Criteria = "total_weight_100", IconClass = "fa fa-trophy" },
                new Badge { Name = "E-Waste Hero", Description = "Submitted 10 kg or more of e-waste", Criteria = "ewaste_10kg", IconClass = "fa fa-bolt" },
                new Badge { Name = "Points Champion", Description = "Earned 1000 points total", Criteria = "total_points_1000", IconClass = "fa fa-star" }
            );
            context.SaveChanges();
        }

        private void SeedSystemConfig(ApplicationDbContext context)
        {
            context.SystemConfigs.AddOrUpdate(s => s.Key,
                new SystemConfig { Key = "AppName", Value = "EcoRewards SA", Description = "Application display name" },
                new SystemConfig { Key = "AppTagline", Value = "Recycle. Earn. Repeat.", Description = "Tagline shown on landing page" },
                new SystemConfig { Key = "MinRedemptionPoints", Value = "100", Description = "Minimum points needed to redeem a reward" },
                new SystemConfig { Key = "StreakBonusPoints", Value = "20", Description = "Bonus points for a 5-week recycling streak" },
                new SystemConfig { Key = "SmtpHost", Value = "smtp.gmail.com", Description = "SMTP server host" },
                new SystemConfig { Key = "SmtpPort", Value = "587", Description = "SMTP server port" },
                new SystemConfig { Key = "SmtpFromEmail", Value = "noreply@ecorewardssa.co.za", Description = "From address for outgoing emails" },
                new SystemConfig { Key = "ClickatellApiKey", Value = "", Description = "Clickatell API key for SMS notifications" }
            );
            context.SaveChanges();
        }

        private void SeedDropOffPoints(ApplicationDbContext context)
        {
            context.DropOffPoints.AddOrUpdate(d => d.Name,
                new DropOffPoint { Name = "North Coast Road Buy-Back Centre", Address = "1288 North Coast Road, Redhill, Durban", City = "Durban", OperatingHours = "Mon-Fri 07:00-16:30 | Sat 07:00-15:30", IsActive = true },
                new DropOffPoint { Name = "Westmead Buy-Back Centre", Address = "39 Westmead Road, Westmead, Pinetown", City = "Pinetown", OperatingHours = "Mon-Fri 07:00-16:30 | Sat 07:00-15:30", IsActive = true },
                new DropOffPoint { Name = "Lorne Street Buy-Back Centre", Address = "Lorne Street, Warwick Junction, Durban", City = "Durban", OperatingHours = "Mon-Fri 07:00-16:30 | Sat 07:00-15:30", IsActive = true },
                new DropOffPoint { Name = "Brook Street Buy-Back Centre", Address = "Brook Street, Durban CBD", City = "Durban", OperatingHours = "Mon-Fri 07:00-16:30 | Sat 07:00-15:30", IsActive = true },
                new DropOffPoint { Name = "New Germany Buy-Back Centre", Address = "Escom Road, New Germany, Durban", City = "New Germany", OperatingHours = "Mon-Fri 07:00-16:30 | Sat 07:00-15:30", IsActive = true },
                new DropOffPoint { Name = "KwaMashu Buy-Back Centre", Address = "Opposite Metrorail Station, KwaMashu", City = "KwaMashu", OperatingHours = "Mon-Fri 07:00-16:30 | Sat 07:00-15:30", IsActive = true },
                new DropOffPoint { Name = "Isipingo Buy-Back Centre", Address = "1029 Old Main Road, Isipingo", City = "Isipingo", OperatingHours = "Mon-Fri 07:00-16:30 | Sat 07:00-15:30", IsActive = true },
                new DropOffPoint { Name = "Nyati Road Garden Drop-Off Site", Address = "1 Nyati Road, Athlone Park, Amanzimtoti", City = "Amanzimtoti", OperatingHours = "Mon-Fri 07:00-16:30 | Sat-Sun 07:00-15:30", IsActive = true },
                new DropOffPoint { Name = "Mariannhill Landfill & Recycling Site", Address = "1 Landfill Lane, Mariannhill", City = "Mariannhill", OperatingHours = "Mon-Fri 07:00-16:30 | Sat-Sun 07:00-15:30", IsActive = true },
                new DropOffPoint { Name = "Bisasar Road Recycling Centre", Address = "1 Dhulam Road, Springfield, Durban", City = "Springfield", OperatingHours = "Mon-Fri 07:00-16:30 | Sat-Sun 07:00-15:30", IsActive = true },
                new DropOffPoint { Name = "EWaste Africa – Ladysmith Drop-Off", Address = "Cnr Francis & Hunter St, San Marco Centre, Ladysmith", City = "Ladysmith", OperatingHours = "Mon-Fri 08:00-17:00", IsActive = true }
            );
            context.SaveChanges();
        }
    }
}
