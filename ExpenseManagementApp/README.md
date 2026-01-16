# Expense Management Application

A modern ASP.NET Core 8 Razor Pages application for managing employee expenses with Azure OpenAI integration.

## Features

- **Add Expense**: Create and submit expense claims with category, amount, date, and description
- **List Expenses**: View and filter expenses by status, category, and date range
- **Approve Expenses**: Manager view to approve or reject pending expenses
- **AI Chat Assistant**: Interact with Azure OpenAI to manage expenses via natural language
- **REST API**: Full CRUD operations with Swagger documentation

## Project Structure

```
ExpenseManagementApp/
├── Models/                 # Data models (Expense, Category, Status, User)
├── Services/              # Business logic services
│   ├── DatabaseService.cs # All database operations using stored procedures
│   └── ChatService.cs     # Azure OpenAI integration
├── Pages/                 # Razor Pages
│   ├── Index.cshtml       # Dashboard with stats and recent expenses
│   ├── AddExpense.cshtml  # Create new expense form
│   ├── ListExpenses.cshtml # Expense list with filters
│   ├── ApproveExpenses.cshtml # Manager approval interface
│   └── Chat.cshtml        # AI chat interface
├── wwwroot/               # Static files
│   ├── css/site.css       # Modern UI styles
│   └── js/site.js         # Client-side functionality
├── Program.cs             # API endpoints and app configuration
└── appsettings.json       # Configuration

```

## Configuration

### Database Connection (Managed Identity)

Update `appsettings.json` with your Azure SQL Database details:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net;Database=your-database;Authentication=Active Directory Managed Identity;User Id=your-client-id;"
  }
}
```

### Azure OpenAI

Update `appsettings.json` with your Azure OpenAI resource:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "gpt-4o",
    "ApiVersion": "2024-08-01-preview"
  }
}
```

## Running the Application

### Prerequisites

- .NET 8 SDK
- Azure SQL Database with schema and stored procedures deployed
- Azure OpenAI resource (optional, works with dummy data otherwise)

### Build and Run

```bash
cd ExpenseManagementApp
dotnet restore
dotnet build
dotnet run
```

The application will be available at:
- UI: https://localhost:5001 or http://localhost:5000
- API: https://localhost:5001/swagger

## API Endpoints

### Expenses
- `GET /api/expenses` - Get all expenses with optional filters
- `GET /api/expenses/{id}` - Get expense by ID
- `POST /api/expenses` - Create new expense
- `PUT /api/expenses/{id}` - Update expense
- `POST /api/expenses/{id}/submit` - Submit expense for approval
- `POST /api/expenses/{id}/approve` - Approve expense
- `POST /api/expenses/{id}/reject` - Reject expense
- `DELETE /api/expenses/{id}` - Delete expense

### Reference Data
- `GET /api/categories` - Get all categories
- `GET /api/statuses` - Get all statuses

### Chat
- `POST /api/chat` - Send chat message to AI assistant

## Database Operations

All database operations use stored procedures from `stored-procedures.sql`:
- `GetExpenses` - Retrieve expenses with filters
- `GetExpenseById` - Get single expense
- `CreateExpense` - Create new expense
- `UpdateExpense` - Update existing expense
- `SubmitExpense` - Submit for approval
- `ApproveExpense` - Approve expense
- `RejectExpense` - Reject expense
- `DeleteExpense` - Delete expense
- `GetCategories` - Get categories
- `GetStatuses` - Get statuses
- `GetPendingExpensesForReview` - Get pending expenses for manager

## Error Handling

The application handles database and Azure OpenAI connectivity issues gracefully:
- Falls back to dummy data when database is not configured
- Displays error messages with file location for debugging
- Shows configuration notices in the UI header

## UI Design

Modern, clean interface with:
- Responsive grid layout
- Status badges (Draft, Submitted, Approved, Rejected)
- Filter controls for expense lists
- Interactive chat interface
- Stats dashboards
- British Pound (£) currency formatting

## Dependencies

- **Azure.AI.OpenAI** (2.1.0) - Azure OpenAI client
- **Azure.Identity** (1.13.1) - Managed Identity authentication
- **Microsoft.Data.SqlClient** (5.2.2) - SQL Server data access
- **Swashbuckle.AspNetCore** (6.9.0) - Swagger/OpenAPI

## Security

- Uses Azure Managed Identity for database authentication
- Uses DefaultAzureCredential for Azure OpenAI authentication
- All database operations through stored procedures (no direct SQL)
- Input validation on forms

## License

See LICENSE file in repository root.
