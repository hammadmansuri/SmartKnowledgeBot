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
    /// Structured knowledge base for predefined Q&A
    /// </summary>
    [Table("StructuredKnowledge")]
    public class StructuredKnowledge
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(500)]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        public AccessLevel AccessLevel { get; set; } = AccessLevel.General;

        [MaxLength(500)]
        public string Keywords { get; set; } = string.Empty; // For search optimization

        public int Priority { get; set; } = 0; // Higher priority answers shown first

        public bool IsActive { get; set; } = true;

        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastModified { get; set; }

        [MaxLength(500)]
        public string? Source { get; set; } // Reference to policy document, etc.

        // Navigation properties
        [ForeignKey(nameof(CreatedBy))]
        public virtual User CreatedByUser { get; set; } = null!;
    }
}
