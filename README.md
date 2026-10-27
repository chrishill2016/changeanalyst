# Change Analyst

Business users submit change requests; an AI agent (Microsoft Foundry) asks clarifying questions, works out which
systems are affected and turns the request into clear requirements for a human analyst to review.

- **Web app** – Blazor WebAssembly PWA (MudBlazor), installable and usable offline, hosted on GitHub Pages.
- **API** – Azure Functions (.NET isolated, Flex Consumption). Validates Entra ID tokens; talks to Azure SQL and Foundry
  with its managed identity, so there are no keys or connection-string secrets anywhere.

```
src/ChangeAnalyst/          Blazor WASM PWA
src/ChangeAnalyst.Api/      Azure Functions API
src/ChangeAnalyst.Shared/   Types shared by the app and the API
tests/                      xUnit + bUnit tests
.github/workflows/          Build, test and deploy
```

## Run locally

Requires the .NET 10 SDK.

```bash
dotnet test                          # all tests
dotnet run --project src/ChangeAnalyst   # web app on https://localhost:5001
```

With no settings the app runs signed-out and **local-only** (changes stay in IndexedDB; the status indicator shows
"Local only"). To run the API you also need [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local):

```bash
cp src/ChangeAnalyst.Api/local.settings.sample.json src/ChangeAnalyst.Api/local.settings.json   # fill in the ids
cd src/ChangeAnalyst.Api && func start
```

## Configuration

None of these values are secrets.

**Web app** – `src/ChangeAnalyst/wwwroot/appsettings.json`

| Setting | Value |
|---|---|
| `AzureAd:Authority` | `https://login.microsoftonline.com/<tenant-id>` |
| `AzureAd:ClientId` | client id of the `changeanalyst-web` registration |
| `Api:BaseUrl` | `https://<function-app>.azurewebsites.net/` (or `http://localhost:7071/` locally) |
| `Api:Scope` | `api://<api-client-id>/access_as_user` |

**API** – Function App → Settings → Environment variables (or `local.settings.json`)

| Setting | Value |
|---|---|
| `Auth__TenantId` | tenant id |
| `Auth__Audience` | the API's Application ID URI, `api://<api-client-id>` |
| `Auth__ClientId` | client id of the `changeanalyst-api` registration |

## Deployment

`.github/workflows/build-deploy.yml`:

- **Pull request to `main`** – restore, build, test.
- **Merge to `main`** – test, then publish the web app to GitHub Pages (base href set to `/<repo-name>/`,
  `404.html` for deep links, `.nojekyll`) and, once configured, deploy the API.
- The version shown in the app footer is `<VersionMajorMinor>` from `Directory.Build.props` plus the workflow run number.

One-off setup:

1. **GitHub Pages** – repo Settings → Pages → Source: **GitHub Actions**.
2. **API deploy (OIDC, no secrets)**
   1. Entra ID → App registrations → New: `changeanalyst-github-deploy`.
   2. Certificates & secrets → Federated credentials → Add → *GitHub Actions deploying Azure resources*:
      organisation/repo of this repository, entity **Branch**, branch `main`.
   3. On the Function App → Access control (IAM) → add **Website Contributor** for that registration.
   4. Repo Settings → Secrets and variables → Actions → **Variables**: `AZURE_CLIENT_ID` (the deploy registration),
      `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_FUNCTIONAPP_NAME`.

   The `deploy-api` job is skipped until `AZURE_FUNCTIONAPP_NAME` is set.
