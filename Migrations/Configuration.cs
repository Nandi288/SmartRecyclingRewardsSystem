namespace SmartRecyclingRewardsSystem.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<SmartRecyclingRewardsSystem.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(SmartRecyclingRewardsSystem.Models.ApplicationDbContext context)
        {
            // Call your existing seeder
            var seeder = new SmartRecyclingRewardsSystem.Data.ApplicationDbInitializer();
            seeder.SeedFromMigrations(context);
        }
    }
}
