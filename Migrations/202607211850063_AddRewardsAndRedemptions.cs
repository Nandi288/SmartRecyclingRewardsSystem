namespace SmartRecyclingRewardsSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRewardsAndRedemptions : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RewardRedemptions",
                c => new
                    {
                        RewardRedemptionId = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        RewardId = c.Int(nullable: false),
                        PointsSpent = c.Int(nullable: false),
                        RedemptionDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.RewardRedemptionId)
                .ForeignKey("dbo.Rewards", t => t.RewardId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RewardId);
            
            CreateTable(
                "dbo.Rewards",
                c => new
                    {
                        RewardId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 150),
                        Description = c.String(maxLength: 500),
                        PointsCost = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        ImageUrl = c.String(maxLength: 300),
                    })
                .PrimaryKey(t => t.RewardId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RewardRedemptions", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.RewardRedemptions", "RewardId", "dbo.Rewards");
            DropIndex("dbo.RewardRedemptions", new[] { "RewardId" });
            DropIndex("dbo.RewardRedemptions", new[] { "UserId" });
            DropTable("dbo.Rewards");
            DropTable("dbo.RewardRedemptions");
        }
    }
}
