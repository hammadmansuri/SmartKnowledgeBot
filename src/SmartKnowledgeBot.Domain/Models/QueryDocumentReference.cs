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
    /// Junction table for query-document relationships
    /// </summary>
    [Table("QueryDocumentReferences")]
    public class QueryDocumentReference
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid QueryId { get; set; }

        [Required]
        public Guid DocumentId { get; set; }

        public double RelevanceScore { get; set; }

        public bool WasUsedInResponse { get; set; }

        // Navigation properties
        [ForeignKey(nameof(QueryId))]
        public virtual KnowledgeQuery Query { get; set; } = null!;

        [ForeignKey(nameof(DocumentId))]
        public virtual Document Document { get; set; } = null!;
    }
}
