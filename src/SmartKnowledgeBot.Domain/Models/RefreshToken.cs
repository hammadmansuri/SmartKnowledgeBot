using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartKnowledgeBot.Domain.Models
{
    /// <summary>
    /// Refresh tokens for JWT authentication
    /// </summary>
    [Table("RefreshTokens")]
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedAt { get; set; }

        public bool IsRevoked => RevokedAt.HasValue;

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        public bool IsActive => !IsRevoked && !IsExpired;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}
