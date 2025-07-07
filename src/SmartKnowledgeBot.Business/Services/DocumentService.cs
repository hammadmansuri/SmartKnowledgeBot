using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartKnowledgeBot.Business.Interfaces;
using SmartKnowledgeBot.Domain.DTOs;
using SmartKnowledgeBot.Domain.Models;
using SmartKnowledgeBot.Domain.Enums;
using SmartKnowledgeBot.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace SmartKnowledgeBot.Business.Services
{
    /// <summary>
    /// Document service for managing enterprise document uploads, processing, and retrieval
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly SmartKnowledgeBotDbContext _context;
        private readonly IBlobStorageService _blobService;
        private readonly IAzureOpenAIService _aiService;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
            SmartKnowledgeBotDbContext context,
            IBlobStorageService blobService,
            IAzureOpenAIService aiService,
            ILogger<DocumentService> logger)
        {
            _context = context;
            _blobService = blobService;
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Upload document and initiate background processing for indexing
        /// </summary>
        public async Task<DocumentUploadResponseDto> UploadDocumentAsync(DocumentUploadRequest request)
        {
            try
            {
                _logger.LogInformation("Starting document upload for file {FileName} by user {UserId}",
                    request.File.FileName, request.UploadedBy);

                // Validate file
                ValidateFile(request.File);

                // Generate unique file name to avoid conflicts
                var fileExtension = Path.GetExtension(request.File.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var containerName = GetContainerName(request.Category);

                // Upload to blob storage
                string blobUrl;
                using (var stream = request.File.OpenReadStream())
                {
                    blobUrl = await _blobService.UploadDocumentAsync(stream, uniqueFileName, containerName);
                }

                // Create document entity
                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    FileName = request.File.FileName,
                    BlobUrl = blobUrl,
                    Category = request.Category,
                    AccessLevel = request.AccessLevel,
                    Description = request.Description,
                    FileSizeBytes = request.File.Length,
                    FileType = fileExtension,
                    UploadedBy = request.UploadedBy,
                    UploadedAt = DateTime.UtcNow,
                    Status = DocumentStatus.Uploaded,
                    IsActive = true
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                // Initiate background processing (in a real implementation, this would be a background job)
                _ = Task.Run(async () => await ProcessDocumentAsync(document.Id));

                _logger.LogInformation("Document {DocumentId} uploaded successfully", document.Id);

                return new DocumentUploadResponseDto
                {
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    Category = document.Category,
                    Status = document.Status,
                    ProcessingStarted = DateTime.UtcNow,
                    EstimatedCompletionTime = DateTime.UtcNow.AddMinutes(EstimateProcessingTime(document.FileSizeBytes)),
                    BlobUrl = blobUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document {FileName}", request.File.FileName);
                throw;
            }
        }

        /// <summary>
        /// Get documents accessible to the user based on role and permissions
        /// </summary>
        public async Task<PaginatedResponseDto<DocumentSummaryDto>> GetUserAccessibleDocumentsAsync(
            string userId, string role, string? category, int page, int pageSize)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new ArgumentException("User not found");
                }

                var accessibleLevels = GetAccessibleLevels(role, user.Department);

                var query = _context.Documents
                    .Where(d => d.IsActive && accessibleLevels.Contains(d.AccessLevel));

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(d => d.Category == category);
                }

                var totalItems = await query.CountAsync();

                var documents = await query
                    .OrderByDescending(d => d.UploadedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(d => new DocumentSummaryDto
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        Category = d.Category,
                        AccessLevel = d.AccessLevel,
                        Description = d.Description,
                        FileSizeBytes = d.FileSizeBytes,
                        FileType = d.FileType,
                        UploadedBy = d.UploadedBy,
                        UploadedAt = d.UploadedAt,
                        LastModified = d.LastModified,
                        Status = d.Status
                    })
                    .ToListAsync();

                return new PaginatedResponseDto<DocumentSummaryDto>
                {
                    Data = documents,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get document processing status
        /// </summary>
        public async Task<DocumentProcessingStatusDto?> GetDocumentProcessingStatusAsync(Guid documentId, string userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return null;

                var accessibleLevels = GetAccessibleLevels(user.Role, user.Department);

                var document = await _context.Documents
                    .Where(d => d.Id == documentId && accessibleLevels.Contains(d.AccessLevel))
                    .FirstOrDefaultAsync();

                if (document == null) return null;

                var progressPercentage = CalculateProgressPercentage(document.Status);
                var statusMessage = GetStatusMessage(document.Status);

                return new DocumentProcessingStatusDto
                {
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    Status = document.Status,
                    UploadedAt = document.UploadedAt,
                    ProcessedAt = document.ProcessedAt,
                    ProgressPercentage = progressPercentage,
                    StatusMessage = statusMessage,
                    HasErrors = document.Status == DocumentStatus.Failed,
                    ErrorMessages = document.Status == DocumentStatus.Failed ?
                        new List<string> { "Document processing failed. Please contact support." } : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document status for {DocumentId}", documentId);
                return null;
            }
        }

        /// <summary>
        /// Delete document and associated blob storage
        /// </summary>
        public async Task<bool> DeleteDocumentAsync(Guid documentId, string userId)
        {
            try
            {
                var document = await _context.Documents
                    .Include(d => d.Embeddings)
                    .Include(d => d.QueryReferences)
                    .Include(d => d.ResponseReferences)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null) return false;

                // Verify user has permission to delete (Admin or document owner)
                var user = await _context.Users.FindAsync(userId);
                if (user == null || (user.Role != "Admin" && document.UploadedBy != userId))
                {
                    return false;
                }

                // Soft delete - mark as inactive instead of hard delete to preserve references
                document.IsActive = false;
                document.LastModified = DateTime.UtcNow;

                // Delete blob storage file
                try
                {
                    await _blobService.DeleteDocumentAsync(document.BlobUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete blob storage file for document {DocumentId}", documentId);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentId} deleted by user {UserId}", documentId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                return false;
            }
        }

        /// <summary>
        /// Get available document categories based on user role
        /// </summary>
        public async Task<List<DocumentCategoryDto>> GetAvailableCategoriesAsync(string role)
        {
            try
            {
                var accessibleLevels = GetAccessibleLevels(role, "");

                var categories = await _context.Documents
                    .Where(d => d.IsActive && accessibleLevels.Contains(d.AccessLevel))
                    .GroupBy(d => d.Category)
                    .Select(g => new DocumentCategoryDto
                    {
                        Name = g.Key,
                        DocumentCount = g.Count(),
                        RequiredAccessLevel = g.Min(d => d.AccessLevel),
                        Description = GetCategoryDescription(g.Key)
                    })
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document categories for role {Role}", role);
                return new List<DocumentCategoryDto>();
            }
        }

        /// <summary>
        /// Update document metadata
        /// </summary>
        public async Task<bool> UpdateDocumentMetadataAsync(Guid documentId, DocumentUpdateRequestDto updateRequest, string userId)
        {
            try
            {
                var document = await _context.Documents.FindAsync(documentId);
                if (document == null) return false;

                // Verify user has permission to update (Admin or document owner)
                var user = await _context.Users.FindAsync(userId);
                if (user == null || (user.Role != "Admin" && document.UploadedBy != userId))
                {
                    return false;
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateRequest.Category))
                {
                    document.Category = updateRequest.Category;
                }

                if (updateRequest.AccessLevel.HasValue)
                {
                    document.AccessLevel = updateRequest.AccessLevel.Value;
                }

                if (updateRequest.Description != null)
                {
                    document.Description = updateRequest.Description;
                }

                document.LastModified = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentId} metadata updated by user {UserId}", documentId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId}", documentId);
                return false;
            }
        }

        /// <summary>
        /// Background process to extract text and create embeddings for uploaded documents
        /// </summary>
        private async Task ProcessDocumentAsync(Guid documentId)
        {
            try
            {
                var document = await _context.Documents.FindAsync(documentId);
                if (document == null) return;

                _logger.LogInformation("Starting background processing for document {DocumentId}", documentId);

                // Update status to processing
                document.Status = DocumentStatus.Processing;
                await _context.SaveChangesAsync();

                // Step 1: Extract text from document
                var extractedText = await ExtractTextFromDocumentAsync(document);
                if (string.IsNullOrEmpty(extractedText))
                {
                    document.Status = DocumentStatus.Failed;
                    await _context.SaveChangesAsync();
                    return;
                }

                document.ExtractedText = extractedText;
                document.Status = DocumentStatus.Processed;
                await _context.SaveChangesAsync();

                // Step 2: Generate summary and keywords
                var summary = await _aiService.SummarizeDocumentAsync(extractedText);
                var keywords = await _aiService.ExtractKeywordsAsync(extractedText);

                document.ContentSummary = summary;
                document.Keywords = string.Join(", ", keywords);

                // Step 3: Create embeddings for text chunks
                await CreateDocumentEmbeddingsAsync(document, extractedText);

                // Update final status
                document.Status = DocumentStatus.Indexed;
                document.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentId} processing completed successfully", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {DocumentId}", documentId);

                var document = await _context.Documents.FindAsync(documentId);
                if (document != null)
                {
                    document.Status = DocumentStatus.Failed;
                    await _context.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Extract text from different document types
        /// </summary>
        private async Task<string> ExtractTextFromDocumentAsync(Document document)
        {
            try
            {
                using var documentStream = await _blobService.DownloadDocumentAsync(document.BlobUrl);

                switch (document.FileType.ToLower())
                {
                    case ".txt":
                        return await ExtractTextFromTxtAsync(documentStream);
                    case ".pdf":
                        return await ExtractTextFromPdfAsync(documentStream);
                    case ".docx":
                        return await ExtractTextFromDocxAsync(documentStream);
                    case ".doc":
                        return await ExtractTextFromDocAsync(documentStream);
                    case ".pptx":
                        return await ExtractTextFromPptxAsync(documentStream);
                    case ".xlsx":
                        return await ExtractTextFromXlsxAsync(documentStream);
                    default:
                        _logger.LogWarning("Unsupported file type {FileType} for document {DocumentId}",
                            document.FileType, document.Id);
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from document {DocumentId}", document.Id);
                return string.Empty;
            }
        }

        /// <summary>
        /// Create embeddings for document text chunks
        /// </summary>
        private async Task CreateDocumentEmbeddingsAsync(Document document, string extractedText)
        {
            try
            {
                // Split text into chunks (max 1000 characters with overlap)
                const int chunkSize = 1000;
                const int overlap = 100;

                var chunks = SplitTextIntoChunks(extractedText, chunkSize, overlap);
                var embeddings = new List<DocumentEmbedding>();

                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];
                    if (string.IsNullOrWhiteSpace(chunk)) continue;

                    try
                    {
                        var embeddingVector = await _aiService.GenerateEmbeddingAsync(chunk);
                        var embeddingJson = System.Text.Json.JsonSerializer.Serialize(embeddingVector);

                        var embedding = new DocumentEmbedding
                        {
                            DocumentId = document.Id,
                            TextChunk = chunk,
                            ChunkIndex = i,
                            StartPosition = i * (chunkSize - overlap),
                            EndPosition = Math.Min(i * (chunkSize - overlap) + chunk.Length, extractedText.Length),
                            EmbeddingVector = embeddingJson,
                            EmbeddingModel = "text-embedding-ada-002",
                            CreatedAt = DateTime.UtcNow
                        };

                        embeddings.Add(embedding);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create embedding for chunk {ChunkIndex} of document {DocumentId}",
                            i, document.Id);
                    }
                }

                if (embeddings.Any())
                {
                    _context.DocumentEmbeddings.AddRange(embeddings);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created {EmbeddingCount} embeddings for document {DocumentId}",
                        embeddings.Count, document.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating embeddings for document {DocumentId}", document.Id);
                throw;
            }
        }

        // === TEXT EXTRACTION METHODS ===

        private static async Task<string> ExtractTextFromTxtAsync(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private async Task<string> ExtractTextFromPdfAsync(Stream stream)
        {
            // In a real implementation, you would use a PDF library like iTextSharp or PdfPig
            // For now, return placeholder text
            await Task.Delay(100); // Simulate processing time
            return "PDF text extraction would be implemented here using a PDF library like iTextSharp.";
        }

        private async Task<string> ExtractTextFromDocxAsync(Stream stream)
        {
            // In a real implementation, you would use DocumentFormat.OpenXml
            await Task.Delay(100);
            return "DOCX text extraction would be implemented here using DocumentFormat.OpenXml.";
        }

        private async Task<string> ExtractTextFromDocAsync(Stream stream)
        {
            // In a real implementation, you would use a library to handle legacy .doc files
            await Task.Delay(100);
            return "DOC text extraction would be implemented here using appropriate library.";
        }

        private async Task<string> ExtractTextFromPptxAsync(Stream stream)
        {
            // In a real implementation, you would use DocumentFormat.OpenXml for PowerPoint
            await Task.Delay(100);
            return "PPTX text extraction would be implemented here using DocumentFormat.OpenXml.";
        }

        private async Task<string> ExtractTextFromXlsxAsync(Stream stream)
        {
            // In a real implementation, you would use EPPlus or ClosedXML
            await Task.Delay(100);
            return "XLSX text extraction would be implemented here using EPPlus or ClosedXML.";
        }

        // === HELPER METHODS ===

        private static void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required");

            if (file.Length > 50 * 1024 * 1024) // 50MB
                throw new ArgumentException("File size exceeds 50MB limit");

            var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".txt", ".pptx", ".xlsx" };
            var fileExtension = Path.GetExtension(file.FileName)?.ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException($"File type {fileExtension} is not supported");
        }

        private static string GetContainerName(string category)
        {
            return $"documents-{category.ToLower().Replace(" ", "-")}";
        }

        private static int EstimateProcessingTime(long fileSizeBytes)
        {
            // Estimate processing time based on file size (rough calculation)
            var sizeMB = fileSizeBytes / (1024.0 * 1024.0);
            return Math.Max(1, (int)(sizeMB * 2)); // 2 minutes per MB, minimum 1 minute
        }

        private static List<AccessLevel> GetAccessibleLevels(string role, string department)
        {
            var levels = new List<AccessLevel> { AccessLevel.General };

            switch (role.ToUpper())
            {
                case "ADMIN":
                    return Enum.GetValues<AccessLevel>().ToList();
                case "EXECUTIVE":
                    levels.AddRange(new[] { AccessLevel.Departmental, AccessLevel.Management, AccessLevel.Executive });
                    break;
                case "MANAGER":
                    levels.AddRange(new[] { AccessLevel.Departmental, AccessLevel.Management });
                    break;
                default:
                    levels.Add(AccessLevel.Departmental);
                    break;
            }

            switch (department?.ToUpper())
            {
                case "HR":
                    levels.Add(AccessLevel.HR);
                    break;
                case "IT":
                    levels.Add(AccessLevel.IT);
                    break;
                case "FINANCE":
                    levels.Add(AccessLevel.Finance);
                    break;
            }

            return levels.Distinct().ToList();
        }

        private static int CalculateProgressPercentage(DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Uploaded => 10,
                DocumentStatus.Processing => 50,
                DocumentStatus.Processed => 80,
                DocumentStatus.Indexed => 100,
                DocumentStatus.Failed => 0,
                DocumentStatus.Archived => 100,
                _ => 0
            };
        }

        private static string GetStatusMessage(DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Uploaded => "Document uploaded successfully, queued for processing",
                DocumentStatus.Processing => "Extracting text and analyzing content",
                DocumentStatus.Processed => "Creating search indexes and embeddings",
                DocumentStatus.Indexed => "Document ready for search",
                DocumentStatus.Failed => "Processing failed",
                DocumentStatus.Archived => "Document archived",
                _ => "Unknown status"
            };
        }

        private static string GetCategoryDescription(string category)
        {
            return category switch
            {
                "HR" => "Human Resources policies and procedures",
                "IT" => "Technical documentation and support guides",
                "Finance" => "Financial procedures and reporting guidelines",
                "Legal" => "Legal documents and compliance information",
                "Operations" => "Operational procedures and workflows",
                "Training" => "Training materials and educational resources",
                _ => $"Documents in the {category} category"
            };
        }

        private static List<string> SplitTextIntoChunks(string text, int chunkSize, int overlap)
        {
            var chunks = new List<string>();
            var start = 0;

            while (start < text.Length)
            {
                var end = Math.Min(start + chunkSize, text.Length);
                var chunk = text.Substring(start, end - start);

                // Try to break at word boundaries
                if (end < text.Length && !char.IsWhiteSpace(text[end]))
                {
                    var lastSpace = chunk.LastIndexOf(' ');
                    if (lastSpace > 0 && lastSpace > chunk.Length / 2)
                    {
                        chunk = chunk.Substring(0, lastSpace);
                        end = start + lastSpace;
                    }
                }

                chunks.Add(chunk.Trim());
                start = end - overlap;

                if (start >= text.Length) break;
            }

            return chunks;
        }
    }
}