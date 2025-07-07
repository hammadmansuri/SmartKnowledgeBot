using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartKnowledgeBot.Business.Interfaces;
using SmartKnowledgeBot.Domain.DTOs;
using SmartKnowledgeBot.Domain.Models;
using SmartKnowledgeBot.Domain.Enums;
using SmartKnowledgeBot.Infrastructure.Data;
using System.Diagnostics;

namespace SmartKnowledgeBot.Business.Services
{
    /// <summary>
    /// Knowledge service implementing RAG pattern for enterprise knowledge retrieval
    /// </summary>
    public class KnowledgeService : IKnowledgeService
    {
        private readonly SmartKnowledgeBotDbContext _context;
        private readonly IAzureOpenAIService _aiService;
        private readonly IBlobStorageService _blobService;
        private readonly ILogger<KnowledgeService> _logger;

        public KnowledgeService(
            SmartKnowledgeBotDbContext context,
            IAzureOpenAIService aiService,
            IBlobStorageService blobService,
            ILogger<KnowledgeService> logger)
        {
            _context = context;
            _aiService = aiService;
            _blobService = blobService;
            _logger = logger;
        }

        /// <summary>
        /// Process knowledge query using RAG pattern: structured data first, then AI-powered document search
        /// </summary>
        public async Task<KnowledgeResponse> ProcessQueryAsync(KnowledgeQuery query)
        {
            var stopwatch = Stopwatch.StartNew();
            var queryId = query.Id;

            try
            {
                _logger.LogInformation("Processing knowledge query {QueryId} for user {UserId}",
                    queryId, query.UserId);

                // Step 1: Try structured knowledge first (faster response)
                var structuredResult = await SearchStructuredKnowledgeAsync(query);
                if (structuredResult != null)
                {
                    stopwatch.Stop();
                    var structuredResponse = new KnowledgeResponse
                    {
                        QueryId = queryId,
                        Answer = structuredResult.Answer,
                        Source = structuredResult.Source ?? "Knowledge Base",
                        ConfidenceScore = 0.95, // High confidence for exact matches
                        IsFromStructuredData = true,
                        ResponseTime = (int)stopwatch.ElapsedMilliseconds,
                        GeneratedAt = DateTime.UtcNow,
                        ModelUsed = "StructuredDB"
                    };

                    await SaveQueryAndResponseAsync(query, structuredResponse);
                    return structuredResponse;
                }

                // Step 2: Use AI-powered document search for unstructured data
                var aiResponse = await ProcessUnstructuredQueryAsync(query);
                stopwatch.Stop();
                aiResponse.ResponseTime = (int)stopwatch.ElapsedMilliseconds;

                await SaveQueryAndResponseAsync(query, aiResponse);
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing knowledge query {QueryId}", queryId);
                stopwatch.Stop();

                // Return fallback response
                var errorResponse = new KnowledgeResponse
                {
                    QueryId = queryId,
                    Answer = "I'm sorry, I encountered an error while processing your query. Please try again or contact support if the issue persists.",
                    Source = "System",
                    ConfidenceScore = 0.0,
                    IsFromStructuredData = false,
                    ResponseTime = (int)stopwatch.ElapsedMilliseconds,
                    GeneratedAt = DateTime.UtcNow,
                    ModelUsed = "Error"
                };

                await SaveQueryAndResponseAsync(query, errorResponse);
                return errorResponse;
            }
        }

        /// <summary>
        /// Search structured knowledge base for exact or similar matches
        /// </summary>
        private async Task<StructuredKnowledge?> SearchStructuredKnowledgeAsync(KnowledgeQuery query)
        {
            try
            {
                var accessibleLevels = GetAccessibleLevels(query.UserRole, query.Department);
                var searchTerms = ExtractSearchTerms(query.Query);

                // Try exact question match first
                var exactMatch = await _context.StructuredKnowledge
                    .Where(k => k.IsActive && accessibleLevels.Contains(k.AccessLevel))
                    .Where(k => EF.Functions.Like(k.Question, $"%{query.Query}%"))
                    .OrderByDescending(k => k.Priority)
                    .FirstOrDefaultAsync();

                if (exactMatch != null)
                {
                    _logger.LogInformation("Found exact structured match for query {QueryId}", query.Id);
                    return exactMatch;
                }

                // Try keyword-based search
                var keywordMatches = await _context.StructuredKnowledge
                    .Where(k => k.IsActive && accessibleLevels.Contains(k.AccessLevel))
                    .Where(k => searchTerms.Any(term => k.Keywords.Contains(term) || k.Question.Contains(term)))
                    .OrderByDescending(k => k.Priority)
                    .Take(5)
                    .ToListAsync();

                if (keywordMatches.Any())
                {
                    // Return the best match based on keyword overlap
                    var bestMatch = keywordMatches
                        .OrderByDescending(k => CalculateKeywordRelevance(k, searchTerms))
                        .First();

                    _logger.LogInformation("Found keyword-based structured match for query {QueryId}", query.Id);
                    return bestMatch;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching structured knowledge for query {QueryId}", query.Id);
                return null;
            }
        }

        /// <summary>
        /// Process query using AI and document embeddings
        /// </summary>
        private async Task<KnowledgeResponse> ProcessUnstructuredQueryAsync(KnowledgeQuery query)
        {
            try
            {
                _logger.LogInformation("Processing unstructured query {QueryId} with AI", query.Id);

                // Generate query embedding
                var queryEmbedding = await _aiService.GenerateEmbeddingAsync(query.Query);

                // Find relevant documents using vector similarity
                var relevantDocuments = await FindRelevantDocumentsAsync(query, queryEmbedding);

                if (!relevantDocuments.Any())
                {
                    return new KnowledgeResponse
                    {
                        QueryId = query.Id,
                        Answer = "I couldn't find relevant information in the available documents. Please try rephrasing your question or contact your administrator if you believe this information should be available.",
                        Source = "AI Search",
                        ConfidenceScore = 0.0,
                        IsFromStructuredData = false,
                        GeneratedAt = DateTime.UtcNow,
                        ModelUsed = "No Results"
                    };
                }

                // Extract relevant text chunks from documents
                var documentContexts = await ExtractDocumentContextsAsync(relevantDocuments);

                // Generate AI response using retrieved documents
                var userContext = $"Role: {query.UserRole}, Department: {query.Department}";
                var aiAnswer = await _aiService.GenerateResponseAsync(query.Query, documentContexts, userContext);

                // Calculate confidence based on document relevance
                var avgRelevance = relevantDocuments.Average(d => d.RelevanceScore);
                var confidenceScore = Math.Min(avgRelevance * 1.2, 1.0); // Boost confidence slightly

                var response = new KnowledgeResponse
                {
                    QueryId = query.Id,
                    Answer = aiAnswer,
                    Source = "AI + Documents",
                    ConfidenceScore = confidenceScore,
                    IsFromStructuredData = false,
                    GeneratedAt = DateTime.UtcNow,
                    ModelUsed = "gpt-4",
                    TokensUsed = EstimateTokenUsage(query.Query, documentContexts, aiAnswer)
                };

                // Store document references
                await SaveDocumentReferencesAsync(query.Id, response.Id, relevantDocuments);

                _logger.LogInformation("Generated AI response for query {QueryId} with confidence {Confidence}",
                    query.Id, confidenceScore);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unstructured query {QueryId}", query.Id);
                throw;
            }
        }

        /// <summary>
        /// Find relevant documents using vector similarity search
        /// </summary>
        private async Task<List<DocumentWithRelevance>> FindRelevantDocumentsAsync(KnowledgeQuery query, float[] queryEmbedding)
        {
            try
            {
                var accessibleLevels = GetAccessibleLevels(query.UserRole, query.Department);

                // Get accessible documents with embeddings
                var documentsWithEmbeddings = await _context.Documents
                    .Where(d => d.IsActive &&
                               d.Status == DocumentStatus.Indexed &&
                               accessibleLevels.Contains(d.AccessLevel))
                    .Include(d => d.Embeddings)
                    .ToListAsync();

                var relevantDocs = new List<DocumentWithRelevance>();

                foreach (var document in documentsWithEmbeddings)
                {
                    var bestChunkScore = 0.0;
                    DocumentEmbedding? bestChunk = null;

                    foreach (var embedding in document.Embeddings)
                    {
                        var embeddingVector = DeserializeEmbedding(embedding.EmbeddingVector);
                        var similarity = CalculateCosineSimilarity(queryEmbedding, embeddingVector);

                        if (similarity > bestChunkScore)
                        {
                            bestChunkScore = similarity;
                            bestChunk = embedding;
                        }
                    }

                    // Include documents with relevance score > 0.7
                    if (bestChunkScore > 0.7 && bestChunk != null)
                    {
                        relevantDocs.Add(new DocumentWithRelevance
                        {
                            Document = document,
                            RelevanceScore = bestChunkScore,
                            RelevantChunk = bestChunk
                        });
                    }
                }

                // Return top 5 most relevant documents
                return relevantDocs
                    .OrderByDescending(d => d.RelevanceScore)
                    .Take(5)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding relevant documents for query {QueryId}", query.Id);
                return new List<DocumentWithRelevance>();
            }
        }

        /// <summary>
        /// Extract document contexts for AI processing
        /// </summary>
        private async Task<List<string>> ExtractDocumentContextsAsync(List<DocumentWithRelevance> relevantDocuments)
        {
            var contexts = new List<string>();

            foreach (var docWithRelevance in relevantDocuments)
            {
                try
                {
                    var chunk = docWithRelevance.RelevantChunk;
                    var document = docWithRelevance.Document;

                    // Create context with document metadata and relevant text
                    var context = $"Document: {document.FileName} (Category: {document.Category})\n" +
                                 $"Relevance: {docWithRelevance.RelevanceScore:F2}\n" +
                                 $"Content: {chunk.TextChunk}\n";

                    contexts.Add(context);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error extracting context from document {DocumentId}",
                        docWithRelevance.Document.Id);
                }
            }

            return contexts;
        }

        /// <summary>
        /// Get user query history with pagination
        /// </summary>
        public async Task<PaginatedResponseDto<QueryHistoryDto>> GetUserQueryHistoryAsync(string userId, int page, int pageSize)
        {
            try
            {
                var totalItems = await _context.KnowledgeQueries
                    .CountAsync(q => q.UserId == userId);

                var queries = await _context.KnowledgeQueries
                    .Where(q => q.UserId == userId)
                    .OrderByDescending(q => q.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(q => q.Response)
                    .Include(q => q.Feedbacks)
                    .Select(q => new QueryHistoryDto
                    {
                        Id = q.Id,
                        Query = q.Query,
                        Timestamp = q.Timestamp,
                        IsAnswered = q.IsAnswered,
                        ConfidenceScore = q.ConfidenceScore,
                        AnswerSource = q.AnswerSource,
                        ResponseTimeMs = q.ResponseTimeMs,
                        HasFeedback = q.Feedbacks.Any()
                    })
                    .ToListAsync();

                return new PaginatedResponseDto<QueryHistoryDto>
                {
                    Data = queries,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving query history for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Record user feedback for continuous improvement
        /// </summary>
        public async Task RecordFeedbackAsync(Guid queryId, string userId, QueryFeedbackDto feedback)
        {
            try
            {
                // Check if feedback already exists
                var existingFeedback = await _context.QueryFeedbacks
                    .FirstOrDefaultAsync(f => f.QueryId == queryId && f.UserId == userId);

                if (existingFeedback != null)
                {
                    // Update existing feedback
                    existingFeedback.FeedbackType = feedback.FeedbackType;
                    existingFeedback.Rating = feedback.Rating;
                    existingFeedback.Comments = feedback.Comments;
                    existingFeedback.CreatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new feedback
                    var newFeedback = new QueryFeedback
                    {
                        QueryId = queryId,
                        UserId = userId,
                        FeedbackType = feedback.FeedbackType,
                        Rating = feedback.Rating,
                        Comments = feedback.Comments,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.QueryFeedbacks.Add(newFeedback);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Recorded feedback for query {QueryId} by user {UserId}", queryId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording feedback for query {QueryId}", queryId);
                throw;
            }
        }

        /// <summary>
        /// Get suggested queries based on user role and department
        /// </summary>
        public async Task<List<QuerySuggestionDto>> GetQuerySuggestionsAsync(string role, string department)
        {
            try
            {
                var accessibleLevels = GetAccessibleLevels(role, department);

                // Get popular queries from analytics
                var suggestions = await _context.StructuredKnowledge
                    .Where(k => k.IsActive && accessibleLevels.Contains(k.AccessLevel))
                    .OrderByDescending(k => k.Priority)
                    .Take(10)
                    .Select(k => new QuerySuggestionDto
                    {
                        Suggestion = k.Question,
                        Category = k.Category,
                        PopularityScore = k.Priority,
                        Description = k.Answer.Length > 100 ? k.Answer.Substring(0, 100) + "..." : k.Answer
                    })
                    .ToListAsync();

                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving query suggestions for role {Role}", role);
                return new List<QuerySuggestionDto>();
            }
        }

        // === HELPER METHODS ===

        private async Task SaveQueryAndResponseAsync(KnowledgeQuery query, KnowledgeResponse response)
        {
            try
            {
                // Update query with response metadata
                query.IsAnswered = true;
                query.ConfidenceScore = response.ConfidenceScore;
                query.AnswerSource = response.IsFromStructuredData ? "SQL" : "AI";
                query.ResponseTimeMs = response.ResponseTime;

                _context.KnowledgeQueries.Add(query);
                _context.KnowledgeResponses.Add(response);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving query and response for query {QueryId}", query.Id);
                throw;
            }
        }

        private async Task SaveDocumentReferencesAsync(Guid queryId, Guid responseId, List<DocumentWithRelevance> relevantDocuments)
        {
            try
            {
                var queryReferences = relevantDocuments.Select(d => new QueryDocumentReference
                {
                    QueryId = queryId,
                    DocumentId = d.Document.Id,
                    RelevanceScore = d.RelevanceScore,
                    WasUsedInResponse = true
                }).ToList();

                var responseReferences = relevantDocuments.Select(d => new ResponseDocumentReference
                {
                    ResponseId = responseId,
                    DocumentId = d.Document.Id,
                    RelevanceScore = d.RelevanceScore,
                    CitedText = d.RelevantChunk?.TextChunk?.Substring(0, Math.Min(d.RelevantChunk.TextChunk.Length, 500))
                }).ToList();

                _context.QueryDocumentReferences.AddRange(queryReferences);
                _context.ResponseDocumentReferences.AddRange(responseReferences);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving document references for query {QueryId}", queryId);
            }
        }

        private static List<AccessLevel> GetAccessibleLevels(string role, string department)
        {
            var levels = new List<AccessLevel> { AccessLevel.General };

            switch (role.ToUpper())
            {
                case "ADMIN":
                    return Enum.GetValues<AccessLevel>().ToList(); // Admin can access everything
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

            // Add department-specific access
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

        private static List<string> ExtractSearchTerms(string query)
        {
            return query.ToLower()
                .Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(term => term.Length > 2) // Ignore short words
                .Distinct()
                .ToList();
        }

        private static int CalculateKeywordRelevance(StructuredKnowledge knowledge, List<string> searchTerms)
        {
            var allText = $"{knowledge.Question} {knowledge.Keywords}".ToLower();
            return searchTerms.Count(term => allText.Contains(term));
        }

        private static float[] DeserializeEmbedding(string embeddingJson)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<float[]>(embeddingJson) ?? Array.Empty<float>();
            }
            catch
            {
                return Array.Empty<float>();
            }
        }

        private static double CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length || vector1.Length == 0)
                return 0.0;

            double dotProduct = 0.0;
            double magnitude1 = 0.0;
            double magnitude2 = 0.0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            if (magnitude1 == 0.0 || magnitude2 == 0.0)
                return 0.0;

            return dotProduct / (Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));
        }

        private static int EstimateTokenUsage(string query, List<string> contexts, string response)
        {
            // Rough estimation: 1 token ≈ 4 characters
            var totalChars = query.Length + contexts.Sum(c => c.Length) + response.Length;
            return totalChars / 4;
        }

        // Helper class for document relevance
        private class DocumentWithRelevance
        {
            public Document Document { get; set; } = null!;
            public double RelevanceScore { get; set; }
            public DocumentEmbedding? RelevantChunk { get; set; }
        }
    }
}