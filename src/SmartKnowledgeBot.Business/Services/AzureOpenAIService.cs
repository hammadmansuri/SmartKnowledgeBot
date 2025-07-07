using Azure;
using Azure.AI.OpenAI;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartKnowledgeBot.Business.Interfaces;
using System.Text;
using System.Text.Json;

namespace SmartKnowledgeBot.Infrastructure.Services
{
    /// <summary>
    /// Azure OpenAI service for AI-powered knowledge processing
    /// </summary>
    public class AzureOpenAIService : IAzureOpenAIService
    {
        private readonly OpenAIClient _openAIClient;
        private readonly ILogger<AzureOpenAIService> _logger;
        private readonly string _chatDeploymentName;
        private readonly string _embeddingDeploymentName;

        public AzureOpenAIService(IConfiguration configuration, ILogger<AzureOpenAIService> logger)
        {
            _logger = logger;

            var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");
            var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("Azure OpenAI API key not configured");

            _chatDeploymentName = configuration["AzureOpenAI:ChatDeploymentName"] ?? "gpt-4";
            _embeddingDeploymentName = configuration["AzureOpenAI:EmbeddingDeploymentName"] ?? "text-embedding-ada-002";

            _openAIClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }

        /// <summary>
        /// Generate AI response using retrieved documents and user context
        /// </summary>
        public async Task<string> GenerateResponseAsync(string query, List<string> relevantDocuments, string userContext)
        {
            try
            {
                _logger.LogInformation("Generating AI response for query with {DocumentCount} relevant documents", relevantDocuments.Count);

                var systemPrompt = BuildSystemPrompt(userContext);
                var userPrompt = BuildUserPrompt(query, relevantDocuments);

                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _chatDeploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage(systemPrompt),
                        new ChatRequestUserMessage(userPrompt)
                    },
                    MaxTokens = 1000,
                    Temperature = 0.3f, // Lower temperature for more consistent responses
                    FrequencyPenalty = 0.0f,
                    PresencePenalty = 0.0f
                };

                var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                var aiResponse = response.Value.Choices[0].Message.Content;

                _logger.LogInformation("AI response generated successfully, tokens used: {TokensUsed}",
                    response.Value.Usage.TotalTokens);

                return aiResponse ?? "I'm sorry, I couldn't generate a response to your query.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI response");
                return "I'm sorry, I encountered an error while processing your request. Please try again.";
            }
        }

        /// <summary>
        /// Generate embedding vector for text using Azure OpenAI
        /// </summary>
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return Array.Empty<float>();
                }

                // Truncate text if too long (ada-002 has 8192 token limit)
                var truncatedText = TruncateText(text, 8000);

                var embeddingsOptions = new EmbeddingsOptions(_embeddingDeploymentName, new[] { truncatedText });
                var response = await _openAIClient.GetEmbeddingsAsync(embeddingsOptions);

                var embedding = response.Value.Data[0].Embedding.ToArray();

                _logger.LogDebug("Generated embedding for text of length {TextLength}, vector dimension: {VectorDimension}",
                    text.Length, embedding.Length);

                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for text");
                return Array.Empty<float>();
            }
        }

        /// <summary>
        /// Extract keywords from text using AI
        /// </summary>
        public async Task<List<string>> ExtractKeywordsAsync(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return new List<string>();
                }

                var truncatedText = TruncateText(text, 3000);
                var prompt = BuildKeywordExtractionPrompt(truncatedText);

                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _chatDeploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage("You are a helpful assistant that extracts important keywords and phrases from documents."),
                        new ChatRequestUserMessage(prompt)
                    },
                    MaxTokens = 200,
                    Temperature = 0.1f
                };

                var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                var keywordResponse = response.Value.Choices[0].Message.Content;

                var keywords = ParseKeywordsFromResponse(keywordResponse);

                _logger.LogDebug("Extracted {KeywordCount} keywords from text", keywords.Count);
                return keywords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting keywords from text");
                return new List<string>();
            }
        }

        /// <summary>
        /// Generate summary of document content using AI
        /// </summary>
        public async Task<string> SummarizeDocumentAsync(string documentText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(documentText))
                {
                    return "No content available for summarization.";
                }

                var truncatedText = TruncateText(documentText, 3000);
                var prompt = BuildSummarizationPrompt(truncatedText);

                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _chatDeploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage("You are a helpful assistant that creates concise, informative summaries of business documents."),
                        new ChatRequestUserMessage(prompt)
                    },
                    MaxTokens = 300,
                    Temperature = 0.2f
                };

                var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                var summary = response.Value.Choices[0].Message.Content;

                _logger.LogDebug("Generated summary for document of length {DocumentLength}", documentText.Length);
                return summary ?? "Unable to generate summary.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error summarizing document");
                return "Error occurred while generating summary.";
            }
        }

        // === PRIVATE HELPER METHODS ===

        private static string BuildSystemPrompt(string userContext)
        {
            return $@"You are an intelligent enterprise knowledge assistant. Your role is to provide accurate, helpful responses to employee questions based on company documents and policies.

User Context: {userContext}

Guidelines:
1. Provide clear, concise answers based on the provided documents
2. If information is not available in the documents, state this clearly
3. Maintain a professional, helpful tone
4. Reference relevant documents when citing information
5. Respect role-based access - only provide information appropriate for the user's role
6. If the question requires specific permissions or approvals, mention this
7. Suggest next steps or additional resources when helpful

Always structure your response with:
- Direct answer to the question
- Supporting details from documents
- Any relevant warnings or additional considerations";
        }

        private static string BuildUserPrompt(string query, List<string> relevantDocuments)
        {
            var documentsText = string.Join("\n\n---\n\n", relevantDocuments);

            return $@"Question: {query}

Relevant Documents:
{documentsText}

Please provide a comprehensive answer based on the above documents. If the documents don't contain enough information to fully answer the question, please indicate what additional information might be needed.";
        }

        private static string BuildKeywordExtractionPrompt(string text)
        {
            return $@"Extract the most important keywords and phrases from the following text. Focus on:
- Key concepts and topics
- Important terms and terminology
- Main subjects discussed
- Action items or processes

Return the keywords as a comma-separated list, with the most important ones first.

Text:
{text}

Keywords:";
        }

        private static string BuildSummarizationPrompt(string text)
        {
            return $@"Create a concise summary of the following document that captures:
- Main purpose and scope
- Key points and important information
- Any action items or requirements
- Target audience (if apparent)

Keep the summary professional and informative, suitable for enterprise knowledge management.

Document:
{text}

Summary:";
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (text.Length <= maxLength)
                return text;

            // Try to truncate at word boundary
            var truncated = text.Substring(0, maxLength);
            var lastSpace = truncated.LastIndexOf(' ');

            if (lastSpace > maxLength / 2)
            {
                return truncated.Substring(0, lastSpace) + "...";
            }

            return truncated + "...";
        }

        private static List<string> ParseKeywordsFromResponse(string? response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return new List<string>();

            return response
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim().Trim('"', '\''))
                .Where(k => !string.IsNullOrWhiteSpace(k) && k.Length > 2)
                .Take(20) // Limit to top 20 keywords
                .ToList();
        }
    }

    /// <summary>
    /// Azure Blob Storage service for document storage and retrieval
    /// </summary>
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobStorageService> _logger;

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            _logger = logger;

            var connectionString = configuration.GetConnectionString("AzureBlobStorage")
                ?? throw new InvalidOperationException("Azure Blob Storage connection string not configured");

            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        /// <summary>
        /// Upload document to blob storage
        /// </summary>
        public async Task<string> UploadDocumentAsync(Stream documentStream, string fileName, string containerName)
        {
            try
            {
                _logger.LogInformation("Uploading document {FileName} to container {ContainerName}", fileName, containerName);

                // Ensure container exists
                var containerClient = await GetOrCreateContainerAsync(containerName);

                // Create blob client
                var blobClient = containerClient.GetBlobClient(fileName);

                // Set content type based on file extension
                var contentType = GetContentType(fileName);
                var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

                // Upload with metadata
                var metadata = new Dictionary<string, string>
                {
                    ["uploadedAt"] = DateTime.UtcNow.ToString("O"),
                    ["originalFileName"] = fileName
                };

                await blobClient.UploadAsync(documentStream, new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders,
                    Metadata = metadata
                });

                var blobUrl = blobClient.Uri.ToString();
                _logger.LogInformation("Document uploaded successfully to {BlobUrl}", blobUrl);

                return blobUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document {FileName} to container {ContainerName}", fileName, containerName);
                throw;
            }
        }

        /// <summary>
        /// Download document from blob storage
        /// </summary>
        public async Task<Stream> DownloadDocumentAsync(string blobUrl)
        {
            try
            {
                _logger.LogDebug("Downloading document from {BlobUrl}", blobUrl);

                var blobUri = new Uri(blobUrl);
                var blobClient = new BlobClient(blobUri);

                var response = await blobClient.DownloadStreamingAsync();
                return response.Value.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document from {BlobUrl}", blobUrl);
                throw;
            }
        }

        /// <summary>
        /// Delete document from blob storage
        /// </summary>
        public async Task<bool> DeleteDocumentAsync(string blobUrl)
        {
            try
            {
                _logger.LogInformation("Deleting document from {BlobUrl}", blobUrl);

                var blobUri = new Uri(blobUrl);
                var blobClient = new BlobClient(blobUri);

                var response = await blobClient.DeleteIfExistsAsync();

                if (response.Value)
                {
                    _logger.LogInformation("Document deleted successfully from {BlobUrl}", blobUrl);
                }
                else
                {
                    _logger.LogWarning("Document not found for deletion at {BlobUrl}", blobUrl);
                }

                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document from {BlobUrl}", blobUrl);
                return false;
            }
        }

        /// <summary>
        /// Get document URL for a specific container and file name
        /// </summary>
        public async Task<string> GetDocumentUrlAsync(string containerName, string fileName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                // Check if blob exists
                var exists = await blobClient.ExistsAsync();
                if (!exists.Value)
                {
                    throw new FileNotFoundException($"Document {fileName} not found in container {containerName}");
                }

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document URL for {FileName} in container {ContainerName}", fileName, containerName);
                throw;
            }
        }

        // === PRIVATE HELPER METHODS ===

        private async Task<BlobContainerClient> GetOrCreateContainerAsync(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName.ToLower());

            try
            {
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
                return containerClient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating or accessing container {ContainerName}", containerName);
                throw;
            }
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".txt" => "text/plain",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}