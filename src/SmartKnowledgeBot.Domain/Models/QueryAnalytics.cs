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
    /// Analytics for query patterns and system usage
    /// </summary>
    [Table("QueryAnalytics")]
    public class QueryAnalytics
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public DateTime Date { get; set; }

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Department { get; set; } = string.Empty;

        public int TotalQueries { get; set; }

        public int SuccessfulQueries { get; set; }

        public double AverageResponseTime { get; set; }

        public double AverageConfidenceScore { get; set; }

        public int UniqueUsers { get; set; }

        [MaxLength(1000)]
        public string? TopQueries { get; set; } // JSON array of most common queries

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
