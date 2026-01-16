# Azure Architecture Diagram - Expense Management System

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Azure Subscription                                  │
│                                                                               │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    Resource Group: rg-expensemgmt-demo                │  │
│  │                                                                        │  │
│  │  ┌────────────────────────────────────────────────────────────────┐  │  │
│  │  │                User-Assigned Managed Identity                   │  │  │
│  │  │            mid-appmodassist-[timestamp]                        │  │  │
│  │  │                                                                 │  │  │
│  │  │  • Authentication for App Service → Azure SQL                  │  │  │
│  │  │  • Authentication for App Service → Azure OpenAI               │  │  │
│  │  │  • Authentication for App Service → AI Search                  │  │  │
│  │  └────────────────────────────────────────────────────────────────┘  │  │
│  │                                 ↓                                      │  │
│  │  ┌─────────────────────────────────────────────────────────────────┐ │  │
│  │  │               App Service (Linux, .NET 8)                        │ │  │
│  │  │                app-expensemgmt-[unique]                         │ │  │
│  │  │                                                                  │ │  │
│  │  │  Components:                                                     │ │  │
│  │  │  • ASP.NET Core Razor Pages (UI)                               │ │  │
│  │  │  • REST API with Swagger                                        │ │  │
│  │  │  • Database Service (Stored Procedures)                         │ │  │
│  │  │  • Chat Service (Azure OpenAI Integration)                      │ │  │
│  │  │                                                                  │ │  │
│  │  │  Pages:                                                          │ │  │
│  │  │  • /Index - Dashboard                                           │ │  │
│  │  │  • /AddExpense - Create expenses                                │ │  │
│  │  │  • /ListExpenses - View & filter                                │ │  │
│  │  │  • /ApproveExpenses - Manager approval                          │ │  │
│  │  │  • /Chat - AI Chat Interface                                    │ │  │
│  │  └─────────────────────────────────────────────────────────────────┘ │  │
│  │           │                    │                        │              │  │
│  │           │ (MI Auth)          │ (MI Auth)              │ (MI Auth)    │  │
│  │           ↓                    ↓                        ↓              │  │
│  │  ┌──────────────────┐  ┌─────────────────┐   ┌──────────────────────┐│  │
│  │  │   Azure SQL      │  │  Azure OpenAI   │   │   Azure AI Search    ││  │
│  │  │   (UK South)     │  │ (Sweden Central)│   │    (UK South)        ││  │
│  │  │                  │  │                 │   │                      ││  │
│  │  │  Server:         │  │  Model:         │   │  Service:            ││  │
│  │  │  sql-expensemgmt │  │  gpt-4o         │   │  search-expensemgmt  ││  │
│  │  │                  │  │  Capacity: 8    │   │  SKU: Basic          ││  │
│  │  │  Database:       │  │  SKU: S0        │   │                      ││  │
│  │  │  ExpenseManagement│ │                 │   │  Purpose:            ││  │
│  │  │                  │  │  Purpose:        │   │  • RAG for chat     ││  │
│  │  │  Auth:           │  │  • Function     │   │  • Document search  ││  │
│  │  │  • Entra ID only │  │    calling      │   │                      ││  │
│  │  │  • No SQL auth   │  │  • Natural      │   │                      ││  │
│  │  │                  │  │    language     │   │                      ││  │
│  │  │  Data Access:    │  │    queries      │   │                      ││  │
│  │  │  • Stored Procs  │  │  • Chat UI      │   │                      ││  │
│  │  │    only          │  │                 │   │                      ││  │
│  │  └──────────────────┘  └─────────────────┘   └──────────────────────┘│  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
└───────────────────────────────────────────────────────────────────────────────┘

                            External Users
                                  │
                                  │ HTTPS
                                  ↓
                    ┌──────────────────────────────┐
                    │  App Service Public Endpoint │
                    │  https://app-expensemgmt-*   │
                    │  .azurewebsites.net         │
                    └──────────────────────────────┘
```

## Component Details

### 1. **User-Assigned Managed Identity**
- **Name Pattern**: `mid-appmodassist-[DD-HH-MM]`
- **Purpose**: Secure authentication across all Azure services
- **Assigned Roles**:
  - `db_datareader`, `db_datawriter`, `EXECUTE` on Azure SQL
  - `Cognitive Services OpenAI User` on Azure OpenAI
  - `Search Index Data Reader` on AI Search

### 2. **App Service Plan**
- **SKU**: S1 Standard (no cold start)
- **OS**: Linux
- **Runtime**: .NET 8 (LTS)
- **Always On**: Enabled

### 3. **Azure SQL Database**
- **Tier**: Basic (5 DTU) - suitable for POC
- **Authentication**: Entra ID Only (azureADOnlyAuthentication: true)
- **Firewall**: Azure services + deployment IP
- **Schema**: 
  - 5 tables: Roles, Users, ExpenseCategories, ExpenseStatus, Expenses
  - 15+ stored procedures for all CRUD operations
  - Sample data included

### 4. **Azure OpenAI** (Conditional - deploy-with-chat.sh only)
- **Location**: Sweden Central (for GPT-4o availability)
- **Model**: gpt-4o (2024-05-13)
- **Capacity**: 8 units
- **Features**:
  - Function calling for database operations
  - Natural language expense queries
  - Integrated with app APIs

### 5. **Azure AI Search** (Conditional - deploy-with-chat.sh only)
- **SKU**: Basic
- **Purpose**: RAG (Retrieval-Augmented Generation)
- **Integration**: Provides context to OpenAI for better responses

## Data Flow

### Expense Creation Flow
```
User → /AddExpense → POST /api/expenses → DatabaseService 
→ EXEC CreateExpense (Stored Proc) → Azure SQL → Return ExpenseId
```

### Chat Query Flow
```
User → /Chat → POST /api/chat → ChatService → Azure OpenAI (Function Calling)
→ Determines operation → Call API endpoint → DatabaseService → Stored Proc 
→ Azure SQL → Return data → Format response → Display in chat
```

### Authentication Flow
```
App Service starts → DefaultAzureCredential → Detects Managed Identity
→ Gets access token for SQL → Connection with MI → Database access granted
→ Gets access token for OpenAI → API calls authenticated
```

## Security Features

1. **No Secrets in Code**: All authentication via Managed Identity
2. **Entra ID Only**: SQL authentication disabled
3. **HTTPS Only**: App Service enforces HTTPS
4. **Firewall Rules**: SQL Server allows only Azure services + known IPs
5. **Principle of Least Privilege**: MI has only required permissions
6. **TLS 1.2+**: Minimum TLS version enforced

## Deployment Options

### Option 1: Base Deployment (deploy.sh)
- App Service + Azure SQL only
- Chat UI present but shows dummy responses
- Cost: ~£50-70/month

### Option 2: Full Deployment (deploy-with-chat.sh)
- App Service + Azure SQL + Azure OpenAI + AI Search
- Fully functional AI chat
- Cost: ~£100-150/month (depending on OpenAI usage)

## Resource Naming Convention

All resources use `uniqueString(resourceGroup().id)` for unique suffixes:
- App Service Plan: `asp-expensemgmt-[unique]`
- App Service: `app-expensemgmt-[unique]`
- SQL Server: `sql-expensemgmt-[unique]`
- Azure OpenAI: `oai-expensemgmt-[unique]` (lowercase)
- AI Search: `search-expensemgmt-[unique]` (lowercase)
- Managed Identity: `mid-appmodassist-[timestamp]`

## URLs

- **Application**: `https://app-expensemgmt-[unique].azurewebsites.net/Index`
- **Swagger API**: `https://app-expensemgmt-[unique].azurewebsites.net/swagger`
- **Chat Interface**: `https://app-expensemgmt-[unique].azurewebsites.net/Chat`
