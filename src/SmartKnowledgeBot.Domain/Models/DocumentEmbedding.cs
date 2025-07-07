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
    /// Document embeddings for AI vector search
    /// </summary>
    [Table("DocumentEmbeddings")]
    public class DocumentEmbedding
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid DocumentId { get; set; }

        [Required]
        public string TextChunk { get; set; } = string.Empty;

        public int ChunkIndex { get; set; }

        public int StartPosition { get; set; }

        public int EndPosition { get; set; }

        // Embedding vector (stored as JSON or binary)
        [Required]
        public string EmbeddingVector { get; set; } = string.Empty;

        [MaxLength(50)]
        public string EmbeddingModel { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(DocumentId))]
        public virtual Document Document { get; set; } = null!;
    }
}
