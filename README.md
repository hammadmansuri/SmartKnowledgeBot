# Smart Enterprise Knowledge Bot

> AI-powered enterprise knowledge management system using Azure OpenAI and RAG (Retrieval-Augmented Generation) pattern for intelligent document processing and query responses.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Azure OpenAI](https://img.shields.io/badge/Azure-OpenAI-orange.svg)](https://azure.microsoft.com/en-us/products/cognitive-services/openai-service)
[![Entity Framework](https://img.shields.io/badge/EF-Core-purple.svg)](https://docs.microsoft.com/en-us/ef/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

## 🚀 Overview

The Smart Enterprise Knowledge Bot streamlines organizational knowledge access by enabling employees to query company documents and policies through natural language. The system combines structured knowledge bases with AI-powered document retrieval to provide accurate, contextually relevant responses while maintaining role-based security.

### Key Features

- 🤖 **AI-Powered RAG**: Hybrid retrieval using structured data and document embeddings
- 🔐 **Role-Based Security**: JWT authentication with department-level access control
- 📄 **Multi-Format Support**: PDF, DOCX, TXT, PPTX, XLSX document processing
- ⚡ **Real-Time Processing**: Asynchronous document indexing and query responses
- 📊 **Analytics & Monitoring**: Query tracking and performance insights
- 🎯 **Enterprise-Grade**: Scalable architecture with comprehensive error handling

## 🏗️ Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Frontend UI   │────│   API Gateway    │────│  Azure OpenAI   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                               │
                       ┌───────┴────────┐
                       │ Business Logic │
                       └───────┬────────┘
                               │
        ┌──────────────────────┼──────────────────────┐
        │                      │                      │
┌───────▼────────┐    ┌────────▼─────────┐    ┌────── ▼──────┐
│  SQL Database  │    │  Document Store  │    │    Blob      │
│  (Structured)  │    │   (Embeddings)   │    │   Storage    │
└────────────────┘    └──────────────────┘    └──-───────────┘
```

### Technology Stack

- **Backend**: .NET 8 Web API, Entity Framework Core
- **Database**: SQL Server with full-text indexing
- **AI Services**: Azure OpenAI (GPT-4, text-embedding-ada-002)
- **Storage**: Azure Blob Storage for documents
- **Authentication**: JWT with refresh tokens
- **Testing**: xUnit, Moq, FluentAssertions

## 📁 Project Structure

```
SmartEnterpriseKnowledgeBot/
├── src/
│   ├── SmartKnowledgeBot.API/              # Web API controllers and startup
│   ├── SmartKnowledgeBot.Business/         # Business logic and services
│   ├── SmartKnowledgeBot.Domain/           # Entities, DTOs, and enums
│   ├── SmartKnowledgeBot.Infrastructure/   # Data access and external services
│   └── SmartKnowledgeBot.Common/           # Shared utilities
├── tests/
│   ├── SmartKnowledgeBot.UnitTests/        # Unit tests
│   └── SmartKnowledgeBot.IntegrationTests/ # Integration tests
├── docs/                                   # Documentation
├── scripts/                                # Database scripts
└── .github/workflows/                      # CI/CD pipelines
```

## 🛠️ Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-editions-express)
- [Azure OpenAI Service](https://azure.microsoft.com/en-us/products/cognitive-services/openai-service) subscription
- [Azure Storage Account](https://docs.microsoft.com/en-us/azure/storage/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/smart-enterprise-knowledge-bot.git
   cd smart-enterprise-knowledge-bot
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure settings**
   
   Update `appsettings.json` in the API project:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=SmartKnowledgeBotDb;Trusted_Connection=true;",
       "AzureBlobStorage": "your-blob-storage-connection-string"
     },
     "AzureOpenAI": {
       "Endpoint": "https://your-openai-resource.openai.azure.com/",
       "ApiKey": "your-api-key",
       "ChatDeploymentName": "gpt-4",
       "EmbeddingDeploymentName": "text-embedding-ada-002"
     },
     "JwtSettings": {
       "SecretKey": "your-256-bit-secret-key",
       "Issuer": "SmartKnowledgeBot",
       "Audience": "Enterprise"
     }
   }
   ```

4. **Set up the database**
   ```bash
   dotnet ef database update --project src/SmartKnowledgeBot.Infrastructure --startup-project src/SmartKnowledgeBot.API
   ```

5. **Run the application**
   ```bash
   dotnet run --project src/SmartKnowledgeBot.API
   ```

6. **Access the API**
   - Swagger UI: `https://localhost:7001`
   - Health Check: `https://localhost:7001/health`

### Environment Variables (Production)

For production deployment, use environment variables:

```bash
export ConnectionStrings__DefaultConnection="your-production-db-connection"
export AzureOpenAI__ApiKey="your-production-api-key"
export JwtSettings__SecretKey="your-production-secret-key"
```

## 📚 API Documentation

### Authentication

All endpoints (except `/auth/login`) require JWT bearer token authentication.

#### Use HTTP for the demo to avoid certificate issues

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@company.com",
  "password": "password"
}
```

#### Response
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenExpiration": "2024-01-01T12:00:00Z",
  "refreshToken": "refresh-token-string",
  "user": {
    "id": "user-id",
    "email": "user@company.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "Employee",
    "department": "IT"
  }
}
```

### Knowledge Queries

#### Submit Query
```http
POST /api/knowledge/query
Authorization: Bearer {token}
Content-Type: application/json

{
  "query": "What is the company's vacation policy?",
  "sessionId": "optional-session-id"
}
```

#### Response
```json
{
  "answer": "Employees accrue vacation time based on length of service...",
  "source": "HR Policy Manual",
  "confidenceScore": 0.95,
  "isFromStructuredData": true,
  "relevantDocuments": [
    {
      "documentId": "doc-id",
      "title": "Employee Handbook 2024",
      "category": "HR",
      "relevanceScore": 0.92
    }
  ],
  "responseTime": 150,
  "queryId": "query-id"
}
```

### Document Management

#### Upload Document
```http
POST /api/documents/upload
Authorization: Bearer {token}
Content-Type: multipart/form-data

{
  "file": [binary-file],
  "category": "HR",
  "accessLevel": "General",
  "description": "Updated employee handbook"
}
```

#### Get Documents
```http
GET /api/documents?category=HR&page=1&pageSize=20
Authorization: Bearer {token}
```

## 🧪 Testing

### Run Unit Tests
```bash
dotnet test tests/SmartKnowledgeBot.UnitTests/
```

### Run Integration Tests
```bash
dotnet test tests/SmartKnowledgeBot.IntegrationTests/
```

### Test Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 🔧 Development

### Database Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName --project src/SmartKnowledgeBot.Infrastructure --startup-project src/SmartKnowledgeBot.API
```

Apply migrations:
```bash
dotnet ef database update --project src/SmartKnowledgeBot.Infrastructure --startup-project src/SmartKnowledgeBot.API
```

### Adding New Features

1. **Domain Models**: Add entities to `SmartKnowledgeBot.Domain/Models/`
2. **Business Logic**: Implement services in `SmartKnowledgeBot.Business/Services/`
3. **API Endpoints**: Add controllers to `SmartKnowledgeBot.API/Controllers/`
4. **Tests**: Write tests in `SmartKnowledgeBot.UnitTests/`

### Code Quality

- **StyleCop**: Enforces C# coding conventions
- **EditorConfig**: Consistent code formatting
- **SonarAnalyzer**: Code quality and security analysis

## 🚀 Deployment

### Docker Deployment

Build and run with Docker:
```bash
docker build -t smart-knowledge-bot .
docker run -p 8080:80 smart-knowledge-bot
```

### Azure Deployment

Deploy to Azure App Service:
```bash
az webapp create --resource-group myResourceGroup --plan myAppServicePlan --name myKnowledgeBot --runtime "DOTNETCORE:8.0"
az webapp deployment source config --name myKnowledgeBot --resource-group myResourceGroup --repo-url https://github.com/yourusername/smart-enterprise-knowledge-bot --branch main
```

## 📊 Monitoring & Analytics

### Health Checks
- `/health` - Basic health status
- `/health/detailed` - Detailed component health with metrics

### Application Insights
The application integrates with Azure Application Insights for:
- Performance monitoring
- Error tracking
- Usage analytics
- Custom telemetry

### Logging
Structured logging with:
- **Information**: Normal operations
- **Warning**: Potential issues
- **Error**: Exceptions and failures
- **Debug**: Development troubleshooting

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Coding Standards
- Follow C# coding conventions
- Write unit tests for new features
- Update documentation for API changes
- Ensure all tests pass before submitting PR

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation**: Check the [docs/](docs/) folder
- **Issues**: Report bugs via [GitHub Issues](https://github.com/yourusername/smart-enterprise-knowledge-bot/issues)
- **Discussions**: Use [GitHub Discussions](https://github.com/yourusername/smart-enterprise-knowledge-bot/discussions)

## 🙏 Acknowledgments

- [Azure OpenAI Service](https://azure.microsoft.com/en-us/products/cognitive-services/openai-service)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/)
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [.NET Community](https://dotnet.foundation/)

---

**Made with ❤️ for Enterprise Knowledge Management**