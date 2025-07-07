using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SmartKnowledgeBot.Business.Interfaces;
using SmartKnowledgeBot.Domain.DTOs;
using SmartKnowledgeBot.Domain.Models;
using SmartKnowledgeBot.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SmartKnowledgeBot.Business.Services
{
    /// <summary>
    /// Authentication service handling JWT tokens, user management, and security
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly SmartKnowledgeBotDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _tokenExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        public AuthService(
            SmartKnowledgeBotDbContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;

            // Load JWT configuration
            _jwtSecret = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            _jwtIssuer = _configuration["JwtSettings:Issuer"] ?? "SmartKnowledgeBot";
            _jwtAudience = _configuration["JwtSettings:Audience"] ?? "Enterprise";
            _tokenExpirationMinutes = int.Parse(_configuration["JwtSettings:TokenExpirationMinutes"] ?? "60");
            _refreshTokenExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "30");
        }

        /// <summary>
        /// Authenticate user with email and password, return JWT token and user info
        /// </summary>
        public async Task<AuthenticationResult?> AuthenticateAsync(string email, string password)
        {
            try
            {
                _logger.LogInformation("Authentication attempt for email: {Email}", email);

                // Find user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("Authentication failed: User not found for email {Email}", email);
                    return null;
                }

                // Verify password
                if (!VerifyPassword(password, user.PasswordHash))
                {
                    _logger.LogWarning("Authentication failed: Invalid password for user {UserId}", user.Id);
                    return null;
                }

                // Update last login time
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generate tokens
                var (token, tokenExpiration) = GenerateJwtToken(user);
                var refreshToken = await GenerateRefreshTokenAsync(user.Id);

                _logger.LogInformation("Authentication successful for user {UserId}", user.Id);

                return new AuthenticationResult
                {
                    Token = token,
                    TokenExpiration = tokenExpiration,
                    RefreshToken = refreshToken,
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication for email {Email}", email);
                return null;
            }
        }

        /// <summary>
        /// Refresh JWT token using refresh token
        /// </summary>
        public async Task<TokenRefreshResult?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Token refresh attempt");

                // Find and validate refresh token
                var tokenEntity = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (tokenEntity == null || !tokenEntity.IsActive)
                {
                    _logger.LogWarning("Token refresh failed: Invalid or expired refresh token");
                    return null;
                }

                // Revoke old refresh token
                tokenEntity.RevokedAt = DateTime.UtcNow;

                // Generate new tokens
                var (newToken, tokenExpiration) = GenerateJwtToken(tokenEntity.User);
                var newRefreshToken = await GenerateRefreshTokenAsync(tokenEntity.User.Id);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Token refresh successful for user {UserId}", tokenEntity.User.Id);

                return new TokenRefreshResult
                {
                    Token = newToken,
                    TokenExpiration = tokenExpiration,
                    RefreshToken = newRefreshToken
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return null;
            }
        }

        /// <summary>
        /// Logout user and revoke all refresh tokens
        /// </summary>
        public async Task LogoutAsync(string userId)
        {
            try
            {
                // Revoke all active refresh tokens for the user
                var activeTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                    .ToListAsync();

                foreach (var token in activeTokens)
                {
                    token.RevokedAt = DateTime.UtcNow;
                }

                if (activeTokens.Any())
                {
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("User {UserId} logged out, {TokenCount} refresh tokens revoked",
                    userId, activeTokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get user profile information
        /// </summary>
        public async Task<UserProfileDto?> GetUserProfileAsync(string userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return null;
                }

                // Get additional profile statistics
                var totalQueries = await _context.KnowledgeQueries
                    .CountAsync(q => q.UserId == userId);

                var lastQuery = await _context.KnowledgeQueries
                    .Where(q => q.UserId == userId)
                    .OrderByDescending(q => q.Timestamp)
                    .FirstOrDefaultAsync();

                return new UserProfileDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    Department = user.Department,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    TotalQueries = totalQueries,
                    LastQueryAt = lastQuery?.Timestamp
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile for {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Update user profile information
        /// </summary>
        public async Task<bool> UpdateUserProfileAsync(string userId, UserProfileUpdateDto updateRequest)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return false;
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateRequest.FirstName))
                {
                    user.FirstName = updateRequest.FirstName.Trim();
                }

                if (!string.IsNullOrEmpty(updateRequest.LastName))
                {
                    user.LastName = updateRequest.LastName.Trim();
                }

                if (!string.IsNullOrEmpty(updateRequest.Department))
                {
                    user.Department = updateRequest.Department.Trim();
                }

                if (updateRequest.ProfilePictureUrl != null)
                {
                    user.ProfilePictureUrl = updateRequest.ProfilePictureUrl.Trim();
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("User profile updated for {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return false;
                }

                // Verify current password
                if (!VerifyPassword(currentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Password change failed: Invalid current password for user {UserId}", userId);
                    return false;
                }

                // Validate new password strength
                if (!IsPasswordValid(newPassword))
                {
                    _logger.LogWarning("Password change failed: New password doesn't meet requirements for user {UserId}", userId);
                    return false;
                }

                // Hash and update password
                user.PasswordHash = HashPassword(newPassword);
                await _context.SaveChangesAsync();

                // Revoke all refresh tokens to force re-login on other devices
                await LogoutAsync(userId);

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return false;
            }
        }

        // === PRIVATE HELPER METHODS ===

        /// <summary>
        /// Generate JWT token for authenticated user
        /// </summary>
        private (string token, DateTime expiration) GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            var expiration = DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Role, user.Role),
                new("department", user.Department),
                new("userId", user.Id),
                new("jti", Guid.NewGuid().ToString()) // JWT ID for token tracking
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiration,
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return (tokenHandler.WriteToken(token), expiration);
        }

        /// <summary>
        /// Generate secure refresh token
        /// </summary>
        private async Task<string> GenerateRefreshTokenAsync(string userId)
        {
            // Generate cryptographically secure random token
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var refreshToken = Convert.ToBase64String(randomBytes);

            // Store refresh token in database
            var tokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(tokenEntity);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        /// <summary>
        /// Hash password using BCrypt
        /// </summary>
        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        /// <summary>
        /// Verify password against hash using BCrypt
        /// </summary>
        private static bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate password strength
        /// </summary>
        private static bool IsPasswordValid(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            // Check for at least one uppercase, lowercase, digit, and special character
            var hasUpper = password.Any(char.IsUpper);
            var hasLower = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        /// <summary>
        /// Map User entity to UserDto
        /// </summary>
        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                Department = user.Department,
                IsActive = user.IsActive
            };
        }
    }
}