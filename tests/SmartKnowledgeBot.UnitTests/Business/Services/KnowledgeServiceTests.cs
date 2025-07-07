using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartKnowledgeBot.Business.Services;
using SmartKnowledgeBot.Domain.Models;
using SmartKnowledgeBot.Domain.Enums;
using SmartKnowledgeBot.Infrastructure.Data;
using Xunit;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using SmartKnowledgeBot.Business.Interfaces;
using SmartKnowledgeBot.Domain.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartKnowledgeBot.UnitTests.Business.Services
{
    /// <summary>
    /// Unit tests for KnowledgeService
    /// </summary>
    public class KnowledgeServiceTests : IDisposable
    {
        private readonly SmartKnowledgeBotDbContext _context;
        private readonly Mock<IAzureOpenAIService> _mockAIService;
        private readonly Mock<IBlobStorageService> _mockBlobService;
        private readonly Mock<ILogger<KnowledgeService>> _mockLogger;
        private readonly KnowledgeService _knowledgeService;

        public KnowledgeServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<SmartKnowledgeBotDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SmartKnowledgeBotDbContext(options);
            _mockAIService = new Mock<IAzureOpenAIService>();
            _mockBlobService = new Mock<IBlobStorageService>();
            _mockLogger = new Mock<ILogger<KnowledgeService>>();

            _knowledgeService = new KnowledgeService(
                _context,
                _mockAIService.Object,
                _mockBlobService.Object,
                _mockLogger.Object);

            SeedTestData();
        }

        [Fact]
        public async Task ProcessQueryAsync_WithStructuredKnowledgeMatch_ShouldReturnStructuredResponse()
        {
            // Arrange
            var query = new KnowledgeQuery
            {
                Id = Guid.NewGuid(),
                Query = "How do I reset my password?",
                UserId = "user1",
                UserRole = "Employee",
                Department = "General",
                Timestamp = DateTime.UtcNow
            };

            // Act
            var result = await _knowledgeService.ProcessQueryAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsFromStructuredData.Should().BeTrue();
            result.ConfidenceScore.Should().Be(0.95);
            result.Answer.Should().Contain("reset your password");
            result.Source.Should().Be("IT Policy Manual v2.1");
        }

        [Fact]
        public async Task ProcessQueryAsync_WithNoStructuredMatch_ShouldUseAI()
        {
            // Arrange
            var query = new KnowledgeQuery
            {
                Id = Guid.NewGuid(),
                Query = "What is the company's approach to remote work?",
                UserId = "user1",
                UserRole = "Employee",
                Department = "General",
                Timestamp = DateTime.UtcNow
            };

            var mockEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
            var mockAIResponse = "Based on company policy, remote work is supported...";

            _mockAIService.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>()))
                .ReturnsAsync(mockEmbedding);

            _mockAIService.Setup(x => x.GenerateResponseAsync(
                It.IsAny<string>(),
                It.IsAny<List<string>>(),
                It.IsAny<string>()))
                .ReturnsAsync(mockAIResponse);

            // Act
            var result = await _knowledgeService.ProcessQueryAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsFromStructuredData.Should().BeFalse();
            result.Answer.Should().Be(mockAIResponse);
            result.Source.Should().Be("AI + Documents");

            _mockAIService.Verify(x => x.GenerateEmbeddingAsync(query.Query), Times.Once);
        }

        [Fact]
        public async Task GetUserQueryHistoryAsync_ShouldReturnPaginatedResults()
        {
            // Arrange
            var userId = "user1";
            await SeedQueryHistory(userId, 15); // Create 15 queries

            // Act
            var result = await _knowledgeService.GetUserQueryHistoryAsync(userId, page: 1, pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(10);
            result.TotalItems.Should().Be(15);
            result.TotalPages.Should().Be(2);
            result.HasNextPage.Should().BeTrue();
            result.HasPreviousPage.Should().BeFalse();
        }

        [Fact]
        public async Task RecordFeedbackAsync_ShouldCreateNewFeedback()
        {
            // Arrange
            var queryId = Guid.NewGuid();
            var userId = "user1";

            var query = new KnowledgeQuery
            {
                Id = queryId,
                UserId = userId,
                Query = "Test query",
                UserRole = "Employee",
                Department = "General",
                Timestamp = DateTime.UtcNow
            };

            _context.KnowledgeQueries.Add(query);
            await _context.SaveChangesAsync();

            var feedbackDto = new QueryFeedbackDto
            {
                FeedbackType = FeedbackType.Helpful,
                Rating = 5,
                Comments = "Very helpful response"
            };

            // Act
            await _knowledgeService.RecordFeedbackAsync(queryId, userId, feedbackDto);

            // Assert
            var feedback = await _context.QueryFeedbacks
                .FirstOrDefaultAsync(f => f.QueryId == queryId && f.UserId == userId);

            feedback.Should().NotBeNull();
            feedback!.FeedbackType.Should().Be(FeedbackType.Helpful);
            feedback.Rating.Should().Be(5);
            feedback.Comments.Should().Be("Very helpful response");
        }

        [Fact]
        public async Task GetQuerySuggestionsAsync_ShouldReturnRoleBasedSuggestions()
        {
            // Arrange
            var role = "Employee";
            var department = "General";

            // Act
            var suggestions = await _knowledgeService.GetQuerySuggestionsAsync(role, department);

            // Assert
            suggestions.Should().NotBeEmpty();
            suggestions.Should().AllSatisfy(s => s.Category.Should().NotBeNullOrEmpty());
            suggestions.Should().AllSatisfy(s => s.Suggestion.Should().NotBeNullOrEmpty());
        }

        private void SeedTestData()
        {
            // Seed users
            var user = new User
            {
                Id = "user1",
                Email = "test@company.com",
                FirstName = "Test",
                LastName = "User",
                Role = "Employee",
                Department = "General",
                PasswordHash = "hashed_password",
                IsActive = true
            };

            _context.Users.Add(user);

            // Seed structured knowledge
            var structuredKnowledge = new StructuredKnowledge
            {
                Id = Guid.NewGuid(),
                Question = "How do I reset my password?",
                Answer = "To reset your password, go to the login page and click 'Forgot Password'. Enter your email address and follow the instructions sent to your email.",
                Category = "IT Support",
                AccessLevel = AccessLevel.General,
                Keywords = "password, reset, forgot, login, email",
                Priority = 10,
                CreatedBy = "admin",
                Source = "IT Policy Manual v2.1",
                IsActive = true
            };

            _context.StructuredKnowledge.Add(structuredKnowledge);
            _context.SaveChanges();
        }

        private async Task SeedQueryHistory(string userId, int count)
        {
            var queries = new List<KnowledgeQuery>();

            for (int i = 0; i < count; i++)
            {
                queries.Add(new KnowledgeQuery
                {
                    Id = Guid.NewGuid(),
                    Query = $"Test query {i + 1}",
                    UserId = userId,
                    UserRole = "Employee",
                    Department = "General",
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    IsAnswered = true,
                    ConfidenceScore = 0.8,
                    AnswerSource = "SQL",
                    ResponseTimeMs = 150
                });
            }

            _context.KnowledgeQueries.AddRange(queries);
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

namespace SmartKnowledgeBot.UnitTests.Business.Services
{
    /// <summary>
    /// Unit tests for AuthService
    /// </summary>
    public class AuthServiceTests : IDisposable
    {
        private readonly SmartKnowledgeBotDbContext _context;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<SmartKnowledgeBotDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SmartKnowledgeBotDbContext(options);
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AuthService>>();

            SetupConfiguration();

            _authService = new AuthService(_context, _mockConfiguration.Object, _mockLogger.Object);

            SeedTestData();
        }

        [Fact]
        public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnAuthResult()
        {
            // Arrange
            var email = "test@company.com";
            var password = "TestPassword123!";

            // Act
            var result = await _authService.AuthenticateAsync(email, password);

            // Assert
            result.Should().NotBeNull();
            result!.Token.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();
            result.User.Email.Should().Be(email);
            result.TokenExpiration.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task AuthenticateAsync_WithInvalidCredentials_ShouldReturnNull()
        {
            // Arrange
            var email = "test@company.com";
            var wrongPassword = "WrongPassword";

            // Act
            var result = await _authService.AuthenticateAsync(email, wrongPassword);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AuthenticateAsync_WithNonExistentUser_ShouldReturnNull()
        {
            // Arrange
            var nonExistentEmail = "nonexistent@company.com";
            var password = "TestPassword123!";

            // Act
            var result = await _authService.AuthenticateAsync(nonExistentEmail, password);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserProfileAsync_WithValidUserId_ShouldReturnProfile()
        {
            // Arrange
            var userId = "user1";

            // Act
            var profile = await _authService.GetUserProfileAsync(userId);

            // Assert
            profile.Should().NotBeNull();
            profile!.Id.Should().Be(userId);
            profile.Email.Should().Be("test@company.com");
            profile.FullName.Should().Be("Test User");
        }

        [Fact]
        public async Task UpdateUserProfileAsync_WithValidData_ShouldUpdateProfile()
        {
            // Arrange
            var userId = "user1";
            var updateRequest = new UserProfileUpdateDto
            {
                FirstName = "Updated",
                LastName = "Name",
                Department = "IT"
            };

            // Act
            var result = await _authService.UpdateUserProfileAsync(userId, updateRequest);

            // Assert
            result.Should().BeTrue();

            var updatedUser = await _context.Users.FindAsync(userId);
            updatedUser!.FirstName.Should().Be("Updated");
            updatedUser.LastName.Should().Be("Name");
            updatedUser.Department.Should().Be("IT");
        }

        private void SetupConfiguration()
        {
            _mockConfiguration.Setup(c => c["JwtSettings:SecretKey"])
                .Returns("this-is-a-very-secure-secret-key-for-testing-purposes-only");
            _mockConfiguration.Setup(c => c["JwtSettings:Issuer"])
                .Returns("SmartKnowledgeBot");
            _mockConfiguration.Setup(c => c["JwtSettings:Audience"])
                .Returns("Enterprise");
            _mockConfiguration.Setup(c => c["JwtSettings:TokenExpirationMinutes"])
                .Returns("60");
            _mockConfiguration.Setup(c => c["JwtSettings:RefreshTokenExpirationDays"])
                .Returns("30");
        }

        private void SeedTestData()
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!");

            var user = new User
            {
                Id = "user1",
                Email = "test@company.com",
                FirstName = "Test",
                LastName = "User",
                Role = "Employee",
                Department = "General",
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

namespace SmartKnowledgeBot.UnitTests.API.Controllers
{
    /// <summary>
    /// Integration tests for KnowledgeController
    /// </summary>
    public class KnowledgeControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public KnowledgeControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task ProcessQuery_WithoutAuth_ShouldReturn401()
        {
            // Arrange
            var queryDto = new KnowledgeQueryDto
            {
                Query = "How do I reset my password?"
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(queryDto),
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/knowledge/query", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Health_ShouldReturnHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("Healthy");
        }
    }
}