using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartKnowledgeBot.Business.Interfaces;
using SmartKnowledgeBot.Domain.DTOs;
using SmartKnowledgeBot.Domain.Enums;
using SmartKnowledgeBot.Domain.Models;
using System.Security.Claims;

namespace SmartKnowledgeBot.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            IDocumentService documentService,
            ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        /// <summary>
        /// Upload a new document to the knowledge base
        /// </summary>
        /// <param name="file">The document file to upload</param>
        /// <param name="category">Document category</param>
        /// <param name="accessLevel">Who can access this document</param>
        /// <param name="description">Optional description of the document</param>
        /// <returns>Upload result with document metadata</returns>
        [HttpPost("upload")]
        [Authorize(Roles = "Admin,DocumentManager")]
        [ProducesResponseType(typeof(DocumentUploadResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status413PayloadTooLarge)]
        public async Task<ActionResult<DocumentUploadResponseDto>> UploadDocument(
            IFormFile file,
            [FromForm] string category,
            [FromForm] AccessLevel accessLevel,
            [FromForm] string? description = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "No file uploaded",
                        Details = new List<string> { "Please select a file to upload" }
                    });
                }

                // File size validation (50MB limit)
                if (file.Length > 50 * 1024 * 1024)
                {
                    return StatusCode(413, new ErrorResponseDto
                    {
                        Message = "File size exceeds the 50MB limit"
                    });
                }

                // File type validation
                var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".txt", ".pptx", ".xlsx" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Unsupported file type",
                        Details = new List<string> { $"Supported types: {string.Join(", ", allowedExtensions)}" }
                    });
                }

                var userContext = GetUserContext();
                var uploadRequest = new DocumentUploadRequest
                {
                    File = file,
                    Category = category,
                    AccessLevel = accessLevel,
                    Description = description,
                    UploadedBy = userContext.UserId,
                    UploadedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Processing document upload: {FileName} by user {UserId}",
                    file.FileName, userContext.UserId);

                var result = await _documentService.UploadDocumentAsync(uploadRequest);

                return Ok(new DocumentUploadResponseDto
                {
                    DocumentId = result.DocumentId,
                    FileName = result.FileName,
                    Category = result.Category,
                    Status = result.Status,
                    ProcessingStarted = result.ProcessingStarted,
                    EstimatedCompletionTime = result.EstimatedCompletionTime,
                    BlobUrl = result.BlobUrl
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid upload parameters: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "An error occurred while uploading the document"
                });
            }
        }

        /// <summary>
        /// Get list of documents accessible to the current user
        /// </summary>
        /// <param name="category">Optional category filter</param>
        /// <param name="page">Page number for pagination</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Paginated list of documents</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponseDto<DocumentSummaryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponseDto<DocumentSummaryDto>>> GetDocuments(
            [FromQuery] string? category = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userContext = GetUserContext();
                var documents = await _documentService.GetUserAccessibleDocumentsAsync(
                    userContext.UserId, userContext.Role, category, page, pageSize);

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents");
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "Unable to retrieve documents"
                });
            }
        }

        /// <summary>
        /// Get document processing status
        /// </summary>
        /// <param name="documentId">Document ID to check status for</param>
        /// <returns>Processing status and details</returns>
        [HttpGet("{documentId}/status")]
        [ProducesResponseType(typeof(DocumentProcessingStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentProcessingStatusDto>> GetDocumentStatus(Guid documentId)
        {
            try
            {
                var userContext = GetUserContext();
                var status = await _documentService.GetDocumentProcessingStatusAsync(documentId, userContext.UserId);

                if (status == null)
                {
                    return NotFound(new ErrorResponseDto
                    {
                        Message = "Document not found or access denied"
                    });
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document status for {DocumentId}", documentId);
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "Unable to retrieve document status"
                });
            }
        }

        /// <summary>
        /// Delete a document from the knowledge base
        /// </summary>
        /// <param name="documentId">Document ID to delete</param>
        /// <returns>Success response</returns>
        [HttpDelete("{documentId}")]
        [Authorize(Roles = "Admin,DocumentManager")]
        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SuccessResponseDto>> DeleteDocument(Guid documentId)
        {
            try
            {
                var userContext = GetUserContext();
                var deleted = await _documentService.DeleteDocumentAsync(documentId, userContext.UserId);

                if (!deleted)
                {
                    return NotFound(new ErrorResponseDto
                    {
                        Message = "Document not found or access denied"
                    });
                }

                _logger.LogInformation("Document {DocumentId} deleted by user {UserId}",
                    documentId, userContext.UserId);

                return Ok(new SuccessResponseDto
                {
                    Message = "Document deleted successfully",
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "Unable to delete document"
                });
            }
        }

        /// <summary>
        /// Get document categories available to the current user
        /// </summary>
        /// <returns>List of available categories</returns>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<DocumentCategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DocumentCategoryDto>>> GetDocumentCategories()
        {
            try
            {
                var userContext = GetUserContext();
                var categories = await _documentService.GetAvailableCategoriesAsync(userContext.Role);

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document categories");
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "Unable to retrieve categories"
                });
            }
        }

        /// <summary>
        /// Update document metadata
        /// </summary>
        /// <param name="documentId">Document ID to update</param>
        /// <param name="updateRequest">Updated document metadata</param>
        /// <returns>Success response</returns>
        [HttpPatch("{documentId}")]
        [Authorize(Roles = "Admin,DocumentManager")]
        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SuccessResponseDto>> UpdateDocument(
            Guid documentId,
            [FromBody] DocumentUpdateRequestDto updateRequest)
        {
            try
            {
                var userContext = GetUserContext();
                var updated = await _documentService.UpdateDocumentMetadataAsync(
                    documentId, updateRequest, userContext.UserId);

                if (!updated)
                {
                    return NotFound(new ErrorResponseDto
                    {
                        Message = "Document not found or access denied"
                    });
                }

                return Ok(new SuccessResponseDto
                {
                    Message = "Document updated successfully",
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId}", documentId);
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "Unable to update document"
                });
            }
        }

        private UserContext GetUserContext()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var department = User.FindFirst("department")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            {
                throw new UnauthorizedAccessException("Invalid user context");
            }

            return new UserContext
            {
                UserId = userId,
                Role = role,
                Department = department ?? "General",
                Email = email
            };
        }
    }
}