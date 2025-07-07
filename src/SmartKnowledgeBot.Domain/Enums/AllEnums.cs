using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SmartKnowledgeBot.Domain.Enums;

namespace SmartKnowledgeBot.Domain.Enums
{
    /// <summary>
    /// Document access levels for role-based security
    /// </summary>
    public enum AccessLevel
    {
        General = 0,        // All employees
        Departmental = 1,   // Specific department only
        Management = 2,     // Management level and above
        Executive = 3,      // Executive level only
        HR = 4,            // HR department only
        IT = 5,            // IT department only
        Finance = 6,       // Finance department only
        Admin = 7          // Admin users only
    }

    /// <summary>
    /// Document processing status
    /// </summary>
    public enum DocumentStatus
    {
        Uploaded = 0,
        Processing = 1,
        Processed = 2,
        Failed = 3,
        Indexed = 4,
        Archived = 5
    }

    /// <summary>
    /// User feedback types
    /// </summary>
    public enum FeedbackType
    {
        Helpful = 0,
        NotHelpful = 1,
        Inaccurate = 2,
        Incomplete = 3,
        Irrelevant = 4
    }
}

namespace SmartKnowledgeBot.Domain.DTOs
{
    // ===== REQUEST DTOs =====

    /// <summary>
    /// Knowledge query request DTO
    /// </summary>
    public class KnowledgeQueryDto
    {
        [Required]
        [MinLength(3)]
        [MaxLength(2000)]
        public string Query { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? SessionId { get; set; }

        [MaxLength(100)]
        public string? Context { get; set; } // Additional context for the query
    }

    /// <summary>
    /// Login request DTO
    /// </summary>
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }

    /// <summary>
    /// Token refresh request DTO
    /// </summary>
    public class TokenRefreshRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Change password request DTO
    /// </summary>
    public class ChangePasswordRequestDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword))]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// User profile update DTO
    /// </summary>
    public class UserProfileUpdateDto
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }
    }

    /// <summary>
    /// Document update request DTO
    /// </summary>
    public class DocumentUpdateRequestDto
    {
        [MaxLength(100)]
        public string? Category { get; set; }

        public AccessLevel? AccessLevel { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Query feedback DTO
    /// </summary>
    public class QueryFeedbackDto
    {
        [Required]
        public FeedbackType FeedbackType { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } = 3;

        [MaxLength(1000)]
        public string? Comments { get; set; }
    }

    // ===== RESPONSE DTOs =====

    /// <summary>
    /// Knowledge response DTO
    /// </summary>
    public class KnowledgeResponseDto
    {
        public string Answer { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public bool IsFromStructuredData { get; set; }
        public List<DocumentReferenceDto>? RelevantDocuments { get; set; }
        public int ResponseTime { get; set; }
        public Guid QueryId { get; set; }
    }

    /// <summary>
    /// Login response DTO
    /// </summary>
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime TokenExpiration { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

    /// <summary>
    /// Token refresh response DTO
    /// </summary>
    public class TokenRefreshResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime TokenExpiration { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// User DTO
    /// </summary>
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }

    /// <summary>
    /// User profile DTO with additional details
    /// </summary>
    public class UserProfileDto : UserDto
    {
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public int TotalQueries { get; set; }
        public DateTime? LastQueryAt { get; set; }
    }

    /// <summary>
    /// Document upload response DTO
    /// </summary>
    public class DocumentUploadResponseDto
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DocumentStatus Status { get; set; }
        public DateTime ProcessingStarted { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
        public string BlobUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Document summary DTO for listing
    /// </summary>
    public class DocumentSummaryDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public AccessLevel AccessLevel { get; set; }
        public string? Description { get; set; }
        public long FileSizeBytes { get; set; }
        public string FileType { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public DateTime? LastModified { get; set; }
        public DocumentStatus Status { get; set; }
        public string FileSizeFormatted => FormatFileSize(FileSizeBytes);

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Document processing status DTO
    /// </summary>
    public class DocumentProcessingStatusDto
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DocumentStatus Status { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int ProgressPercentage { get; set; }
        public string? StatusMessage { get; set; }
        public bool HasErrors { get; set; }
        public List<string>? ErrorMessages { get; set; }
    }

    /// <summary>
    /// Document reference DTO
    /// </summary>
    public class DocumentReferenceDto
    {
        public Guid DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public double RelevanceScore { get; set; }
        public string? CitedText { get; set; }
    }

    /// <summary>
    /// Document category DTO
    /// </summary>
    public class DocumentCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DocumentCount { get; set; }
        public AccessLevel RequiredAccessLevel { get; set; }
    }

    /// <summary>
    /// Query history DTO
    /// </summary>
    public class QueryHistoryDto
    {
        public Guid Id { get; set; }
        public string Query { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsAnswered { get; set; }
        public double? ConfidenceScore { get; set; }
        public string? AnswerSource { get; set; }
        public int ResponseTimeMs { get; set; }
        public bool HasFeedback { get; set; }
    }

    /// <summary>
    /// Query suggestion DTO
    /// </summary>
    public class QuerySuggestionDto
    {
        public string Suggestion { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int PopularityScore { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Paginated response DTO
    /// </summary>
    public class PaginatedResponseDto<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }

    /// <summary>
    /// Generic success response DTO
    /// </summary>
    public class SuccessResponseDto
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    /// <summary>
    /// Generic error response DTO
    /// </summary>
    public class ErrorResponseDto
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public List<string>? Details { get; set; }
        public string? ErrorCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // ===== BUSINESS DTOs =====

    /// <summary>
    /// User context for business operations
    /// </summary>
    public class UserContext
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    /// <summary>
    /// Document upload request for business layer
    /// </summary>
    public class DocumentUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public string Category { get; set; } = string.Empty;
        public AccessLevel AccessLevel { get; set; }
        public string? Description { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>
    /// Authentication result DTO
    /// </summary>
    public class AuthenticationResult
    {
        public string Token { get; set; } = string.Empty;
        public DateTime TokenExpiration { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

    /// <summary>
    /// Token refresh result DTO
    /// </summary>
    public class TokenRefreshResult
    {
        public string Token { get; set; } = string.Empty;
        public DateTime TokenExpiration { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
    }
}