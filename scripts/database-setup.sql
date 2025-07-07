-- Initial Database Setup Script for Smart Knowledge Bot
-- Run this script to set up the database manually if EF migrations are not used

-- Create database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SmartKnowledgeBotDb')
BEGIN
    CREATE DATABASE SmartKnowledgeBotDb;
END
GO

USE SmartKnowledgeBotDb;
GO

-- Create Users table
CREATE TABLE Users (
    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    Department NVARCHAR(100) NOT NULL DEFAULT '',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginAt DATETIME2 NULL,
    ProfilePictureUrl NVARCHAR(500) NULL
);

-- Create KnowledgeQueries table
CREATE TABLE KnowledgeQueries (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Query NVARCHAR(2000) NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    UserRole NVARCHAR(50) NOT NULL,
    Department NVARCHAR(100) NOT NULL DEFAULT '',
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    SessionId NVARCHAR(100) NULL,
    IsAnswered BIT NOT NULL DEFAULT 0,
    ConfidenceScore FLOAT NULL,
    AnswerSource NVARCHAR(50) NULL,
    ResponseTimeMs INT NOT NULL DEFAULT 0,
    IsFromStructuredData BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Create KnowledgeResponses table
CREATE TABLE KnowledgeResponses (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    QueryId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    Answer NVARCHAR(MAX) NOT NULL,
    Source NVARCHAR(200) NOT NULL DEFAULT '',
    ConfidenceScore DECIMAL(5,4) NOT NULL DEFAULT 0,
    IsFromStructuredData BIT NOT NULL DEFAULT 0,
    ResponseTime INT NOT NULL DEFAULT 0,
    GeneratedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModelUsed NVARCHAR(100) NULL,
    TokensUsed INT NULL,
    FOREIGN KEY (QueryId) REFERENCES KnowledgeQueries(Id) ON DELETE CASCADE
);

-- Create Documents table
CREATE TABLE Documents (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    FileName NVARCHAR(255) NOT NULL,
    BlobUrl NVARCHAR(500) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    AccessLevel INT NOT NULL DEFAULT 0, -- AccessLevel enum
    Description NVARCHAR(1000) NULL,
    FileSizeBytes BIGINT NOT NULL,
    FileType NVARCHAR(10) NOT NULL,
    UploadedBy NVARCHAR(450) NOT NULL,
    UploadedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModified DATETIME2 NULL,
    Status INT NOT NULL DEFAULT 0, -- DocumentStatus enum
    ProcessedAt DATETIME2 NULL,
    ExtractedText NVARCHAR(MAX) NULL,
    ContentSummary NVARCHAR(MAX) NULL,
    Keywords NVARCHAR(MAX) NULL,
    SearchVector NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (UploadedBy) REFERENCES Users(Id)
);

-- Create DocumentEmbeddings table
CREATE TABLE DocumentEmbeddings (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    DocumentId UNIQUEIDENTIFIER NOT NULL,
    TextChunk NVARCHAR(MAX) NOT NULL,
    ChunkIndex INT NOT NULL,
    StartPosition INT NOT NULL,
    EndPosition INT NOT NULL,
    EmbeddingVector NVARCHAR(MAX) NOT NULL,
    EmbeddingModel NVARCHAR(50) NOT NULL DEFAULT '',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE
);

-- Create StructuredKnowledge table
CREATE TABLE StructuredKnowledge (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Question NVARCHAR(500) NOT NULL,
    Answer NVARCHAR(MAX) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    AccessLevel INT NOT NULL DEFAULT 0,
    Keywords NVARCHAR(500) NOT NULL DEFAULT '',
    Priority INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedBy NVARCHAR(450) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModified DATETIME2 NULL,
    Source NVARCHAR(500) NULL,
    FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
);

-- Create RefreshTokens table
CREATE TABLE RefreshTokens (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId NVARCHAR(450) NOT NULL,
    Token NVARCHAR(500) NOT NULL UNIQUE,
    ExpiresAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RevokedAt DATETIME2 NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Create QueryFeedbacks table
CREATE TABLE QueryFeedbacks (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    QueryId UNIQUEIDENTIFIER NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    FeedbackType INT NOT NULL DEFAULT 0, -- FeedbackType enum
    Rating INT NOT NULL DEFAULT 3,
    Comments NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (QueryId) REFERENCES KnowledgeQueries(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    UNIQUE(QueryId, UserId)
);

-- Create QueryDocumentReferences table
CREATE TABLE QueryDocumentReferences (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    QueryId UNIQUEIDENTIFIER NOT NULL,
    DocumentId UNIQUEIDENTIFIER NOT NULL,
    RelevanceScore DECIMAL(5,4) NOT NULL DEFAULT 0,
    WasUsedInResponse BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (QueryId) REFERENCES KnowledgeQueries(Id) ON DELETE CASCADE,
    FOREIGN KEY (DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE,
    UNIQUE(QueryId, DocumentId)
);

-- Create ResponseDocumentReferences table
CREATE TABLE ResponseDocumentReferences (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    ResponseId UNIQUEIDENTIFIER NOT NULL,
    DocumentId UNIQUEIDENTIFIER NOT NULL,
    RelevanceScore DECIMAL(5,4) NOT NULL DEFAULT 0,
    CitedText NVARCHAR(MAX) NULL,
    FOREIGN KEY (ResponseId) REFERENCES KnowledgeResponses(Id) ON DELETE CASCADE,
    FOREIGN KEY (DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE,
    UNIQUE(ResponseId, DocumentId)
);

-- Create QueryAnalytics table
CREATE TABLE QueryAnalytics (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Date DATE NOT NULL,
    Category NVARCHAR(100) NOT NULL DEFAULT '',
    Department NVARCHAR(50) NOT NULL DEFAULT '',
    TotalQueries INT NOT NULL DEFAULT 0,
    SuccessfulQueries INT NOT NULL DEFAULT 0,
    AverageResponseTime DECIMAL(10,2) NOT NULL DEFAULT 0,
    AverageConfidenceScore DECIMAL(5,4) NOT NULL DEFAULT 0,
    UniqueUsers INT NOT NULL DEFAULT 0,
    TopQueries NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Create SystemSettings table
CREATE TABLE SystemSettings (
    [Key] NVARCHAR(100) NOT NULL PRIMARY KEY,
    Value NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(500) NULL,
    Category NVARCHAR(50) NOT NULL DEFAULT 'General',
    IsEncrypted BIT NOT NULL DEFAULT 0,
    LastModified DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NOT NULL DEFAULT 'System'
);

-- Create indexes for performance
-- Users indexes
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Role_Department ON Users(Role, Department);

-- KnowledgeQueries indexes
CREATE INDEX IX_KnowledgeQueries_Timestamp ON KnowledgeQueries(Timestamp);
CREATE INDEX IX_KnowledgeQueries_UserId_Timestamp ON KnowledgeQueries(UserId, Timestamp);
CREATE INDEX IX_KnowledgeQueries_UserRole ON KnowledgeQueries(UserRole);

-- KnowledgeResponses indexes
CREATE INDEX IX_KnowledgeResponses_GeneratedAt ON KnowledgeResponses(GeneratedAt);

-- Documents indexes
CREATE INDEX IX_Documents_Category ON Documents(Category);
CREATE INDEX IX_Documents_AccessLevel ON Documents(AccessLevel);
CREATE INDEX IX_Documents_Status_IsActive ON Documents(Status, IsActive);
CREATE INDEX IX_Documents_UploadedAt ON Documents(UploadedAt);

-- DocumentEmbeddings indexes
CREATE INDEX IX_DocumentEmbeddings_DocumentId ON DocumentEmbeddings(DocumentId);
CREATE INDEX IX_DocumentEmbeddings_DocumentId_ChunkIndex ON DocumentEmbeddings(DocumentId, ChunkIndex);

-- StructuredKnowledge indexes
CREATE INDEX IX_StructuredKnowledge_Category ON StructuredKnowledge(Category);
CREATE INDEX IX_StructuredKnowledge_AccessLevel ON StructuredKnowledge(AccessLevel);
CREATE INDEX IX_StructuredKnowledge_IsActive_Priority ON StructuredKnowledge(IsActive, Priority);

-- RefreshTokens indexes
CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
CREATE INDEX IX_RefreshTokens_UserId_ExpiresAt ON RefreshTokens(UserId, ExpiresAt);
CREATE INDEX IX_RefreshTokens_ExpiresAt ON RefreshTokens(ExpiresAt);

-- QueryFeedbacks indexes
CREATE INDEX IX_QueryFeedbacks_FeedbackType ON QueryFeedbacks(FeedbackType);
CREATE INDEX IX_QueryFeedbacks_CreatedAt ON QueryFeedbacks(CreatedAt);

-- QueryAnalytics indexes
CREATE INDEX IX_QueryAnalytics_Date ON QueryAnalytics(Date);
CREATE INDEX IX_QueryAnalytics_Date_Category ON QueryAnalytics(Date, Category);
CREATE INDEX IX_QueryAnalytics_Date_Department ON QueryAnalytics(Date, Department);

-- SystemSettings indexes
CREATE INDEX IX_SystemSettings_Category ON SystemSettings(Category);

-- Insert initial data
-- Default admin user (password: Admin123!)
INSERT INTO Users (Id, Email, FirstName, LastName, PasswordHash, Role, Department, IsActive, CreatedAt)
VALUES (
    NEWID(),
    'admin@company.com',
    'System',
    'Administrator',
    '$2a$11$dummy.hash.for.initial.setup', -- Replace with proper BCrypt hash
    'Admin',
    'IT',
    1,
    GETUTCDATE()
);

-- System settings
INSERT INTO SystemSettings ([Key], Value, Description, Category, ModifiedBy)
VALUES 
    ('MaxDocumentSizeMB', '50', 'Maximum document upload size in MB', 'Document', 'System'),
    ('DefaultTokenExpirationMinutes', '60', 'Default JWT token expiration time in minutes', 'Authentication', 'System'),
    ('RefreshTokenExpirationDays', '30', 'Refresh token expiration time in days', 'Authentication', 'System'),
    ('MaxQueryLength', '2000', 'Maximum length for knowledge queries', 'Query', 'System'),
    ('EnableAnalytics', 'true', 'Enable query analytics collection', 'Analytics', 'System');

-- Sample structured knowledge
DECLARE @AdminUserId NVARCHAR(450) = (SELECT Id FROM Users WHERE Email = 'admin@company.com');

INSERT INTO StructuredKnowledge (Id, Question, Answer, Category, AccessLevel, Keywords, Priority, CreatedBy, Source, IsActive)
VALUES 
    (
        NEWID(),
        'How do I reset my password?',
        'To reset your password, go to the login page and click ''Forgot Password''. Enter your email address and follow the instructions sent to your email.',
        'IT Support',
        0, -- General access
        'password, reset, forgot, login, email',
        10,
        @AdminUserId,
        'IT Policy Manual v2.1',
        1
    ),
    (
        NEWID(),
        'What are the company''s vacation policies?',
        'Employees accrue vacation time based on length of service. New employees start with 2 weeks annually, increasing to 3 weeks after 3 years and 4 weeks after 7 years.',
        'HR',
        0, -- General access
        'vacation, PTO, time off, leave, policy',
        8,
        @AdminUserId,
        'Employee Handbook 2024',
        1
    ),
    (
        NEWID(),
        'How do I submit an expense report?',
        'Expense reports should be submitted through the company portal within 30 days of the expense. Include all receipts and detailed descriptions of business purposes.',
        'Finance',
        0, -- General access
        'expense, report, receipt, reimbursement, finance',
        7,
        @AdminUserId,
        'Finance Procedures Guide',
        1
    ),
    (
        NEWID(),
        'What is the remote work policy?',
        'Employees may work remotely up to 3 days per week with manager approval. Full-time remote work requires executive approval and is subject to role requirements.',
        'HR',
        0, -- General access
        'remote work, work from home, WFH, hybrid, policy',
        9,
        @AdminUserId,
        'Remote Work Guidelines 2024',
        1
    ),
    (
        NEWID(),
        'How do I access the VPN?',
        'Download the company VPN client from the IT portal. Use your domain credentials to connect. For issues, contact the IT helpdesk.',
        'IT Support',
        5, -- IT department access
        'VPN, remote access, IT, network, connection',
        8,
        @AdminUserId,
        'IT Security Manual',
        1
    );

-- Enable full-text search (if supported)
-- Note: Uncomment these if your SQL Server instance supports full-text indexing
/*
IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'SmartKnowledgeBotCatalog')
BEGIN
    CREATE FULLTEXT CATALOG SmartKnowledgeBotCatalog;
END

-- Full-text indexes for better search performance
CREATE FULLTEXT INDEX ON KnowledgeQueries(Query) KEY INDEX PK__Knowledg__3214EC077F2CE0D4;
CREATE FULLTEXT INDEX ON Documents(FileName, ExtractedText) KEY INDEX PK__Document__3214EC077F2CE0D4;
CREATE FULLTEXT INDEX ON StructuredKnowledge(Question, Keywords) KEY INDEX PK__Structur__3214EC077F2CE0D4;
*/

PRINT 'Database setup completed successfully!';
PRINT 'Default admin user created: admin@company.com';
PRINT 'Remember to update the password hash with a proper BCrypt hash before production use.';

GO