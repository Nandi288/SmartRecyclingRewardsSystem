namespace SmartRecyclingRewardsSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveVirtualCards : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.VirtualCards", "UserId", "dbo.AspNetUsers");
            DropIndex("dbo.VirtualCards", new[] { "UserId" });
            DropTable("dbo.VirtualCards");
        }
        
        public override void Down()
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
                .PrimaryKey(t => t.VirtualCardId);
            
            CreateIndex("dbo.VirtualCards", "UserId");
            AddForeignKey("dbo.VirtualCards", "UserId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
        }
    }
}
