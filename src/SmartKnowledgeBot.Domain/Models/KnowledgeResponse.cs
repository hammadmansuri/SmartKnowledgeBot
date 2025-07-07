using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SmartKnowledgeBot.Domain.Models
{
    /// <summary>
    /// Knowledge response entity storing AI-generated answers
    /// </summary>
    [Table("KnowledgeResponses")]
    public class KnowledgeResponse
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid QueryId { get; set; }

        [Required]
        public string Answer { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Source { get; set; } = string.Empty;

        public double ConfidenceScore { get; set; }

        public bool IsFromStructuredData { get; set; }

        public int ResponseTime { get; set; } // in milliseconds

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // AI-specific fields
        [MaxLength(100)]
        public string? ModelUsed { get; set; }

        public int? TokensUsed { get; set; }

        // Navigation properties
        [ForeignKey(nameof(QueryId))]
        public virtual KnowledgeQuery Query { get; set; } = null!;

        public virtual ICollection<ResponseDocumentReference> RelevantDocuments { get; set; } = new List<ResponseDocumentReference>();
    }
}
