using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using SmartRecyclingRewardsSystem.Models;
using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;

namespace SmartRecyclingRewardsSystem.Data
{
    public class ApplicationDbInitializer 
    {
        public void SeedFromMigrations(ApplicationDbContext context)
        {
            SeedRoles(context);
            SeedAdminUser(context);
            SeedBadges(context);
            SeedSystemConfig(context);
            SeedRewards(context);

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
                new Badge { Name = "Points Champion", Description = "Earned 1000 points total", Criteria = "total_points_1000", IconClass = "fa fa-star" },
                new Badge { Name = "Top Recycler", Description = "Ranked #1 on the monthly community leaderboard", Criteria = "monthly_rank_1", IconClass = "fa fa-crown" }
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

        private void SeedRewards(ApplicationDbContext context)
        {
            context.Rewards.AddOrUpdate(r => r.Name,
                new Reward { Name = "R50 Airtime Voucher", Description = "R50 airtime for any major SA network", PointsCost = 500, IsActive = true, ImageUrl = "/Content/images/rewards/airtime50.png" },
                new Reward { Name = "R100 Airtime Voucher", Description = "R100 airtime for any major SA network", PointsCost = 950, IsActive = true, ImageUrl = "/Content/images/rewards/airtime100.png" },
                new Reward { Name = "R50 Woolworths Voucher", Description = "R50 gift voucher redeemable at Woolworths", PointsCost = 600, IsActive = true, ImageUrl = "/Content/images/rewards/woolworths50.png" },
                new Reward { Name = "R100 Checkers Voucher", Description = "R100 gift voucher redeemable at Checkers", PointsCost = 1100, IsActive = true, ImageUrl = "/Content/images/rewards/checkers100.png" },
                new Reward { Name = "Reusable Shopping Bag", Description = "Durable EcoRewards SA branded canvas tote", PointsCost = 200, IsActive = true, ImageUrl = "/Content/images/rewards/tote.png" },
                new Reward { Name = "Stainless Steel Water Bottle", Description = "500ml insulated eco-friendly bottle", PointsCost = 350, IsActive = true, ImageUrl = "/Content/images/rewards/bottle.png" },
                new Reward { Name = "Reusable Produce Bag Set", Description = "Set of 5 mesh produce bags for grocery shopping", PointsCost = 250, IsActive = true, ImageUrl = "/Content/images/rewards/produce-bags.png" },
                new Reward { Name = "Compost Starter Kit", Description = "Home composting bin and starter guide", PointsCost = 800, IsActive = true, ImageUrl = "/Content/images/rewards/compost-kit.png" },
                new Reward { Name = "R150 Uber Eats Voucher", Description = "R150 voucher for Uber Eats orders", PointsCost = 1400, IsActive = true, ImageUrl = "/Content/images/rewards/ubereats.png" },
                new Reward { Name = "Tree Planting Donation", Description = "Sponsor a tree planted in your name via a local NGO", PointsCost = 300, IsActive = true, ImageUrl = "/Content/images/rewards/tree.png" }
            );
            context.SaveChanges();
        }
    }
}