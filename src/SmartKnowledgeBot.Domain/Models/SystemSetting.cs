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
    /// System configuration settings
    /// </summary>
    [Table("SystemSettings")]
    public class SystemSetting
    {
        [Key]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string Category { get; set; } = "General";

        public bool IsEncrypted { get; set; } = false;

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string ModifiedBy { get; set; } = "System";
    }
}
