# Copilot Instructions for AvantiPoint Packages Templates

## Project Overview

This repository contains a dotnet template for creating a self-hosted NuGet Package feed using AvantiPoint Packages. The template generates an ASP.NET Core application that:

- Uses Azure Active Directory for user authentication
- Allows authenticated users to create and manage Auth Tokens for package feed access
- Provides pre-wired NuGet Package Authentication and Callback Handlers
- Includes an Email Service with HTML templates for user notifications (welcome, token creation/revocation, package uploads, downloads from new IPs)
- Integrates with SendGrid for email delivery
- Uses SQL Server for data persistence

## Technology Stack

- **.NET SDK**: 9.0
- **Framework**: ASP.NET Core (net9.0)
- **Authentication**: Azure Active Directory via Microsoft.Identity.Web
- **Email Service**: SendGrid (primary) and Postmark (alternative)
- **Database**: SQL Server with Entity Framework Core
- **Package Management**: NuGet
- **Template Engine**: Handlebars.Net for email templates

## Repository Structure

```
/templates/NuGetFeedTemplate/
  ├── Authentication/     # Azure AD authentication logic
  ├── Configuration/      # Application configuration classes
  ├── Controllers/        # ASP.NET Core controllers
  ├── Data/              # Entity Framework DbContext and data access
  ├── Migrations/        # EF Core database migrations
  ├── Models/            # Data models and view models
  ├── Pages/             # Razor pages
  ├── Services/          # Business logic and services
  ├── wwwroot/           # Static web assets
  ├── appsettings.json   # Application settings template
  └── Program.cs         # Application entry point

/.github/workflows/      # CI/CD workflows
/pack.ps1               # Template packaging script
/template.nuspec        # NuGet package specification
```

## Building and Testing

### Building the Template Package

The repository uses PowerShell for packaging:

```powershell
# Build locally with default version
./pack.ps1

# Build with specific version
./pack.ps1 -Version "1.0.42"
```

The script will:
1. Clean build artifacts (bin, obj folders)
2. Temporarily rename .editorconfig and .gitignore files
3. Create NuGet package in the `Artifacts/` directory
4. Restore original file names

### Testing the Template

After packing, you can install and test the template locally:

```bash
# Install the template from the generated .nupkg
dotnet new install ./Artifacts/AvantiPoint.Packages.Templates.1.0.0.nupkg

# Create a new project from the template
dotnet new nugetfeed -n MyNuGetFeed

# Build the generated project
cd MyNuGetFeed
dotnet build
```

### CI/CD

The repository uses GitHub Actions (`.github/workflows/ci.yaml`) to:
- Build and pack templates on push to master
- Upload artifacts
- Publish to configured NuGet feeds

## Coding Standards and Conventions

### General Guidelines

1. **Minimal Changes**: Make the smallest possible changes to achieve the goal
2. **Don't Break Existing Code**: Never modify working code unless absolutely necessary
3. **Follow Existing Patterns**: Match the coding style and patterns already in use
4. **Implicit Usings**: The project uses implicit usings (.NET 6+)
5. **File-Scoped Namespaces**: Prefer file-scoped namespaces when editing C# files

### C# Code Style

- Use modern C# features (pattern matching, records, etc.) where appropriate
- Follow ASP.NET Core best practices for controllers, services, and middleware
- Entity Framework conventions for data access
- Use dependency injection for services

### Template-Specific Guidelines

1. **Template Configuration**: Files in `.template.config/` define template parameters and behavior
2. **Conditional Content**: Use template parameters for conditional content (e.g., email provider choice)
3. **File Naming**: Templates use `git.ignore` and `editor.config` (renamed during packaging)
4. **Settings Templates**: Use `appsettings.Template.json` as the base for generated settings

### Documentation

- Update ReadMe.md if adding new features or changing setup instructions
- Document new template parameters in template.json
- Include XML comments for public APIs in generated code

## Common Tasks

### Adding a New NuGet Package Dependency

1. Add the PackageReference to `templates/NuGetFeedTemplate/NuGetFeedTemplate.csproj`
2. Test the template generation and build
3. Update documentation if the package requires configuration

### Modifying Email Templates

1. Email templates use Handlebars syntax
2. Located in the Services folder
3. Test with sample data to ensure proper rendering

### Updating Database Models

1. Modify the entity classes in Models/
2. Generate new migrations using EF Core tools
3. Update seed data if necessary
4. Test migration up and down

### Modifying Authentication

1. Authentication logic is in the Authentication/ folder
2. Azure AD configuration is in appsettings.json
3. Follow Microsoft.Identity.Web patterns and best practices

## Security Considerations

1. **Never Commit Secrets**: Use user secrets for local development, environment variables for production
2. **API Keys**: SendGrid API keys and Azure AD credentials must be configured by users
3. **Token Security**: Auth tokens for package feeds should be securely generated and stored
4. **Input Validation**: Always validate user input in controllers and services
5. **SQL Injection**: Use parameterized queries (EF Core handles this)

## Dependencies to Watch

- AvantiPoint.Packages.* packages (custom packages for NuGet feed functionality)
- Microsoft.Identity.Web (Azure AD integration)
- SendGrid (email service)
- Entity Framework Core (data access)

## Testing Approach

Since this is a template repository:
- Primary testing is generating projects from the template and building them
- Verify all template parameters work correctly
- Test with different configuration options
- Ensure generated projects build and run successfully

## File Patterns to Preserve

- `.editorconfig` and `.gitignore` are renamed during packaging (handled by pack.ps1)
- `LICENSE` is renamed to `LICENSE.txt` during packaging
- All renames are reversed after packaging

## When Making Changes

1. **Template Files**: Changes to files in `templates/` affect generated projects
2. **Build Scripts**: Changes to `pack.ps1` affect packaging process
3. **Workflows**: Changes to `.github/workflows/` affect CI/CD
4. **NuSpec**: Changes to `template.nuspec` affect package metadata

## Questions or Issues?

- Check the GitHub repository: https://github.com/avantipoint/packages.templates
- Review existing issues and pull requests
- Follow ASP.NET Core and .NET template documentation for guidance
