namespace SmartRecyclingRewardsSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AspNetRoles",
                c => new
                {
                    Id = c.String(nullable: false, maxLength: 128),
                    Name = c.String(nullable: false, maxLength: 256),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");

            CreateTable(
                "dbo.AspNetUsers",
                c => new
                {
                    Id = c.String(nullable: false, maxLength: 128),
                    FirstName = c.String(nullable: false, maxLength: 50),
                    LastName = c.String(nullable: false, maxLength: 50),
                    Address = c.String(maxLength: 200),
                    City = c.String(maxLength: 100),
                    Role = c.String(nullable: false, maxLength: 20),
                    DateJoined = c.DateTime(nullable: false),
                    IsActive = c.Boolean(nullable: false),
                    PointsBalance = c.Int(nullable: false),
                    ReceiveEmailNotifications = c.Boolean(nullable: false),
                    ReceiveSmsNotifications = c.Boolean(nullable: false),
                    Email = c.String(maxLength: 256),
                    EmailConfirmed = c.Boolean(nullable: false),
                    PasswordHash = c.String(),
                    SecurityStamp = c.String(),
                    PhoneNumber = c.String(),
                    PhoneNumberConfirmed = c.Boolean(nullable: false),
                    TwoFactorEnabled = c.Boolean(nullable: false),
                    LockoutEndDateUtc = c.DateTime(),
                    LockoutEnabled = c.Boolean(nullable: false),
                    AccessFailedCount = c.Int(nullable: false),
                    UserName = c.String(nullable: false, maxLength: 256),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");

            CreateTable(
                "dbo.Badges",
                c => new
                {
                    BadgeId = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 100),
                    Description = c.String(maxLength: 500),
                    Criteria = c.String(maxLength: 100),
                    IconClass = c.String(maxLength: 500),
                })
                .PrimaryKey(t => t.BadgeId);

            CreateTable(
                "dbo.MaterialTypes",
                c => new
                {
                    MaterialTypeId = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 100),
                    Description = c.String(maxLength: 500),
                    PointsPerKg = c.Decimal(nullable: false, precision: 10, scale: 2),
                    CO2SavingPerKg = c.Decimal(nullable: false, precision: 10, scale: 4),
                    ColourCode = c.String(maxLength: 20),
                    IsActive = c.Boolean(nullable: false),
                    CreatedAt = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.MaterialTypeId);

            CreateTable(
                "dbo.DropOffPoints",
                c => new
                {
                    DropOffPointId = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 150),
                    Address = c.String(nullable: false, maxLength: 300),
                    City = c.String(maxLength: 100),
                    OperatingHours = c.String(maxLength: 500),
                    IsActive = c.Boolean(nullable: false),
                    CreatedAt = c.DateTime(nullable: false),
                    AssignedOfficerId = c.String(maxLength: 128),
                })
                .PrimaryKey(t => t.DropOffPointId)
                .ForeignKey("dbo.AspNetUsers", t => t.AssignedOfficerId)
                .Index(t => t.AssignedOfficerId);

            CreateTable(
                "dbo.CollectionEvents",
                c => new
                {
                    CollectionEventId = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 200),
                    Description = c.String(maxLength: 1000),
                    EventDate = c.DateTime(nullable: false),
                    EndTime = c.DateTime(),
                    DropOffPointId = c.Int(nullable: false),
                    MaxRegistrations = c.Int(),
                    IsActive = c.Boolean(nullable: false),
                    CreatedAt = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.CollectionEventId)
                .ForeignKey("dbo.DropOffPoints", t => t.DropOffPointId, cascadeDelete: true)
                .Index(t => t.DropOffPointId);

            CreateTable(
                "dbo.UserBadges",
                c => new
                {
                    UserBadgeId = c.Int(nullable: false, identity: true),
                    UserId = c.String(nullable: false, maxLength: 128),
                    BadgeId = c.Int(nullable: false),
                    EarnedAt = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.UserBadgeId)
                .ForeignKey("dbo.Badges", t => t.BadgeId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => new { t.UserId, t.BadgeId }, unique: true);

            CreateTable(
                "dbo.CollectionEventRegistrations",
                c => new
                {
                    CollectionEventRegistrationId = c.Int(nullable: false, identity: true),
                    ResidentId = c.String(nullable: false, maxLength: 128),
                    CollectionEventId = c.Int(nullable: false),
                    RegisteredAt = c.DateTime(nullable: false),
                    Attended = c.Boolean(nullable: false),
                })
                .PrimaryKey(t => t.CollectionEventRegistrationId)
                .ForeignKey("dbo.CollectionEvents", t => t.CollectionEventId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.ResidentId)
                .Index(t => new { t.ResidentId, t.CollectionEventId }, unique: true);

            CreateTable(
                "dbo.RecyclingSubmissions",
                c => new
                {
                    RecyclingSubmissionId = c.Int(nullable: false, identity: true),
                    ResidentId = c.String(nullable: false, maxLength: 128),
                    MaterialTypeId = c.Int(nullable: false),
                    DropOffPointId = c.Int(nullable: false),
                    WeightKg = c.Decimal(nullable: false, precision: 10, scale: 2),
                    SubmissionDate = c.DateTime(nullable: false),
                    Status = c.Int(nullable: false),
                    VerifiedByOfficerId = c.String(maxLength: 128),
                    ProcessedAt = c.DateTime(),
                    RejectionReason = c.String(maxLength: 500),
                    PointsAwarded = c.Int(nullable: false),
                    CO2SavedKg = c.Decimal(nullable: false, precision: 10, scale: 4),
                    Notes = c.String(maxLength: 1000),
                })
                .PrimaryKey(t => t.RecyclingSubmissionId)
                .ForeignKey("dbo.DropOffPoints", t => t.DropOffPointId, cascadeDelete: true)
                .ForeignKey("dbo.MaterialTypes", t => t.MaterialTypeId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.ResidentId)
                .ForeignKey("dbo.AspNetUsers", t => t.VerifiedByOfficerId)
                .Index(t => t.ResidentId)
                .Index(t => t.MaterialTypeId)
                .Index(t => t.DropOffPointId)
                .Index(t => t.VerifiedByOfficerId);

            CreateTable(
                "dbo.Notifications",
                c => new
                {
                    NotificationId = c.Int(nullable: false, identity: true),
                    UserId = c.String(nullable: false, maxLength: 128),
                    NotificationType = c.Int(nullable: false),
                    Title = c.String(nullable: false, maxLength: 200),
                    Message = c.String(nullable: false, maxLength: 1000),
                    IsRead = c.Boolean(nullable: false),
                    EmailSent = c.Boolean(nullable: false),
                    SmsSent = c.Boolean(nullable: false),
                    CreatedAt = c.DateTime(nullable: false),
                    RecyclingSubmissionId = c.Int(),
                })
                .PrimaryKey(t => t.NotificationId)
                .ForeignKey("dbo.RecyclingSubmissions", t => t.RecyclingSubmissionId)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId)
                .Index(t => t.UserId)
                .Index(t => t.RecyclingSubmissionId);

            CreateTable(
                "dbo.PointTransactions",
                c => new
                {
                    PointTransactionId = c.Int(nullable: false, identity: true),
                    UserId = c.String(nullable: false, maxLength: 128),
                    TransactionType = c.Int(nullable: false),
                    Points = c.Int(nullable: false),
                    BalanceAfter = c.Int(nullable: false),
                    Description = c.String(maxLength: 500),
                    TransactionDate = c.DateTime(nullable: false),
                    RecyclingSubmissionId = c.Int(),
                })
                .PrimaryKey(t => t.PointTransactionId)
                .ForeignKey("dbo.RecyclingSubmissions", t => t.RecyclingSubmissionId)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId)
                .Index(t => t.UserId)
                .Index(t => t.RecyclingSubmissionId);

            CreateTable(
                "dbo.AspNetUserClaims",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    UserId = c.String(nullable: false, maxLength: 128),
                    ClaimType = c.String(),
                    ClaimValue = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);

            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                {
                    LoginProvider = c.String(nullable: false, maxLength: 128),
                    ProviderKey = c.String(nullable: false, maxLength: 128),
                    UserId = c.String(nullable: false, maxLength: 128),
                })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);

            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                {
                    UserId = c.String(nullable: false, maxLength: 128),
                    RoleId = c.String(nullable: false, maxLength: 128),
                })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);

            CreateTable(
                "dbo.SystemConfigs",
                c => new
                {
                    SystemConfigId = c.Int(nullable: false, identity: true),
                    Key = c.String(nullable: false, maxLength: 100),
                    Value = c.String(nullable: false, maxLength: 500),
                    Description = c.String(maxLength: 500),
                    LastUpdated = c.DateTime(nullable: false),
                    UpdatedByAdminId = c.String(),
                })
                .PrimaryKey(t => t.SystemConfigId);
        }

        public override void Down()
        {
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.PointTransactions", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.PointTransactions", "RecyclingSubmissionId", "dbo.RecyclingSubmissions");
            DropForeignKey("dbo.Notifications", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Notifications", "RecyclingSubmissionId", "dbo.RecyclingSubmissions");
            DropForeignKey("dbo.RecyclingSubmissions", "VerifiedByOfficerId", "dbo.AspNetUsers");
            DropForeignKey("dbo.RecyclingSubmissions", "ResidentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.RecyclingSubmissions", "MaterialTypeId", "dbo.MaterialTypes");
            DropForeignKey("dbo.RecyclingSubmissions", "DropOffPointId", "dbo.DropOffPoints");
            DropForeignKey("dbo.CollectionEventRegistrations", "ResidentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.CollectionEventRegistrations", "CollectionEventId", "dbo.CollectionEvents");
            DropForeignKey("dbo.UserBadges", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.UserBadges", "BadgeId", "dbo.Badges");
            DropForeignKey("dbo.CollectionEvents", "DropOffPointId", "dbo.DropOffPoints");
            DropForeignKey("dbo.DropOffPoints", "AssignedOfficerId", "dbo.AspNetUsers");
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.PointTransactions", new[] { "RecyclingSubmissionId" });
            DropIndex("dbo.PointTransactions", new[] { "UserId" });
            DropIndex("dbo.Notifications", new[] { "RecyclingSubmissionId" });
            DropIndex("dbo.Notifications", new[] { "UserId" });
            DropIndex("dbo.RecyclingSubmissions", new[] { "VerifiedByOfficerId" });
            DropIndex("dbo.RecyclingSubmissions", new[] { "DropOffPointId" });
            DropIndex("dbo.RecyclingSubmissions", new[] { "MaterialTypeId" });
            DropIndex("dbo.RecyclingSubmissions", new[] { "ResidentId" });
            DropIndex("dbo.CollectionEventRegistrations", new[] { "ResidentId", "CollectionEventId" });
            DropIndex("dbo.UserBadges", new[] { "UserId", "BadgeId" });
            DropIndex("dbo.CollectionEvents", new[] { "DropOffPointId" });
            DropIndex("dbo.DropOffPoints", new[] { "AssignedOfficerId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropTable("dbo.SystemConfigs");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.PointTransactions");
            DropTable("dbo.Notifications");
            DropTable("dbo.RecyclingSubmissions");
            DropTable("dbo.CollectionEventRegistrations");
            DropTable("dbo.UserBadges");
            DropTable("dbo.CollectionEvents");
            DropTable("dbo.DropOffPoints");
            DropTable("dbo.MaterialTypes");
            DropTable("dbo.Badges");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.AspNetRoles");
        }
    }
}