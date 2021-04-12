# AvantiPoint Packages Templates

The following is a dotnet template for a basic NuGet Package feed using AvantiPoint Packages. This feed uses Azure Active Directory to authenticate users in the web. Authenticated users can then create and manage their own Auth Tokens for use with the package feed. By default only the first user has Package Publishing privileges. You can change this or implement more complex user management scenarios.

In addition to this the NuGet Package Authentication, and Callback Handlers are pre-wired up, and come with an Email Service and basic html templates. This will send an email to your users to welcome them when they create their first token, along with any time they create or revoke a token, they have uploaded a package or symbols package, or download a package from a new IP Address.

You will need:

1. Create a new Application in Azure Active Directory. Be sure to add Access and ID tokens after creating the application in the Azure Portal.
2. Update the App Settings with your Tenant Id, Client Id and Domain.
3. Update the Email Settings with the email address you want emails to send from along with the Send Grid API Key.