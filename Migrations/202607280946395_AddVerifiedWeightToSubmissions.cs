namespace SmartRecyclingRewardsSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddVerifiedWeightToSubmissions : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RecyclingSubmissions", "VerifiedWeightKg", c => c.Decimal(precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.RecyclingSubmissions", "VerifiedWeightKg");
        }
    }
}
