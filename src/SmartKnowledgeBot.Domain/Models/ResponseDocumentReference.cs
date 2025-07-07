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
    /// Junction table for response-document relationships
    /// </summary>
    [Table("ResponseDocumentReferences")]
    public class ResponseDocumentReference
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ResponseId { get; set; }

        [Required]
        public Guid DocumentId { get; set; }

        public double RelevanceScore { get; set; }

        public string? CitedText { get; set; } // Specific text that was referenced

        // Navigation properties
        [ForeignKey(nameof(ResponseId))]
        public virtual KnowledgeResponse Response { get; set; } = null!;

        [ForeignKey(nameof(DocumentId))]
        public virtual Document Document { get; set; } = null!;
    }
}
