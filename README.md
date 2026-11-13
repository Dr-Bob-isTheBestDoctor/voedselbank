Get a tenant you can use
A tenant is an organization's own space at Microsoft, where its accounts and apps live. Your school has one. Go to entra.microsoft.com, sign in with your school account and open Entra ID → App registrations. If it says you aren't allowed to register apps, ask your teacher which tenant to use. You can also create your own free one by signing up for a free Azure account, which makes you its administrator.

Register your app
In App registrations, click New registration. Name: Voedselbank. Supported account types: 'Accounts in this organizational directory only (Single tenant)', so only accounts from this tenant can log in. Under Redirect URI choose 'Web' and enter https://localhost:7166/signin-oidc. That's the address Microsoft sends people back to after they log in. Click Register.

Copy the two IDs
On the app's Overview page, copy 'Application (client) ID' (this is your ClientId) and 'Directory (tenant) ID' (your TenantId). Neither is secret, but your app needs both.

Add the logout addresses
Go to Manage → Authentication and add a second redirect URI: https://localhost:7166/signout-callback-oidc. Microsoft uses it to send people back to your app after they log out. Set 'Front-channel logout URL' to https://localhost:7166/signout-oidc. Leave the 'ID tokens' box unticked and click Save.

Create the client secret
Go to Certificates & secrets → Client secrets → New client secret. Type a description, pick an expiry date and click Add. Copy the 'Value' column right away, because Microsoft only shows it once. Don't copy 'Secret ID'; that's something else.

Give the values to your app
Store the TenantId, ClientId and secret with the dotnet user-secrets commands below. That keeps them out of appsettings.json, so they can't end up on GitHub.

Choose who can be admin
Your code makes everyone who logs in with Microsoft an Admin. In a school tenant, by default that's the whole school. Go to Enterprise applications → Voedselbank → Properties, set 'Assignment required?' to Yes and click Save. Then open Users and groups → Add user/group and add only the real admins, starting with yourself.

Test it
Run the app with the https profile (dotnet run --launch-profile https), open /Login and click 'Sign in with Microsoft'. After you log in at Microsoft, you should land back in the app with the Admin badge, and Voorraad should appear in the menu.

dotnet user-secrets init

dotnet user-secrets set "AzureAd:TenantId" "paste-Directory-tenant-ID"

dotnet user-secrets set "AzureAd:ClientId" "paste-Application-client-ID"

dotnet user-secrets set "AzureAd:ClientSecret" "paste-secret-Value"

How to build:

cd BlazorWebTest/

dotnet publish -c Release -o ./publish

the build is now in /publish

for Linux nginx hosting

install .NET runtime for linux

copy the published files to /var/www/myapp


for extra details:
https://chatgpt.com/share/6af57561-6ad4-83eb-9d22-3fb6d17ad0a8

dependencies

Microsoft.AspNetCore.Authentication.JwtBearer Version=11.0.0

Microsoft.Identity.Client Version=4.92.0

Microsoft.Identity.Client.Broker" Version="4.92.0" />

Microsoft.Identity.Web Version=4.18.0

Microsoft.Identity.Web.UI Version=4.18.0