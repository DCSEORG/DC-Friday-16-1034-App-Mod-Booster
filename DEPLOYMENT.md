# Expense Management System - Modern Azure Solution

A complete modernization of a legacy expense management system built with ASP.NET Core 8, Azure SQL Database, and Azure OpenAI.

## 🚀 Features

- **Modern Web UI**: Clean, responsive Razor Pages interface
- **REST API**: Full API with Swagger documentation
- **Secure Authentication**: Azure Managed Identity (no passwords)
- **AI-Powered Chat**: Natural language expense operations via Azure OpenAI
- **Manager Approval Workflow**: Submit, approve, and reject expenses
- **Advanced Filtering**: Filter expenses by status, category, and date range

## 📋 Prerequisites

- Azure CLI installed and configured
- Azure subscription with permissions to create resources
- .NET 8 SDK (for local development)
- Python 3.8+ with pip
- ODBC Driver 18 for SQL Server

## 🏗️ Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture diagrams and component descriptions.

**Key Components:**
- App Service (Linux, .NET 8, S1 SKU)
- Azure SQL Database (Entra ID-only authentication)
- User-Assigned Managed Identity
- Azure OpenAI (GPT-4o, Sweden Central) - Optional
- Azure AI Search (Basic tier) - Optional

## 📦 Deployment

### Option 1: Base Deployment (Without AI Chat)

Deploy the core application with database functionality:

```bash
# Login to Azure
az login

# Run deployment script
./deploy.sh
```

This deploys:
- ✅ App Service with .NET 8 application
- ✅ Azure SQL Database with schema and sample data
- ✅ Managed Identity for secure authentication
- ✅ All 5 UI pages (Dashboard, Add, List, Approve, Chat)
- ⚠️ Chat UI shows dummy responses (GenAI not deployed)

**Estimated Cost**: ~£50-70/month

### Option 2: Full Deployment (With AI Chat)

Deploy the complete solution including Azure OpenAI:

```bash
# Login to Azure
az login

# Run full deployment script
./deploy-with-chat.sh
```

This deploys everything from Option 1 plus:
- ✅ Azure OpenAI with GPT-4o model
- ✅ Azure AI Search for RAG
- ✅ Fully functional AI chat with function calling
- ✅ Natural language expense queries and operations

**Estimated Cost**: ~£100-150/month (depending on OpenAI usage)

### Deployment Process

Both scripts automatically:
1. Create resource group in UK South
2. Deploy infrastructure via Bicep
3. Configure managed identity permissions
4. Set up SQL firewall rules
5. Import database schema
6. Deploy stored procedures
7. Build and deploy .NET application
8. Configure App Service settings

**Duration**: ~10-15 minutes

## 🌐 Accessing the Application

After deployment completes, the script outputs:

```
🌐 Application URL: https://app-expensemgmt-[unique].azurewebsites.net/Index
```

**Important**: Navigate to `/Index` (not just the root URL)

### Available Pages

- `/Index` - Dashboard with stats and navigation
- `/AddExpense` - Create new expense
- `/ListExpenses` - View and filter all expenses
- `/ApproveExpenses` - Manager approval interface
- `/Chat` - AI chat interface (fully functional with deploy-with-chat.sh)
- `/swagger` - API documentation

## 🔧 Local Development

To run the application locally:

1. **Configure connection string** in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=tcp:your-server.database.windows.net;Database=ExpenseManagement;Authentication=Active Directory Default;"
     }
   }
   ```

2. **Login to Azure CLI**:
   ```bash
   az login
   ```

3. **Run the application**:
   ```bash
   cd ExpenseManagementApp
   dotnet restore
   dotnet run
   ```

4. **Access locally**: http://localhost:5000/Index

**Note**: `Authentication=Active Directory Default` uses your Azure CLI credentials locally.

## 📊 Database Schema

The database includes:
- **Tables**: Roles, Users, ExpenseCategories, ExpenseStatus, Expenses
- **Stored Procedures**: 15+ procedures for all CRUD operations
- **Sample Data**: 2 users (Alice - Employee, Bob - Manager), 4 sample expenses

All application data access uses stored procedures - no direct T-SQL in app code.

## 🤖 AI Chat Capabilities

When deployed with `deploy-with-chat.sh`, the AI chat can:

- **Query expenses**: "Show me all submitted expenses"
- **Create expenses**: "Add a £50 travel expense for today"
- **Get summaries**: "What's my total spending this month?"
- **Filter data**: "List all approved expenses over £100"
- **Natural language**: Understands context and intent

The chat uses Azure OpenAI function calling to execute real database operations via the REST API.

## 🔐 Security Features

- ✅ **Entra ID-only authentication** for Azure SQL (no SQL passwords)
- ✅ **Managed Identity** for all service-to-service communication
- ✅ **No secrets in code** or configuration
- ✅ **HTTPS enforced** on App Service
- ✅ **TLS 1.2+ required** for all connections
- ✅ **Firewall rules** restrict database access
- ✅ **Principle of least privilege** for managed identity permissions

## 📁 Repository Structure

```
├── infrastructure/          # Bicep IaC files
│   ├── main.bicep          # Main orchestrator
│   ├── app-service.bicep   # App Service + Managed Identity
│   ├── azure-sql.bicep     # SQL Server + Database
│   └── genai.bicep         # Azure OpenAI + AI Search
├── ExpenseManagementApp/   # ASP.NET Core 8 application
│   ├── Models/             # DTOs and data models
│   ├── Services/           # Database and Chat services
│   ├── Pages/              # Razor Pages UI
│   └── wwwroot/            # Static assets (CSS, JS)
├── Database-Schema/        # Original database schema
├── stored-procedures.sql   # All stored procedures
├── script.sql              # Managed identity permissions
├── run-sql.py              # Schema import script
├── run-sql-dbrole.py       # Permissions script
├── run-sql-stored-procs.py # Stored procedures deployment
├── deploy.sh               # Base deployment script
├── deploy-with-chat.sh     # Full deployment with AI
└── ARCHITECTURE.md         # Architecture diagrams
```

## 🧪 Testing

1. **Create an expense**:
   - Go to /AddExpense
   - Fill in amount (e.g., 25.50), category, date, description
   - Click "Create Expense"

2. **List expenses**:
   - Go to /ListExpenses
   - Use filters for status, category, date range
   - View expenses in table format

3. **Approve expenses** (as manager):
   - Go to /ApproveExpenses
   - See all submitted expenses
   - Click "Approve" or "Reject"

4. **Test API**:
   - Go to /swagger
   - Try GET /api/expenses
   - Test other endpoints

5. **Chat with AI** (if deployed with GenAI):
   - Go to /Chat
   - Type: "Show me all my expenses"
   - Try: "Add a £100 accommodation expense for yesterday"

## 🛠️ Troubleshooting

### Deployment Issues

**Managed Identity Errors**:
- Ensure the 30-second wait completed after infrastructure deployment
- Check that AZURE_CLIENT_ID is set in App Service settings
- Verify managed identity has correct permissions on SQL Database

**SQL Connection Errors**:
- Verify firewall rules include your IP
- Check that Entra ID admin is configured correctly
- Ensure managed identity user exists in database

**Build Errors**:
- Verify .NET 8 SDK is installed
- Check that all NuGet packages restored successfully
- Ensure Python dependencies installed (pyodbc, azure-identity)

### Application Issues

**Chat Returns Dummy Responses**:
- You deployed with `deploy.sh` instead of `deploy-with-chat.sh`
- Redeploy with `deploy-with-chat.sh` to enable AI features

**Database Connection Fails**:
- Check connection string in App Service settings
- Verify managed identity client ID is correct
- Check SQL firewall rules allow App Service

**API Errors**:
- Check /swagger for API documentation
- Verify stored procedures exist in database
- Check App Service logs for detailed errors

## 📚 Additional Resources

- [Azure App Service Documentation](https://learn.microsoft.com/en-us/azure/app-service/)
- [Azure SQL Database Best Practices](https://learn.microsoft.com/en-us/azure/azure-sql/database/security-best-practice)
- [Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Managed Identity Overview](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview)

## 📝 License

See [LICENSE](LICENSE) file for details.

---

**Built with Azure best practices** | **Secure by design** | **Production-ready**
