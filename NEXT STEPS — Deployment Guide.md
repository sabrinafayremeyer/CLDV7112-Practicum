# Deployment Guide — Step by Step

Follow these steps in order. Do not skip ahead — each phase depends on the one before it.

---

## PHASE 0 — Azure Portal Setup

### Step 1 — Create a Resource Group

1. Go to [portal.azure.com](https://portal.azure.com)
2. Search **"Resource groups"** → **Create**
3. Fill in:
   - **Subscription:** Your student subscription
   - **Resource group name:** `rg-gamestop`
   - **Region:** South Africa North (or whichever is closest)
4. Click **Review + create** → **Create**

---

### Step 2 — Create Azure SQL Server + Database

1. Search **"SQL databases"** → **Create**
2. **Basics tab:**
   - Resource group: `rg-gamestop`
   - Database name: `gamestop-db`
   - Server: click **Create new**
     - Server name: `gamestop-sfm-sql-server` (must be globally unique — add numbers if needed)
     - Location: same as resource group
     - Authentication: **SQL authentication**
     - Admin login: `sqladmin`
     - Password: choose something strong — **write it down now, you will need it repeatedly**
3. **Compute + storage:** Click **Configure database** → select **Basic** (cheapest, ~R30/mo)
4. Click **Review + create** → **Create**
5. Once deployed, go to the SQL Server resource → **Networking** → **Firewall rules**
   - Toggle **"Allow Azure services and resources to access this server"** → **ON**
   - Click **+ Add your client IP** (adds your current IP for migrations)
   - Click **Save**

**Get the connection string:**
- Go to the SQL **database** (not server) → **Settings** → **Connection strings**
- Copy the **ADO.NET** string — it looks like:
  ```
  Server=tcp:gamestop-sfm-sql-server.database.windows.net,1433;Initial Catalog=gamestop-db;Persist Security Info=False;User ID=sqladmin;Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
  ```
- Replace `{your_password}` with the actual password
- **Save this string** — you will need it in multiple places

---

### Step 3 — Create Storage Account

1. Search **"Storage accounts"** → **Create**
2. Fill in:
   - Resource group: `rg-gamestop`
   - Storage account name: `gamestopstoragesfm`
   - Region: same as above
   - Redundancy: **Locally-redundant storage (LRS)**
3. Click **Review + create** → **Create**

**Create sub-resources inside the storage account:**

Once deployed, go to the storage account:

**Blob containers (Storage → Containers):**
- Click **+ Container** → name: `game-images` → Access level: **Blob (anonymous read access for blobs only)** → Create
- Click **+ Container** → name: `game-thumbnails` → Access level: **Blob** → Create

**Table (Storage → Tables):**
- Click **+ Table** → name: `AuditLogs` → OK

**Queue (Storage → Queues):**
- Click **+ Queue** → name: `orders-queue` → OK

**Get the connection string:**
- Go to **Security + networking** → **Access keys**
- Click **Show** next to key1
- Copy the **Connection string** — it looks like:
  ```
  DefaultEndpointsProtocol=https;AccountName=gamestopstoragesfm;AccountKey=xxxx...;EndpointSuffix=core.windows.net
  ```
- **Save this string**

---

### Step 4 — Create App Service Plan + App Service

1. Search **"App Service plans"** → **Create**
2. Fill in:
   - Resource group: `rg-gamestop`
   - Name: `gamestop-plan`
   - Operating System: **Windows**
   - Region: same as above
   - Pricing plan: **Standard S1** ← IMPORTANT: must be S1, not Free/Basic — only Standard supports autoscale
3. Click **Review + create** → **Create**

4. Search **"App Services"** → **Create** → **Web App**
5. Fill in:
   - Resource group: `rg-gamestop`
   - Name: `gamestop-web` (must be globally unique — try `gamestop-web-2026`)
   - Publish: **Code**
   - Runtime stack: **.NET 8 (LTS)**
   - Operating System: **Windows**
   - Region: same
   - App Service Plan: select `gamestop-plan` (the S1 you just created)
6. Click **Review + create** → **Create**

---

### Step 5 — Create Function App

1. Search **"Function App"** → **Create**
2. Fill in:
   - Resource group: `rg-gamestop`
   - Function App name: `gamestop-sfm-functions` (must be globally unique)
   - Runtime stack: **.NET**
   - Version: **8 (LTS), isolated worker model**
   - Region: same
   - Hosting plan: **Consumption (Serverless)**
3. On the **Storage** tab: select your `gamestopstoragesfm` account
4. Click **Review + create** → **Create**

---

## PHASE 1 — Configure Connection Strings Locally

### Step 6 — Update appsettings.json

Open `GameStop.Web\appsettings.json` in Visual Studio and replace the placeholder values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "PASTE_YOUR_SQL_CONNECTION_STRING_HERE"
  },
  "AzureStorage": {
    "ConnectionString": "PASTE_YOUR_STORAGE_CONNECTION_STRING_HERE",
    "BlobContainer": "game-images",
    "ThumbnailContainer": "game-thumbnails",
    "AuditTable": "AuditLogs",
    "OrderQueue": "orders-queue"
  },
  "AzureFunctions": {
    "BaseUrl": "https://gamestop-sfm-functions.azurewebsites.net",
    "GetStockPath": "/api/GetGameStock",
    "PlaceOrderPath": "/api/PlaceOrder"
  }
}
```

> Note: The Function BaseUrl will only work after you deploy the Functions app in Phase 3. During local testing the stock badge will fall back to the database.

### Step 7 — Update Functions local.settings.json

Open `GameStop.Functions\local.settings.json` and replace the placeholder values:

```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "PASTE_YOUR_STORAGE_CONNECTION_STRING_HERE",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "SqlConnectionString": "PASTE_YOUR_SQL_CONNECTION_STRING_HERE",
        "BlobContainerName": "game-images",
        "ThumbnailContainerName": "game-thumbnails",
        "AuditTableName": "AuditLogs",
        "OrderQueueName": "orders-queue"
    }
}
```

---

## PHASE 2 — Database Migration

### Step 8 — Run EF Core Migrations

In Visual Studio, open the **Package Manager Console** (Tools → NuGet Package Manager → Package Manager Console).

Make sure the **Default project** dropdown at the top of the console is set to `GameStop.Web`.

Run these two commands one at a time:

```powershell
Add-Migration InitialCreate
```

Wait for it to complete, then:

```powershell
Update-Database
```

This creates the `Roles`, `Users`, and `Games` tables in your Azure SQL database.

**Verify it worked:**
- Go to Azure portal → SQL Database → **Query editor (preview)**
- Log in with `sqladmin` and your password
- Run: `SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES`
- You should see: `Roles`, `Users`, `Games`, `__EFMigrationsHistory`

---

## PHASE 3 — Publish the Web App

### Step 9 — Publish GameStop.Web to Azure

1. In Visual Studio, right-click the `GameStop.Web` project → **Publish**
2. Click **+ New** (or **Add a publish profile**)
3. Select **Azure** → **Azure App Service (Windows)** → **Next**
4. Sign in with your Microsoft/Azure account if prompted
5. Select your subscription → expand `rg-gamestop` → select `gamestop-web` → **Finish**
6. Click **Publish** — wait for the upload to complete

### Step 10 — Set App Service Environment Variables

After publishing, go to Azure portal → App Service `gamestop-web` → **Settings** → **Environment variables** → **App settings** tab.

Add the following (click **+ Add** for each):

| Name | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | your SQL connection string |
| `AzureStorage__ConnectionString` | your Storage connection string |
| `AzureStorage__BlobContainer` | `game-images` |
| `AzureStorage__ThumbnailContainer` | `game-thumbnails` |
| `AzureStorage__AuditTable` | `AuditLogs` |
| `AzureStorage__OrderQueue` | `orders-queue` |
| `AzureFunctions__BaseUrl` | `https://gamestop-sfm-functions.azurewebsites.net` |
| `AzureFunctions__GetStockPath` | `/api/GetGameStock` |
| `AzureFunctions__PlaceOrderPath` | `/api/PlaceOrder` |

Click **Apply** → **Confirm** (the app will restart).

### Step 11 — First Run (Seed Data)

1. Go to your App Service → **Overview** → click the **Default domain** URL (e.g. `https://gamestop-web.azurewebsites.net`)
2. The first request will trigger the `DataSeeder`:
   - Creates Roles (Admin, Customer)
   - Creates admin user
   - Uploads all 9 game cover images to Blob Storage
   - Inserts all 9 game records into SQL
3. This first load may take 30–60 seconds — that is normal
4. Once loaded you should see the homepage with 4 featured games

> **Screenshot opportunity (Part A - b):** Go to Azure portal → SQL Database → Query editor → run `SELECT * FROM Games` to show data in Azure SQL. Screenshot this as proof.

---

## PHASE 4 — Publish the Functions App

### Step 12 — Publish GameStop.Functions to Azure

1. In Visual Studio, right-click the `GameStop.Functions` project → **Publish**
2. Click **+ New**
3. Select **Azure** → **Azure Function App (Windows)** → **Next**
4. Select your subscription → `gamestop-sfm-functions` → **Finish**
5. Click **Publish**

### Step 13 — Set Function App Configuration

Go to Azure portal → Function App `gamestop-sfm-functions` → **Settings** → **Environment variables**.

Add the following:

| Name | Value |
|---|---|
| `AzureWebJobsStorage` | your Storage connection string |
| `SqlConnectionString` | your SQL connection string |
| `BlobContainerName` | `game-images` |
| `ThumbnailContainerName` | `game-thumbnails` |
| `AuditTableName` | `AuditLogs` |
| `OrderQueueName` | `orders-queue` |

Click **Apply** → **Confirm**.

### Step 14 — Verify Functions are Running

Go to Azure portal → Function App `gamestop-sfm-functions` → **Functions**.

You should see all 4 functions listed:
- `GameImageProcessor`
- `GetGameStock`
- `PlaceOrder`
- `ProcessOrder`

**Test the GET function:**
- Click `GetGameStock` → **Test/Run** → Method: GET
- Add query param: `gameId` = `1`
- Click **Run** — should return JSON with stock count

**Test the POST function:**
- Click `PlaceOrder` → **Test/Run** → Method: POST
- Body: `{ "gameId": 1, "userId": 1, "quantity": 1 }`
- Click **Run** — should return `202 Accepted`

> **Screenshot opportunity (Part A - d):** Screenshot all 4 functions in the portal and the test results as proof of the serverless pipeline.

---

## PHASE 5 — Verify the Full App

### Step 15 — End-to-End Test

Go to your App Service URL and test the following flow:

1. **Home page** loads with 4 featured games ✓
2. Click **Catalog** — all 9 games visible with cover images ✓
3. Click any game → **Details page** — stock badge loads (green/amber/red) ✓
4. Click **Register** → create a test customer account ✓
5. Log in as customer → click a game → click **Add to Cart**
   - Toast notification appears
   - Stock count should decrement after a few seconds ✓
6. Log out → Log in as **admin** (username: `admin`, password: `Admin@123`)
7. **Admin Dashboard** loads with metric cards and inventory table ✓
8. Click **+ Add Game** → upload a new game with an image ✓

> **Screenshot opportunity:** Screenshot each of these pages as evidence for Part A (a), (b), (c).

---

## PHASE 6 — Auto Scaling

### Step 16 — Configure Auto Scale Rules

1. Go to App Service Plan `gamestop-plan` → **Settings** → **Scale out (App Service plan)**
2. Click **Custom autoscale**
3. Set the default instance count to **1**
4. Click **+ Add a rule** for scale OUT:
   - Metric source: **This resource**
   - Metric name: **CPU Percentage**
   - Operator: **Greater than**
   - Threshold: **70**
   - Duration: **5 minutes**
   - Action: **Increase count by 1**
   - Cool down: **5 minutes**
5. Click **+ Add a rule** for scale IN:
   - Metric name: **CPU Percentage**
   - Operator: **Less than**
   - Threshold: **30**
   - Duration: **5 minutes**
   - Action: **Decrease count by 1**
   - Cool down: **5 minutes**
6. Set **Maximum** instance count to **3**, **Minimum** to **1**
7. Click **Save**

> **Screenshot opportunity (Part A - g):** Screenshot the scale rule configuration screen.

---

## PHASE 7 — Load Testing

### Step 17 — Create Azure Load Testing Resource

1. Search **"Azure Load Testing"** → **Create**
2. Fill in:
   - Resource group: `rg-gamestop`
   - Name: `gamestop-loadtest`
   - Region: same as your other resources
3. Click **Review + create** → **Create**

### Step 18 — Run the Load Test

1. Open `gamestop-loadtest` → **Tests** → **+ Create** → **Create a URL-based test**
2. **Test plan tab:**
   - Add URL: `https://gamestop-web.azurewebsites.net/Games`
   - Add URL: `https://gamestop-web.azurewebsites.net/Games/Details/1`
   - Add URL: `https://gamestop-web.azurewebsites.net`
3. **Load tab:**
   - Virtual users: **50**
   - Test duration: **5 minutes**
   - Ramp-up time: **1 minute**
4. Click **Review + create** → **Create and run**
5. **While the test is running**, open a second browser tab and go to App Service → **Monitoring** → **Metrics** → add the `CPU Percentage` and `Http Requests` metrics — screenshot this graph during the test
6. After the test finishes, go to the test results — screenshot the summary showing response times, throughput, and error rate

> **Screenshot opportunity (Part A - e + f):** Load test config + results + Azure Monitor metrics graph during test.

### Step 19 — Check Autoscale Triggered

1. Go to App Service Plan `gamestop-plan` → **Monitoring** → **Activity log**
2. Filter by **Autoscale** operations
3. You should see a "Scale up" event if CPU exceeded 70%

> If autoscale didn't trigger (CPU stayed low), go back to the Load Test, increase virtual users to 100 and run it again. Alternatively, go to the scale rule and temporarily lower the CPU threshold to 20% just to generate a visible scale event, then restore it to 70%.

> **Screenshot opportunity (Part A - g):** Screenshot the autoscale activity log showing the scale event.

---

## Quick Reference — Admin Credentials

| Field | Value |
|---|---|
| URL | `https://gamestop-web.azurewebsites.net` |
| Admin username | `admin` |
| Admin password | `Admin@123` |
| Azure SQL admin | `sqladmin` |
| Azure SQL password | whatever you set in Step 2 |

---

## Screenshot Checklist for Submission

| Requirement | What to Screenshot |
|---|---|
| Part A (a) — App Service | Azure portal showing App Service running + browser showing live site |
| Part A (b) — SQL Database | Azure portal SQL database overview + Query Editor showing data |
| Part A (c) — Blob Storage | Storage account with game-images container showing uploaded covers |
| Part A (c) — Table Storage | AuditLogs table with entries |
| Part A (d) — Functions | Function App showing all 4 functions + test results for each |
| Part A (e) — Load Test | Load test config + results summary |
| Part A (f) — Monitoring | App Service → Monitoring → Metrics (CPU Percentage + Http Requests) during load test |
| Part A (g) — Auto Scale | Scale rules config + Activity Log showing scale event triggered |
