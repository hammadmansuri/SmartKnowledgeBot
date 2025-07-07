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
    /// Document entity for enterprise documents stored in blob storage
    /// </summary>
    [Table("Documents")]
    public class Document
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string BlobUrl { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        public AccessLevel AccessLevel { get; set; } = AccessLevel.General;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public long FileSizeBytes { get; set; }

        [MaxLength(10)]
        public string FileType { get; set; } = string.Empty; // .pdf, .docx, etc.

        [Required]
        public string UploadedBy { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastModified { get; set; }

        public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

        public DateTime? ProcessedAt { get; set; }

        // Content extraction fields
        public string? ExtractedText { get; set; }

        public string? ContentSummary { get; set; }

        public string? Keywords { get; set; } // JSON array of extracted keywords

        // Search optimization
        public string? SearchVector { get; set; } // For full-text search

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey(nameof(UploadedBy))]
        public virtual User UploadedByUser { get; set; } = null!;

        public virtual ICollection<DocumentEmbedding> Embeddings { get; set; } = new List<DocumentEmbedding>();
        public virtual ICollection<QueryDocumentReference> QueryReferences { get; set; } = new List<QueryDocumentReference>();
        public virtual ICollection<ResponseDocumentReference> ResponseReferences { get; set; } = new List<ResponseDocumentReference>();
    }
}
