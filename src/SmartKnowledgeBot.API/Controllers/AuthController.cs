using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartKnowledgeBot.Business.Interfaces;
using SmartKnowledgeBot.Domain.DTOs;
using System.Security.Claims;

namespace SmartKnowledgeBot.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and return JWT token
        /// </summary>
        /// <param name="loginRequest">User login credentials</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto loginRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Invalid login data",
                        Details = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var result = await _authService.AuthenticateAsync(loginRequest.Email, loginRequest.Password);

                if (result == null)
                {
                    _logger.LogWarning("Failed login attempt for email: {Email}", loginRequest.Email);
                    return Unauthorized(new ErrorResponseDto
                    {
                        Message = "Invalid email or password"
                    });
                }

                _logger.LogInformation("Successful login for user: {UserId}", result.User.Id);

                return Ok(new LoginResponseDto
                {
                    Token = result.Token,
                    TokenExpiration = result.TokenExpiration,
                    RefreshToken = result.RefreshToken,
                    User = new UserDto
                    {
                        Id = result.User.Id,
                        Email = result.User.Email,
                        FirstName = result.User.FirstName,
                        LastName = result.User.LastName,
                        Role = result.User.Role,
                        Department = result.User.Department,
                        IsActive = result.User.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt");
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "An error occurred during login"
                });
            }
        }

        /// <summary>
        /// Refresh JWT token using refresh token
        /// </summary>
        /// <param name="refreshRequest">Refresh token request</param>
        /// <returns>New JWT token</returns>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(TokenRefreshResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokenRefreshResponseDto>> RefreshToken([FromBody] TokenRefreshRequestDto refreshRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Invalid refresh token data"
                    });
                }

                var result = await _authService.RefreshTokenAsync(refreshRequest.RefreshToken);

                if (result == null)
                {
                    _logger.LogWarning("Invalid refresh token used");
                    return Unauthorized(new ErrorResponseDto
                    {
                        Message = "Invalid refresh token"
                    });
                }

                return Ok(new TokenRefreshResponseDto
                {
                    Token = result.Token,
                    TokenExpiration = result.TokenExpiration,
                    RefreshToken = result.RefreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "An error occurred during token refresh"
                });
            }
        }

        /// <summary>
        /// Logout user and invalidate refresh token
        /// </summary>
        /// <returns>Success response</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<SuccessResponseDto>> Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await _authService.LogoutAsync(userId);
                    _logger.LogInformation("User {UserId} logged out", userId);
                }

                return Ok(new SuccessResponseDto
                {
                    Message = "Logged out successfully",
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "An error occurred during logout"
                });
            }
        }

        /// <summary>
        /// Get current user profile information
        /// </summary>
        /// <returns>User profile data</returns>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponseDto
                    {
                        Message = "Invalid user context"
                    });
                }

                var userProfile = await _authService.GetUserProfileAsync(userId);
                if (userProfile == null)
                {
                    return NotFound(new ErrorResponseDto
                    {
                        Message = "User profile not found"
                    });
                }

                return Ok(userProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "Unable to retrieve user profile"
                });
            }
        }

        /// <summary>
        /// Update user profile information
        /// </summary>
        /// <param name="updateRequest">Profile update data</param>
        /// <returns>Success response</returns>
        [HttpPatch("profile")]
        [Authorize]
        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SuccessResponseDto>> UpdateProfile([FromBody] UserProfileUpdateDto updateRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Invalid profile data",
                        Details = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponseDto
                    {
                        Message = "Invalid user context"
                    });
                }

                var updated = await _authService.UpdateUserProfileAsync(userId, updateRequest);
                if (!updated)
                {
                    return NotFound(new ErrorResponseDto
                    {
                        Message = "User profile not found"
                    });
                }

                return Ok(new SuccessResponseDto
                {
                    Message = "Profile updated successfully",
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "Unable to update profile"
                });
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="changePasswordRequest">Password change data</param>
        /// <returns>Success response</returns>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SuccessResponseDto>> ChangePassword([FromBody] ChangePasswordRequestDto changePasswordRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Invalid password data",
                        Details = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponseDto
                    {
                        Message = "Invalid user context"
                    });
                }

                var result = await _authService.ChangePasswordAsync(userId, changePasswordRequest.CurrentPassword, changePasswordRequest.NewPassword);
                if (!result)
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Current password is incorrect"
                    });
                }

                _logger.LogInformation("Password changed for user {UserId}", userId);

                return Ok(new SuccessResponseDto
                {
                    Message = "Password changed successfully",
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "Unable to change password"
                });
            }
        }
    }
}