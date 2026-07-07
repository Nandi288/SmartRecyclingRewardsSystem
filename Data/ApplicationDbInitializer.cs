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
            SeedBadges(context);
            SeedSystemConfig(context);

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
                new SystemConfig
                {
                    Key = "AppName",
                    Value = "EcoRewards SA",
                    Description = "Application display name",
                    LastUpdated = DateTime.Now
                },
                new SystemConfig
                {
                    Key = "AppTagline",
                    Value = "Recycle. Earn. Repeat.",
                    Description = "Tagline shown on landing page",
                    LastUpdated = DateTime.Now
                },
                new SystemConfig
                {
                    Key = "MinRedemptionPoints",
                    Value = "100",
                    Description = "Minimum points needed to redeem a reward",
                    LastUpdated = DateTime.Now
                },
                new SystemConfig
                {
                    Key = "StreakBonusPoints",
                    Value = "20",
                    Description = "Bonus points for a 5-week recycling streak",
                    LastUpdated = DateTime.Now
                },
                new SystemConfig
                {
                    Key = "SmtpHost",
                    Value = "smtp.gmail.com",
                    Description = "SMTP server host",
                    LastUpdated = DateTime.Now
                },
                new SystemConfig
                {
                    Key = "SmtpPort",
                    Value = "587",
                    Description = "SMTP server port",
                    LastUpdated = DateTime.Now
                },
                new SystemConfig
                {
                    Key = "SmtpFromEmail",
                    Value = "noreply@ecorewardssa.co.za",
                    Description = "From address for outgoing emails",
                    LastUpdated = DateTime.Now
                },
                new SystemConfig
                {
                    Key = "ClickatellApiKey",
                    Value = "UNSET",
                    Description = "Clickatell API key for SMS notifications",
                    LastUpdated = DateTime.Now
                }
            );
            context.SaveChanges();
        }
    }
}