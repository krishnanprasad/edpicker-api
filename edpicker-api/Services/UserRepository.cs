using edpicker_api.Models;
using edpicker_api.Models.Dto;
using edpicker_api.Models.Results;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace edpicker_api.Services
{
    public class UserRepository : IUserRepository
    {
        private readonly EdPickerDbContext _context;

        public UserRepository(EdPickerDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByGoogleIdAsync(string googleId)
        {
            return await _context.User.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.User.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> CreateOrUpdateUserAsync(User user)
        {
            try { 
            var existingUser = await _context.User.FirstOrDefaultAsync(u => u.GoogleId == user.GoogleId);

            if (existingUser == null)
            {
                user.CreatedDate = DateTime.UtcNow;
                _context.User.Add(user);
                    await _context.SaveChangesAsync();
                }
            else
            {
                existingUser.FullName = user.FullName;
                existingUser.Email = user.Email;
                existingUser.LastLoginDate = DateTime.UtcNow;
                existingUser.IsActive = true;
                    
                // Update additional fields as needed
            }

            
            return existingUser ?? user;
            }
            catch   (Exception E)
            {
                throw E;
            }
        }
        public async Task<IEnumerable<UserJobApplicationDto>> GetUserJobApplicationsAsync(int userId)
        {

            try
            {
                var result = await _context.Set<UserJobApplicationDto>()
                .FromSqlRaw("EXEC dbo.GetUserJobApplications @UserId = {0}", userId)
                .AsNoTracking()
                .ToListAsync();

            return result;
        }
            catch   (Exception E)
            {
                throw E;
            }
}

        public async Task<ApplyForJobResult?> WithdrawlOfJobAsync(int userId, int jobId, string notes, string createdBy)
        {
            try
            {
                // Build SQL and parameters
                var sql = "EXEC dbo.WithdawnJobApplicationByUser @JobId = {0}, @UserId = {1}, @Notes = {2}, @CreatedBy = {3}";
                var results = await _context.Set<ApplyForJobResult>()
                    .FromSqlRaw(sql, jobId, userId, notes ?? (object)DBNull.Value, createdBy)
                    .ToListAsync();

                return results.FirstOrDefault() ?? new ApplyForJobResult { ResultCode = 0, Message = "Unknown error." };
            }
            catch (Exception E)
            {
                throw E;
            }
        }
    }
}
