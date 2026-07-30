using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using SmartRecyclingRewardsSystem.Models;

namespace SmartRecyclingRewardsSystem.Services
{
    public class RewardService
    {
        private readonly ApplicationDbContext _db;
        private readonly Random _random = new Random();

        public RewardService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Get all active rewards
        public async Task<List<Reward>> GetActiveRewardsAsync()
        {
            return await _db.Rewards
                .Where(r => r.IsActive)
                .OrderBy(r => r.PointsCost)
                .ToListAsync();
        }

        // Get user's redemption history
        public async Task<List<RewardRedemption>> GetUserRedemptionsAsync(string userId)
        {
            return await _db.RewardRedemptions
                .Include(r => r.Reward)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RedemptionDate)
                .ToListAsync();
        }

        // Generate a unique coupon code
        public string GenerateCouponCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            var code = new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());

            return $"ECO-{code.Substring(0, 4)}-{code.Substring(4, 4)}-{code.Substring(8, 4)}";
        }

        // ============================================================
        // Process redemption - NO VIRTUAL CARD REQUIRED
        // ============================================================
        public async Task<RedemptionResult> ProcessRedemptionAsync(string userId, int rewardId)
        {
            var result = new RedemptionResult();

            // 1. Validate user and reward
            var user = await _db.Set<ApplicationUser>().FindAsync(userId);
            var reward = await _db.Rewards.FindAsync(rewardId);

            if (user == null || reward == null)
            {
                result.Success = false;
                result.Message = "Invalid user or reward.";
                return result;
            }

            // 2. Get current balance
            int currentBalance = GetCurrentBalance(userId);

            // 3. Check if user has enough points
            if (currentBalance < reward.PointsCost)
            {
                result.Success = false;
                result.Message = $"Insufficient points. You need {reward.PointsCost} points. You have {currentBalance} points.";
                return result;
            }

            // 4. Calculate new balance
            int newBalance = currentBalance - reward.PointsCost;

            // 5. Generate Transaction ID
            result.TransactionId = $"TXN-{DateTime.Now:yyyyMMdd}-{_random.Next(100000, 999999)}";

            // 6. Generate coupon code
            var couponCode = GenerateCouponCode();

            // 7. Create redemption record
            var redemption = new RewardRedemption
            {
                UserId = userId,
                RewardId = rewardId,
                PointsSpent = reward.PointsCost,
                RedemptionDate = DateTime.Now,
                CouponCode = couponCode,
                CouponDetails = reward.Name,
                CouponExpiryDate = DateTime.Now.AddMonths(3),
                Status = "Completed",
                TransactionId = result.TransactionId,
                PaymentMethod = "Points",
                CompletionDate = DateTime.Now
            };

            _db.RewardRedemptions.Add(redemption);

            // 8. Create point transaction with BalanceAfter
            var transaction = new PointTransaction
            {
                UserId = userId,
                Points = -reward.PointsCost,
                BalanceAfter = newBalance,
                Description = $"Redeemed: {reward.Name}",
                TransactionDate = DateTime.Now,
                TransactionType = TransactionType.Redemption
            };
            _db.PointTransactions.Add(transaction);

            await _db.SaveChangesAsync();

            // 9. Set result properties
            result.Success = true;
            result.Message = $"Successfully redeemed {reward.Name}!";
            result.CouponCode = couponCode;
            result.RedemptionId = redemption.RewardRedemptionId;
            result.NewBalance = newBalance;  // ✅ SET THE NEW BALANCE

            return result;
        }

        // ============================================================
        // Helper: Get current balance from last transaction
        // ============================================================
        private int GetCurrentBalance(string userId)
        {
            var last = _db.PointTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.PointTransactionId)
                .FirstOrDefault();

            return last?.BalanceAfter ?? 0;
        }

        // ============================================================
        // Helper Classes
        // ============================================================
        public class RedemptionResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public string CouponCode { get; set; }
            public int RedemptionId { get; set; }
            public string TransactionId { get; set; }
            public int NewBalance { get; set; }  // ✅ ADDED THIS
        }
    }
}