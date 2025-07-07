using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartKnowledgeBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QueryAnalytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalQueries = table.Column<int>(type: "int", nullable: false),
                    SuccessfulQueries = table.Column<int>(type: "int", nullable: false),
                    AverageResponseTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AverageConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    UniqueUsers = table.Column<int>(type: "int", nullable: false),
                    TopQueries = table.Column<string>(type: "nvarchar(max)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryAnalytics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProfilePictureUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BlobUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccessLevel = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Keywords = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SearchVector = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Users_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeQueries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Query = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsAnswered = table.Column<bool>(type: "bit", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: true),
                    AnswerSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: false),
                    IsFromStructuredData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeQueries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StructuredKnowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccessLevel = table.Column<int>(type: "int", nullable: false),
                    Keywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructuredKnowledge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StructuredKnowledge_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentEmbeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TextChunk = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    StartPosition = table.Column<int>(type: "int", nullable: false),
                    EndPosition = table.Column<int>(type: "int", nullable: false),
                    EmbeddingVector = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentEmbeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentEmbeddings_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    IsFromStructuredData = table.Column<bool>(type: "bit", nullable: false),
                    ResponseTime = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModelUsed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TokensUsed = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeResponses_KnowledgeQueries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "KnowledgeQueries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QueryDocumentReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelevanceScore = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    WasUsedInResponse = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryDocumentReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueryDocumentReferences_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueryDocumentReferences_KnowledgeQueries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "KnowledgeQueries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QueryFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FeedbackType = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueryFeedbacks_KnowledgeQueries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "KnowledgeQueries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueryFeedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResponseDocumentReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelevanceScore = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    CitedText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseDocumentReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseDocumentReferences_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResponseDocumentReferences_KnowledgeResponses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "KnowledgeResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Key", "Category", "Description", "IsEncrypted", "LastModified", "ModifiedBy", "Value" },
                values: new object[,]
                {
                    { "DefaultTokenExpirationMinutes", "Authentication", "Default JWT token expiration time in minutes", false, new DateTime(2025, 7, 7, 3, 41, 40, 214, DateTimeKind.Utc).AddTicks(8947), "275b575e-1073-4240-b0e7-6ceb2ba0ca9d", "60" },
                    { "EnableAnalytics", "Analytics", "Enable query analytics collection", false, new DateTime(2025, 7, 7, 3, 41, 40, 214, DateTimeKind.Utc).AddTicks(8955), "275b575e-1073-4240-b0e7-6ceb2ba0ca9d", "true" },
                    { "MaxDocumentSizeMB", "Document", "Maximum document upload size in MB", false, new DateTime(2025, 7, 7, 3, 41, 40, 214, DateTimeKind.Utc).AddTicks(8941), "275b575e-1073-4240-b0e7-6ceb2ba0ca9d", "50" },
                    { "MaxQueryLength", "Query", "Maximum length for knowledge queries", false, new DateTime(2025, 7, 7, 3, 41, 40, 214, DateTimeKind.Utc).AddTicks(8953), "275b575e-1073-4240-b0e7-6ceb2ba0ca9d", "2000" },
                    { "RefreshTokenExpirationDays", "Authentication", "Refresh token expiration time in days", false, new DateTime(2025, 7, 7, 3, 41, 40, 214, DateTimeKind.Utc).AddTicks(8951), "275b575e-1073-4240-b0e7-6ceb2ba0ca9d", "30" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Department", "Email", "FirstName", "IsActive", "LastLoginAt", "LastName", "PasswordHash", "ProfilePictureUrl", "Role" },
                values: new object[] { "275b575e-1073-4240-b0e7-6ceb2ba0ca9d", new DateTime(2025, 7, 7, 3, 41, 40, 214, DateTimeKind.Utc).AddTicks(8177), "IT", "admin@company.com", "System", true, null, "Administrator", "$2a$11$dummy.hash.for.initial.setup", null, "Admin" });

            migrationBuilder.InsertData(
                table: "StructuredKnowledge",
                columns: new[] { "Id", "AccessLevel", "Answer", "Category", "CreatedAt", "CreatedBy", "IsActive", "Keywords", "LastModified", "Priority", "Question", "Source" },
                values: new object[,]
                {
                    { new Guid("770b483e-1414-4769-b7f5-747da75849d2"), 0, "Expense reports should be submitted through the company portal within 30 days of the expense. Include all receipts and detailed descriptions of business purposes.", "Finance", new DateTime(2025, 7, 7, 3, 41, 40, 214, DateTimeKind.Utc).AddTicks(9133), "275b575e-1073-4240-b0e7-6ceb2ba0ca9d", true, "expense, report, receipt, reimbursement, finance", null, 7, "How do I submit an expense report?", "Finance Procedures Guide" },
                    { new Guid("e7cc47dd-94ca-4eb3-8055-a4136f53ce32"), 0, "Employees accrue vacation time based on length of service. New employees start with 2 weeks annually, increasing to 3 weeks after 3 years and 4 weeks after 7 years.", "HR", new DateTime(2025, 7, 7, 3, 41, 40, 214, DateTimeKind.Utc).AddTicks(9127), "275b575e-1073-4240-b0e7-6ceb2ba0ca9d", true, "vacation, PTO, time off, leave, policy", null, 8, "What are the company's vacation policies?", "Employee Handbook 2024" },
                    { new Guid("f83173a4-da30-4745-9b94-525da0a72add"), 0, "To reset your password, go to the login page and click 'Forgot Password'. Enter your email address and follow the instructions sent to your email.", "IT Support", new DateTime(2025, 7, 7, 3, 41, 40, 214, DateTimeKind.Utc).AddTicks(9098), "275b575e-1073-4240-b0e7-6ceb2ba0ca9d", true, "password, reset, forgot, login, email", null, 10, "How do I reset my password?", "IT Policy Manual v2.1" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentEmbeddings_DocumentId",
                table: "DocumentEmbeddings",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploadedBy",
                table: "Documents",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQueries_Query_FullText",
                table: "KnowledgeQueries",
                column: "Query");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQueries_Timestamp",
                table: "KnowledgeQueries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQueries_UserId_Timestamp",
                table: "KnowledgeQueries",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQueries_UserRole",
                table: "KnowledgeQueries",
                column: "UserRole");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeResponses_GeneratedAt",
                table: "KnowledgeResponses",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeResponses_QueryId",
                table: "KnowledgeResponses",
                column: "QueryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueryAnalytics_Date",
                table: "QueryAnalytics",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_QueryAnalytics_Date_Category",
                table: "QueryAnalytics",
                columns: new[] { "Date", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_QueryAnalytics_Date_Department",
                table: "QueryAnalytics",
                columns: new[] { "Date", "Department" });

            migrationBuilder.CreateIndex(
                name: "IX_QueryDocumentReferences_DocumentId",
                table: "QueryDocumentReferences",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryDocumentReferences_QueryId_DocumentId",
                table: "QueryDocumentReferences",
                columns: new[] { "QueryId", "DocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueryFeedbacks_CreatedAt",
                table: "QueryFeedbacks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QueryFeedbacks_FeedbackType",
                table: "QueryFeedbacks",
                column: "FeedbackType");

            migrationBuilder.CreateIndex(
                name: "IX_QueryFeedbacks_QueryId_UserId",
                table: "QueryFeedbacks",
                columns: new[] { "QueryId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueryFeedbacks_UserId",
                table: "QueryFeedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponseDocumentReferences_DocumentId",
                table: "ResponseDocumentReferences",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseDocumentReferences_ResponseId_DocumentId",
                table: "ResponseDocumentReferences",
                columns: new[] { "ResponseId", "DocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StructuredKnowledge_CreatedBy",
                table: "StructuredKnowledge",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Category",
                table: "SystemSettings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_Department",
                table: "Users",
                columns: new[] { "Role", "Department" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentEmbeddings");

            migrationBuilder.DropTable(
                name: "QueryAnalytics");

            migrationBuilder.DropTable(
                name: "QueryDocumentReferences");

            migrationBuilder.DropTable(
                name: "QueryFeedbacks");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ResponseDocumentReferences");

            migrationBuilder.DropTable(
                name: "StructuredKnowledge");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "KnowledgeResponses");

            migrationBuilder.DropTable(
                name: "KnowledgeQueries");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
