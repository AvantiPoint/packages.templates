# AvantiPoint Packages Templates

The following is a dotnet template for a basic NuGet Package feed using AvantiPoint Packages. This feed uses JWT-based authentication with OAuth providers (Microsoft or Google) to authenticate users in the web interface. Authenticated users can then create and manage their own Auth Tokens for use with the package feed. By default only the first user has Package Publishing privileges. You can change this or implement more complex user management scenarios.

The NuGet Package Authentication and Callback Handlers are pre-wired up, and come with an Email Service and basic html templates. This will send an email to your users to welcome them when they create their first token, along with any time they create or revoke a token, they have uploaded a package or symbols package, or download a package from a new IP Address.

## Authentication

This template uses JWT (JSON Web Tokens) with refresh tokens for authentication. Users can sign in using either Microsoft Azure AD or Google OAuth. The authentication flow:

1. User clicks "Sign in" and selects their provider (Microsoft or Google)
2. User is redirected to the OAuth provider to authenticate
3. After successful authentication, the user is redirected back with an authorization code
4. The application exchanges the code for user information and generates JWT access and refresh tokens
5. Tokens are stored locally and used for subsequent requests
6. When the user logs out, refresh tokens are revoked and local state is cleared

### Key Features

- **JWT Access Tokens**: Short-lived tokens (15 minutes by default) for API authentication
- **Refresh Tokens**: Long-lived tokens (7 days by default) for obtaining new access tokens
- **Local Logout**: Signing out only clears the local session and revokes tokens, without logging out from Microsoft or Google
- **Flexible Provider Choice**: Choose between Microsoft Azure AD or Google OAuth during template setup

## Setup Instructions

You will need:

### For Microsoft Azure AD:

1. Create a new Application in Azure Active Directory
2. Add a Web platform with redirect URI: `https://your-domain.com/api/authentication/callback/microsoft`
3. Enable ID tokens in the Authentication settings
4. Create a client secret
5. Update the app settings with your Tenant ID, Client ID, and Client Secret

### For Google OAuth:

1. Create a new project in Google Cloud Console
2. Enable the Google+ API
3. Create OAuth 2.0 credentials (Web application type)
4. Add authorized redirect URI: `https://your-domain.com/api/authentication/callback/google`
5. Update the app settings with your Client ID and Client Secret

### Additional Configuration:

3. Generate a secure JWT secret key (minimum 32 characters)
4. Update the Email Settings with the email address you want emails to send from along with the SendGrid API Key

## Template Parameters

When creating a new project from this template, you can specify:

- `--OAuthProvider`: Choose "Microsoft" or "Google" for authentication
- `--MSTenantId`: Microsoft Azure AD Tenant ID
- `--MSClientId`: Microsoft Azure AD Client ID  
- `--MSClientSecret`: Microsoft Azure AD Client Secret
- `--GoogleClientId`: Google OAuth Client ID
- `--GoogleClientSecret`: Google OAuth Client Secret
- `--JwtSecret`: Secret key for signing JWT tokens (min 32 chars)
- `--EmailFromDomain`: Domain for sending emails from
- `--SendGridApiKey`: SendGrid API key for email delivery
- `--PostmarkApiKey`: Postmark API key (alternative to SendGrid)