using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SmartKnowledgeBot.Domain.Models
{
    /// <summary>
    /// Knowledge query entity for tracking user questions
    /// </summary>
    [Table("KnowledgeQueries")]
    public class KnowledgeQuery
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(2000)]
        public string Query { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string UserRole { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? SessionId { get; set; }

        public bool IsAnswered { get; set; } = false;

        public double? ConfidenceScore { get; set; }

        [MaxLength(50)]
        public string? AnswerSource { get; set; } // "SQL", "AI", "Hybrid"

        public int ResponseTimeMs { get; set; }

        public bool IsFromStructuredData { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        public virtual KnowledgeResponse? Response { get; set; }
        public virtual ICollection<QueryFeedback> Feedbacks { get; set; } = new List<QueryFeedback>();
        public virtual ICollection<QueryDocumentReference> DocumentReferences { get; set; } = new List<QueryDocumentReference>();
    }
}
