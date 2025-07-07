using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace SmartKnowledgeBot.Domain.Models
{
    /// <summary>
    /// User entity representing enterprise employees
    /// </summary>
    [Table("Users")]
    public class User
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(256)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = string.Empty; // HR, IT, Finance, Admin, etc.

        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }

        // Navigation properties
        public virtual ICollection<KnowledgeQuery> Queries { get; set; } = new List<KnowledgeQuery>();
        public virtual ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual ICollection<QueryFeedback> Feedbacks { get; set; } = new List<QueryFeedback>();

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}
