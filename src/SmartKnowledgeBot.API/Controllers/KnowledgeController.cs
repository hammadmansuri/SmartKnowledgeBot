using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartKnowledgeBot.Business.Interfaces;
using SmartKnowledgeBot.Domain.DTOs;
using SmartKnowledgeBot.Domain.Models;
using System.Security.Claims;

namespace SmartKnowledgeBot.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KnowledgeController : ControllerBase
    {
        private readonly IKnowledgeService _knowledgeService;
        private readonly ILogger<KnowledgeController> _logger;

        public KnowledgeController(
            IKnowledgeService knowledgeService,
            ILogger<KnowledgeController> logger)
        {
            _knowledgeService = knowledgeService;
            _logger = logger;
        }

        /// <summary>
        /// Process a knowledge query and return AI-generated response
        /// </summary>
        /// <param name="request">Query request containing the user's question</param>
        /// <returns>Knowledge response with answer and metadata</returns>
        [HttpPost("query")]
        [ProducesResponseType(typeof(KnowledgeResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<KnowledgeResponseDto>> ProcessQuery(
            [FromBody] KnowledgeQueryDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Invalid query format",
                        Details = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var userContext = GetUserContext();
                var query = new KnowledgeQuery
                {
                    Query = request.Query,
                    UserId = userContext.UserId,
                    UserRole = userContext.Role,
                    Department = userContext.Department,
                    Timestamp = DateTime.UtcNow,
                    SessionId = request.SessionId
                };

                _logger.LogInformation("Processing knowledge query for user {UserId} with role {Role}",
                    userContext.UserId, userContext.Role);

                var response = await _knowledgeService.ProcessQueryAsync(query);

                return Ok(new KnowledgeResponseDto
                {
                    Answer = response.Answer,
                    Source = response.Source,
                    ConfidenceScore = response.ConfidenceScore,
                    IsFromStructuredData = response.IsFromStructuredData,
                    RelevantDocuments = response.RelevantDocuments?.Select(rd => new DocumentReferenceDto
                    {
                        DocumentId = rd.Document.Id,
                        Title = rd.Document.FileName,          // Use FileName as Title
                        Category = rd.Document.Category,
                        LastModified = rd.Document.LastModified ?? rd.Document.UploadedAt,
                        RelevanceScore = rd.RelevanceScore,
                        CitedText = rd.CitedText
                    }).ToList(),
                    ResponseTime = response.ResponseTime,
                    QueryId = response.QueryId
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access attempt: {Message}", ex.Message);
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid query parameter: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing knowledge query");
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "An error occurred while processing your query",
                    Details = new List<string> { "Please try again later or contact support" }
                });
            }
        }

        /// <summary>
        /// Get query history for the current user
        /// </summary>
        /// <param name="page">Page number for pagination</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Paginated list of user's query history</returns>
        [HttpGet("history")]
        [ProducesResponseType(typeof(PaginatedResponseDto<QueryHistoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponseDto<QueryHistoryDto>>> GetQueryHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userContext = GetUserContext();
                var history = await _knowledgeService.GetUserQueryHistoryAsync(
                    userContext.UserId, page, pageSize);

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving query history");
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "Unable to retrieve query history"
                });
            }
        }

        /// <summary>
        /// Provide feedback on a query response
        /// </summary>
        /// <param name="queryId">The ID of the query to provide feedback for</param>
        /// <param name="feedback">Feedback details</param>
        /// <returns>Success response</returns>
        [HttpPost("feedback")]
        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<SuccessResponseDto>> ProvideFeedback(
            [FromQuery] Guid queryId,
            [FromBody] QueryFeedbackDto feedback)
        {
            try
            {
                var userContext = GetUserContext();
                await _knowledgeService.RecordFeedbackAsync(queryId, userContext.UserId, feedback);

                return Ok(new SuccessResponseDto
                {
                    Message = "Feedback recorded successfully",
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording feedback for query {QueryId}", queryId);
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "Unable to record feedback"
                });
            }
        }

        /// <summary>
        /// Get suggested queries based on user role and department
        /// </summary>
        /// <returns>List of suggested queries</returns>
        [HttpGet("suggestions")]
        [ProducesResponseType(typeof(List<QuerySuggestionDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<QuerySuggestionDto>>> GetQuerySuggestions()
        {
            try
            {
                var userContext = GetUserContext();
                var suggestions = await _knowledgeService.GetQuerySuggestionsAsync(
                    userContext.Role, userContext.Department);

                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving query suggestions");
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "Unable to retrieve suggestions"
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