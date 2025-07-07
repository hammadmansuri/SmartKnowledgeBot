using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartKnowledgeBot.Domain.Enums;

namespace SmartKnowledgeBot.Domain.Models
{
    /// <summary>
    /// Query feedback for continuous improvement
    /// </summary>
    [Table("QueryFeedbacks")]
    public class QueryFeedback
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid QueryId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public FeedbackType FeedbackType { get; set; } = FeedbackType.Helpful;

        public int Rating { get; set; } // 1-5 scale

        [MaxLength(1000)]
        public string? Comments { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(QueryId))]
        public virtual KnowledgeQuery Query { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}
