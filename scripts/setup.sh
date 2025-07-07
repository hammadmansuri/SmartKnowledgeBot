# setup.sh - Initial project setup script
#!/bin/bash

echo "🚀 Setting up Smart Enterprise Knowledge Bot..."

# Check prerequisites
echo "Checking prerequisites..."

# Check .NET 8
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET 8 SDK not found. Please install from https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version | cut -d. -f1)
if [ "$DOTNET_VERSION" -lt "8" ]; then
    echo "❌ .NET 8 or higher required. Current version: $(dotnet --version)"
    exit 1
fi

echo "✅ .NET 8 SDK found"

# Create solution structure
echo "📁 Creating project structure..."

# Create solution
dotnet new sln -n SmartKnowledgeBot

# Create projects
echo "Creating API project..."
dotnet new webapi -n SmartKnowledgeBot.API -o src/SmartKnowledgeBot.API

echo "Creating Business layer..."
dotnet new classlib -n SmartKnowledgeBot.Business -o src/SmartKnowledgeBot.Business

echo "Creating Domain layer..."
dotnet new classlib -n SmartKnowledgeBot.Domain -o src/SmartKnowledgeBot.Domain

echo "Creating Infrastructure layer..."
dotnet new classlib -n SmartKnowledgeBot.Infrastructure -o src/SmartKnowledgeBot.Infrastructure

echo "Creating Common library..."
dotnet new classlib -n SmartKnowledgeBot.Common -o src/SmartKnowledgeBot.Common

echo "Creating Unit Tests..."
dotnet new xunit -n SmartKnowledgeBot.UnitTests -o tests/SmartKnowledgeBot.UnitTests

echo "Creating Integration Tests..."
dotnet new xunit -n SmartKnowledgeBot.IntegrationTests -o tests/SmartKnowledgeBot.IntegrationTests

# Add projects to solution
echo "Adding projects to solution..."
dotnet sln add src/SmartKnowledgeBot.API/SmartKnowledgeBot.API.csproj
dotnet sln add src/SmartKnowledgeBot.Business/SmartKnowledgeBot.Business.csproj
dotnet sln add src/SmartKnowledgeBot.Domain/SmartKnowledgeBot.Domain.csproj
dotnet sln add src/SmartKnowledgeBot.Infrastructure/SmartKnowledgeBot.Infrastructure.csproj
dotnet sln add src/SmartKnowledgeBot.Common/SmartKnowledgeBot.Common.csproj
dotnet sln add tests/SmartKnowledgeBot.UnitTests/SmartKnowledgeBot.UnitTests.csproj
dotnet sln add tests/SmartKnowledgeBot.IntegrationTests/SmartKnowledgeBot.IntegrationTests.csproj

# Add project references
echo "Setting up project references..."

# API references
dotnet add src/SmartKnowledgeBot.API/SmartKnowledgeBot.API.csproj reference src/SmartKnowledgeBot.Business/SmartKnowledgeBot.Business.csproj
dotnet add src/SmartKnowledgeBot.API/SmartKnowledgeBot.API.csproj reference src/SmartKnowledgeBot.Infrastructure/SmartKnowledgeBot.Infrastructure.csproj

# Business references
dotnet add src/SmartKnowledgeBot.Business/SmartKnowledgeBot.Business.csproj reference src/SmartKnowledgeBot.Domain/SmartKnowledgeBot.Domain.csproj
dotnet add src/SmartKnowledgeBot.Business/SmartKnowledgeBot.Business.csproj reference src/SmartKnowledgeBot.Infrastructure/SmartKnowledgeBot.Infrastructure.csproj

# Infrastructure references
dotnet add src/SmartKnowledgeBot.Infrastructure/SmartKnowledgeBot.Infrastructure.csproj reference src/SmartKnowledgeBot.Domain/SmartKnowledgeBot.Domain.csproj

# Test references
dotnet add tests/SmartKnowledgeBot.UnitTests/SmartKnowledgeBot.UnitTests.csproj reference src/SmartKnowledgeBot.API/SmartKnowledgeBot.API.csproj
dotnet add tests/SmartKnowledgeBot.UnitTests/SmartKnowledgeBot.UnitTests.csproj reference src/SmartKnowledgeBot.Business/SmartKnowledgeBot.Business.csproj
dotnet add tests/SmartKnowledgeBot.UnitTests/SmartKnowledgeBot.UnitTests.csproj reference src/SmartKnowledgeBot.Domain/SmartKnowledgeBot.Domain.csproj

dotnet add tests/SmartKnowledgeBot.IntegrationTests/SmartKnowledgeBot.IntegrationTests.csproj reference src/SmartKnowledgeBot.API/SmartKnowledgeBot.API.csproj

# Install essential packages
echo "📦 Installing essential packages..."

# API packages
dotnet add src/SmartKnowledgeBot.API/SmartKnowledgeBot.API.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/SmartKnowledgeBot.API/SmartKnowledgeBot.API.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add src/SmartKnowledgeBot.API/SmartKnowledgeBot.API.csproj package Swashbuckle.AspNetCore

# Business packages
dotnet add src/SmartKnowledgeBot.Business/SmartKnowledgeBot.Business.csproj package BCrypt.Net-Next
dotnet add src/SmartKnowledgeBot.Business/SmartKnowledgeBot.Business.csproj package System.IdentityModel.Tokens.Jwt

# Infrastructure packages
dotnet add src/SmartKnowledgeBot.Infrastructure/SmartKnowledgeBot.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/SmartKnowledgeBot.Infrastructure/SmartKnowledgeBot.Infrastructure.csproj package Azure.AI.OpenAI
dotnet add src/SmartKnowledgeBot.Infrastructure/SmartKnowledgeBot.Infrastructure.csproj package Azure.Storage.Blobs

# Test packages
dotnet add tests/SmartKnowledgeBot.UnitTests/SmartKnowledgeBot.UnitTests.csproj package Moq
dotnet add tests/SmartKnowledgeBot.UnitTests/SmartKnowledgeBot.UnitTests.csproj package FluentAssertions
dotnet add tests/SmartKnowledgeBot.UnitTests/SmartKnowledgeBot.UnitTests.csproj package Microsoft.EntityFrameworkCore.InMemory

# Create directories
echo "📂 Creating additional directories..."
mkdir -p docs
mkdir -p scripts
mkdir -p .github/workflows

# Create basic files
echo "📄 Creating configuration files..."

# .gitignore
cat > .gitignore << 'EOF'
## Ignore Visual Studio temporary files, build results, and
## files generated by popular Visual Studio add-ons.

# User-specific files
*.suo
*.user
*.userosscache
*.sln.docstates

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/

# .NET Core
project.lock.json
project.fragment.lock.json
artifacts/

# ASP.NET Scaffolding
ScaffoldingReadMe.txt

# Configuration files with secrets
appsettings.*.json
!appsettings.json
!appsettings.Development.json

# Azure secrets
*.azurewebsites.net.publishsettings

# Others
*.cache
*.log
*.vsidx
.vs/
.vscode/
node_modules/
*.tmp
*.temp
EOF

# EditorConfig
cat > .editorconfig << 'EOF'
root = true

[*]
charset = utf-8
end_of_line = crlf
indent_style = space
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,csx,vb,vbx}]
indent_size = 4

[*.{json,js,html,css}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
EOF

# Basic Dockerfile
cat > Dockerfile << 'EOF'
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/SmartKnowledgeBot.API/SmartKnowledgeBot.API.csproj", "src/SmartKnowledgeBot.API/"]
COPY ["src/SmartKnowledgeBot.Business/SmartKnowledgeBot.Business.csproj", "src/SmartKnowledgeBot.Business/"]
COPY ["src/SmartKnowledgeBot.Domain/SmartKnowledgeBot.Domain.csproj", "src/SmartKnowledgeBot.Domain/"]
COPY ["src/SmartKnowledgeBot.Infrastructure/SmartKnowledgeBot.Infrastructure.csproj", "src/SmartKnowledgeBot.Infrastructure/"]
RUN dotnet restore "src/SmartKnowledgeBot.API/SmartKnowledgeBot.API.csproj"
COPY . .
WORKDIR "/src/src/SmartKnowledgeBot.API"
RUN dotnet build "SmartKnowledgeBot.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartKnowledgeBot.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartKnowledgeBot.API.dll"]
EOF

# GitHub Actions workflow
cat > .github/workflows/ci.yml << 'EOF'
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal
      
    - name: Publish
      run: dotnet publish src/SmartKnowledgeBot.API/SmartKnowledgeBot.API.csproj -c Release -o publish
      
    - name: Upload artifacts
      uses: actions/upload-artifact@v4
      with:
        name: published-app
        path: publish
EOF

# Build and test
echo "🔨 Building solution..."
dotnet build

if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
else
    echo "❌ Build failed. Please check the errors above."
    exit 1
fi

# Test
echo "🧪 Running tests..."
dotnet test

echo ""
echo "🎉 Setup complete!"
echo ""
echo "Next steps:"
echo "1. Update appsettings.json with your Azure credentials"
echo "2. Run database migrations: dotnet ef database update"
echo "3. Start the application: dotnet run --project src/SmartKnowledgeBot.API"
echo "4. Visit https://localhost:7001 to see Swagger UI"
echo ""
echo "Happy coding! 🚀"