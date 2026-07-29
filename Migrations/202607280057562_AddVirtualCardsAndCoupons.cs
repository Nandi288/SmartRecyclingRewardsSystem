namespace SmartRecyclingRewardsSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddVirtualCardsAndCoupons : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.VirtualCards",
                c => new
                    {
                        VirtualCardId = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        CardNumber = c.String(nullable: false, maxLength: 19),
                        CardHolderName = c.String(nullable: false, maxLength: 50),
                        ExpiryDate = c.String(nullable: false, maxLength: 5),
                        LastFourDigits = c.String(maxLength: 4),
                        IsDefault = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        LastUsedDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.VirtualCardId)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            AddColumn("dbo.RewardRedemptions", "CouponCode", c => c.String(maxLength: 20));
            AddColumn("dbo.RewardRedemptions", "CouponDetails", c => c.String(maxLength: 200));
            AddColumn("dbo.RewardRedemptions", "CouponExpiryDate", c => c.DateTime());
            AddColumn("dbo.RewardRedemptions", "Status", c => c.String(maxLength: 50));
            AddColumn("dbo.RewardRedemptions", "TransactionId", c => c.String(maxLength: 50));
            AddColumn("dbo.RewardRedemptions", "PaymentMethod", c => c.String(maxLength: 20));
            AddColumn("dbo.RewardRedemptions", "CompletionDate", c => c.DateTime());
            AddColumn("dbo.RewardRedemptions", "UsedDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.VirtualCards", "UserId", "dbo.AspNetUsers");
            DropIndex("dbo.VirtualCards", new[] { "UserId" });
            DropColumn("dbo.RewardRedemptions", "UsedDate");
            DropColumn("dbo.RewardRedemptions", "CompletionDate");
            DropColumn("dbo.RewardRedemptions", "PaymentMethod");
            DropColumn("dbo.RewardRedemptions", "TransactionId");
            DropColumn("dbo.RewardRedemptions", "Status");
            DropColumn("dbo.RewardRedemptions", "CouponExpiryDate");
            DropColumn("dbo.RewardRedemptions", "CouponDetails");
            DropColumn("dbo.RewardRedemptions", "CouponCode");
            DropTable("dbo.VirtualCards");
        }
    }
}
