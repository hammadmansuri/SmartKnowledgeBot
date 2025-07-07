using Microsoft.EntityFrameworkCore;
using SmartKnowledgeBot.Domain.Models;
using SmartKnowledgeBot.Domain.Enums;

namespace SmartKnowledgeBot.Infrastructure.Data
{
    /// <summary>
    /// Main database context for the Smart Knowledge Bot application
    /// </summary>
    public class SmartKnowledgeBotDbContext : DbContext
    {
        public SmartKnowledgeBotDbContext(DbContextOptions<SmartKnowledgeBotDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<KnowledgeQuery> KnowledgeQueries { get; set; }
        public DbSet<KnowledgeResponse> KnowledgeResponses { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentEmbedding> DocumentEmbeddings { get; set; }
        public DbSet<StructuredKnowledge> StructuredKnowledge { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<QueryFeedback> QueryFeedbacks { get; set; }
        public DbSet<QueryDocumentReference> QueryDocumentReferences { get; set; }
        public DbSet<ResponseDocumentReference> ResponseDocumentReferences { get; set; }
        public DbSet<QueryAnalytics> QueryAnalytics { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => new { e.Role, e.Department });
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            // Configure KnowledgeQuery entity
            modelBuilder.Entity<KnowledgeQuery>(entity =>
            {
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => new { e.UserId, e.Timestamp });
                entity.HasIndex(e => e.UserRole);
                entity.Property(e => e.Query).HasColumnType("nvarchar(2000)");

                // Full-text search index on Query (SQL Server specific)
                entity.HasIndex(e => e.Query).HasDatabaseName("IX_KnowledgeQueries_Query_FullText");
            });

            // Configure KnowledgeResponse entity
            modelBuilder.Entity<KnowledgeResponse>(entity =>
            {
                entity.HasIndex(e => e.QueryId).IsUnique();
                entity.HasIndex(e => e.GeneratedAt);
                entity.Property(e => e.Answer).HasColumnType("nvarchar(max)");
                entity.Property(e => e.ConfidenceScore).HasColumnType("decimal(5,4)");
            });

            static void ConfigureRelationships(ModelBuilder modelBuilder)
            {
                // User -> KnowledgeQueries (One-to-Many)
                modelBuilder.Entity<KnowledgeQuery>()
                    .HasOne(q => q.User)
                    .WithMany(u => u.Queries)
                    .HasForeignKey(q => q.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // KnowledgeQuery -> KnowledgeResponse (One-to-One)
                modelBuilder.Entity<KnowledgeResponse>()
                    .HasOne(r => r.Query)
                    .WithOne(q => q.Response)
                    .HasForeignKey<KnowledgeResponse>(r => r.QueryId)
                    .OnDelete(DeleteBehavior.Cascade);

                // User -> Documents (One-to-Many)
                modelBuilder.Entity<Document>()
                    .HasOne(d => d.UploadedByUser)
                    .WithMany(u => u.UploadedDocuments)
                    .HasForeignKey(d => d.UploadedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                // Document -> DocumentEmbeddings (One-to-Many)
                modelBuilder.Entity<DocumentEmbedding>()
                    .HasOne(e => e.Document)
                    .WithMany(d => d.Embeddings)
                    .HasForeignKey(e => e.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // User -> RefreshTokens (One-to-Many)
                modelBuilder.Entity<RefreshToken>()
                    .HasOne(rt => rt.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // User -> QueryFeedbacks (One-to-Many)
                modelBuilder.Entity<QueryFeedback>()
                    .HasOne(f => f.User)
                    .WithMany(u => u.Feedbacks)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // KnowledgeQuery -> QueryFeedbacks (One-to-Many)
                modelBuilder.Entity<QueryFeedback>()
                    .HasOne(f => f.Query)
                    .WithMany(q => q.Feedbacks)
                    .HasForeignKey(f => f.QueryId)
                    .OnDelete(DeleteBehavior.Cascade);

                // User -> StructuredKnowledge (One-to-Many)
                modelBuilder.Entity<StructuredKnowledge>()
                    .HasOne(sk => sk.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(sk => sk.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            }

            // Configure RefreshToken entity
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.ExpiresAt });
                entity.HasIndex(e => e.ExpiresAt);
            });

            // Configure QueryFeedback entity
            modelBuilder.Entity<QueryFeedback>(entity =>
            {
                entity.HasIndex(e => new { e.QueryId, e.UserId }).IsUnique();
                entity.HasIndex(e => e.FeedbackType);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Configure junction tables
            modelBuilder.Entity<QueryDocumentReference>(entity =>
            {
                entity.HasIndex(e => new { e.QueryId, e.DocumentId }).IsUnique();
                entity.Property(e => e.RelevanceScore).HasColumnType("decimal(5,4)");
            });

            modelBuilder.Entity<ResponseDocumentReference>(entity =>
            {
                entity.HasIndex(e => new { e.ResponseId, e.DocumentId }).IsUnique();
                entity.Property(e => e.RelevanceScore).HasColumnType("decimal(5,4)");
                entity.Property(e => e.CitedText).HasColumnType("nvarchar(max)");
            });

            // Configure QueryAnalytics entity
            modelBuilder.Entity<QueryAnalytics>(entity =>
            {
                entity.HasIndex(e => e.Date);
                entity.HasIndex(e => new { e.Date, e.Category });
                entity.HasIndex(e => new { e.Date, e.Department });
                entity.Property(e => e.AverageResponseTime).HasColumnType("decimal(10,2)");
                entity.Property(e => e.AverageConfidenceScore).HasColumnType("decimal(5,4)");
                entity.Property(e => e.TopQueries).HasColumnType("nvarchar(max)");
            });

            // Configure SystemSetting entity
            modelBuilder.Entity<SystemSetting>(entity =>
            {
                entity.HasIndex(e => e.Category);
                entity.Property(e => e.Value).HasColumnType("nvarchar(max)");
            });

            // Configure relationships
            ConfigureRelationships(modelBuilder);

            // Seed initial data
            SeedInitialData(modelBuilder);
        }

        private static void ConfigureRelationships(ModelBuilder modelBuilder)
        {
            // User -> KnowledgeQueries (One-to-Many)
            modelBuilder.Entity<KnowledgeQuery>()
                .HasOne(q => q.User)
                .WithMany(u => u.Queries)
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // KnowledgeQuery -> KnowledgeResponse (One-to-One)
            modelBuilder.Entity<KnowledgeResponse>()
                .HasOne(r => r.Query)
                .WithOne(q => q.Response)
                .HasForeignKey<KnowledgeResponse>(r => r.QueryId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Documents (One-to-Many)
            modelBuilder.Entity<Document>()
                .HasOne(d => d.UploadedByUser)
                .WithMany(u => u.UploadedDocuments)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Document -> DocumentEmbeddings (One-to-Many)
            modelBuilder.Entity<DocumentEmbedding>()
                .HasOne(e => e.Document)
                .WithMany(d => d.Embeddings)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> RefreshTokens (One-to-Many)
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> QueryFeedbacks (One-to-Many)
            modelBuilder.Entity<QueryFeedback>()
                .HasOne(f => f.User)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // KnowledgeQuery -> QueryFeedbacks (One-to-Many)
            modelBuilder.Entity<QueryFeedback>()
                .HasOne(f => f.Query)
                .WithMany(q => q.Feedbacks)
                .HasForeignKey(f => f.QueryId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> StructuredKnowledge (One-to-Many)
            modelBuilder.Entity<StructuredKnowledge>()
                .HasOne(sk => sk.CreatedByUser)
                .WithMany()
                .HasForeignKey(sk => sk.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Seed default admin user
            var adminUserId = Guid.NewGuid().ToString();
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = adminUserId,
                    Email = "admin@company.com",
                    FirstName = "System",
                    LastName = "Administrator",
                    PasswordHash = "$2a$11$dummy.hash.for.initial.setup", // Should be properly hashed
                    Role = "Admin",
                    Department = "IT",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Seed system settings
            modelBuilder.Entity<SystemSetting>().HasData(
                new SystemSetting
                {
                    Key = "MaxDocumentSizeMB",
                    Value = "50",
                    Description = "Maximum document upload size in MB",
                    Category = "Document",
                    ModifiedBy = adminUserId
                },
                new SystemSetting
                {
                    Key = "DefaultTokenExpirationMinutes",
                    Value = "60",
                    Description = "Default JWT token expiration time in minutes",
                    Category = "Authentication",
                    ModifiedBy = adminUserId
                },
                new SystemSetting
                {
                    Key = "RefreshTokenExpirationDays",
                    Value = "30",
                    Description = "Refresh token expiration time in days",
                    Category = "Authentication",
                    ModifiedBy = adminUserId
                },
                new SystemSetting
                {
                    Key = "MaxQueryLength",
                    Value = "2000",
                    Description = "Maximum length for knowledge queries",
                    Category = "Query",
                    ModifiedBy = adminUserId
                },
                new SystemSetting
                {
                    Key = "EnableAnalytics",
                    Value = "true",
                    Description = "Enable query analytics collection",
                    Category = "Analytics",
                    ModifiedBy = adminUserId
                }
            );

            // Seed sample structured knowledge
            modelBuilder.Entity<StructuredKnowledge>().HasData(
                new StructuredKnowledge
                {
                    Id = Guid.NewGuid(),
                    Question = "How do I reset my password?",
                    Answer = "To reset your password, go to the login page and click 'Forgot Password'. Enter your email address and follow the instructions sent to your email.",
                    Category = "IT Support",
                    AccessLevel = AccessLevel.General,
                    Keywords = "password, reset, forgot, login, email",
                    Priority = 10,
                    CreatedBy = adminUserId,
                    Source = "IT Policy Manual v2.1"
                },
                new StructuredKnowledge
                {
                    Id = Guid.NewGuid(),
                    Question = "What are the company's vacation policies?",
                    Answer = "Employees accrue vacation time based on length of service. New employees start with 2 weeks annually, increasing to 3 weeks after 3 years and 4 weeks after 7 years.",
                    Category = "HR",
                    AccessLevel = AccessLevel.General,
                    Keywords = "vacation, PTO, time off, leave, policy",
                    Priority = 8,
                    CreatedBy = adminUserId,
                    Source = "Employee Handbook 2024"
                },
                new StructuredKnowledge
                {
                    Id = Guid.NewGuid(),
                    Question = "How do I submit an expense report?",
                    Answer = "Expense reports should be submitted through the company portal within 30 days of the expense. Include all receipts and detailed descriptions of business purposes.",
                    Category = "Finance",
                    AccessLevel = AccessLevel.General,
                    Keywords = "expense, report, receipt, reimbursement, finance",
                    Priority = 7,
                    CreatedBy = adminUserId,
                    Source = "Finance Procedures Guide"
                }
            );
        }

        /// <summary>
        /// Override SaveChanges to automatically set timestamps
        /// </summary>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync to automatically set timestamps
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is Document document && entry.State == EntityState.Modified)
                {
                    document.LastModified = DateTime.UtcNow;
                }
                else if (entry.Entity is StructuredKnowledge knowledge && entry.State == EntityState.Modified)
                {
                    knowledge.LastModified = DateTime.UtcNow;
                }
                else if (entry.Entity is SystemSetting setting && entry.State == EntityState.Modified)
                {
                    setting.LastModified = DateTime.UtcNow;
                }
            }
        }
    }
}

// ===== KEY BUSINESS INTERFACES =====

namespace SmartKnowledgeBot.Business.Interfaces
{
    using SmartKnowledgeBot.Domain.Models;
    using SmartKnowledgeBot.Domain.DTOs;

    /// <summary>
    /// Knowledge service interface for query processing
    /// </summary>
    public interface IKnowledgeService
    {
        Task<KnowledgeResponse> ProcessQueryAsync(KnowledgeQuery query);
        Task<PaginatedResponseDto<QueryHistoryDto>> GetUserQueryHistoryAsync(string userId, int page, int pageSize);
        Task RecordFeedbackAsync(Guid queryId, string userId, QueryFeedbackDto feedback);
        Task<List<QuerySuggestionDto>> GetQuerySuggestionsAsync(string role, string department);
    }

    /// <summary>
    /// Document service interface for document management
    /// </summary>
    public interface IDocumentService
    {
        Task<DocumentUploadResponseDto> UploadDocumentAsync(DocumentUploadRequest request);
        Task<PaginatedResponseDto<DocumentSummaryDto>> GetUserAccessibleDocumentsAsync(string userId, string role, string? category, int page, int pageSize);
        Task<DocumentProcessingStatusDto?> GetDocumentProcessingStatusAsync(Guid documentId, string userId);
        Task<bool> DeleteDocumentAsync(Guid documentId, string userId);
        Task<List<DocumentCategoryDto>> GetAvailableCategoriesAsync(string role);
        Task<bool> UpdateDocumentMetadataAsync(Guid documentId, DocumentUpdateRequestDto updateRequest, string userId);
    }

    /// <summary>
    /// Authentication service interface
    /// </summary>
    public interface IAuthService
    {
        Task<AuthenticationResult?> AuthenticateAsync(string email, string password);
        Task<TokenRefreshResult?> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string userId);
        Task<UserProfileDto?> GetUserProfileAsync(string userId);
        Task<bool> UpdateUserProfileAsync(string userId, UserProfileUpdateDto updateRequest);
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    }

    /// <summary>
    /// Azure OpenAI service interface for AI operations
    /// </summary>
    public interface IAzureOpenAIService
    {
        Task<string> GenerateResponseAsync(string query, List<string> relevantDocuments, string userContext);
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<List<string>> ExtractKeywordsAsync(string text);
        Task<string> SummarizeDocumentAsync(string documentText);
    }

    /// <summary>
    /// Blob storage service interface for document storage
    /// </summary>
    public interface IBlobStorageService
    {
        Task<string> UploadDocumentAsync(Stream documentStream, string fileName, string containerName);
        Task<Stream> DownloadDocumentAsync(string blobUrl);
        Task<bool> DeleteDocumentAsync(string blobUrl);
        Task<string> GetDocumentUrlAsync(string containerName, string fileName);
    }
}