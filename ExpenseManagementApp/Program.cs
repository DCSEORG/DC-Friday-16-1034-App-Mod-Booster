using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<ChatService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Expense Management API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Management API v1"));
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

// API Endpoints
var apiGroup = app.MapGroup("/api");

// GET /api/expenses
apiGroup.MapGet("/expenses", async (
    [FromServices] DatabaseService db,
    [FromQuery] int? userId,
    [FromQuery] int? statusId,
    [FromQuery] int? categoryId,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate) =>
{
    try
    {
        var expenses = await db.GetExpensesAsync(userId, statusId, categoryId, startDate, endDate);
        return Results.Ok(expenses);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: $"{ex.Message} [Program.cs:43]", statusCode: 500);
    }
})
.WithName("GetExpenses");

// GET /api/expenses/{id}
apiGroup.MapGet("/expenses/{id}", async (
    [FromServices] DatabaseService db,
    int id) =>
{
    try
    {
        var expense = await db.GetExpenseByIdAsync(id);
        return expense != null ? Results.Ok(expense) : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: $"{ex.Message} [Program.cs:60]", statusCode: 500);
    }
})
.WithName("GetExpenseById");

// POST /api/expenses
apiGroup.MapPost("/expenses", async (
    [FromServices] DatabaseService db,
    [FromBody] CreateExpenseRequest request) =>
{
    try
    {
        var expenseId = await db.CreateExpenseAsync(request);
        var expense = await db.GetExpenseByIdAsync(expenseId);
        return Results.Created($"/api/expenses/{expenseId}", expense);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: $"{ex.Message} [Program.cs:79]", statusCode: 500);
    }
})
.WithName("CreateExpense");

// PUT /api/expenses/{id}
apiGroup.MapPut("/expenses/{id}", async (
    [FromServices] DatabaseService db,
    int id,
    [FromBody] UpdateExpenseRequest request) =>
{
    try
    {
        var rowsAffected = await db.UpdateExpenseAsync(id, request);
        if (rowsAffected == 0)
            return Results.NotFound();
        
        var expense = await db.GetExpenseByIdAsync(id);
        return Results.Ok(expense);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: $"{ex.Message} [Program.cs:102]", statusCode: 500);
    }
})
.WithName("UpdateExpense");

// POST /api/expenses/{id}/submit
apiGroup.MapPost("/expenses/{id}/submit", async (
    [FromServices] DatabaseService db,
    int id) =>
{
    try
    {
        var rowsAffected = await db.SubmitExpenseAsync(id);
        if (rowsAffected == 0)
            return Results.NotFound();
        
        var expense = await db.GetExpenseByIdAsync(id);
        return Results.Ok(expense);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: $"{ex.Message} [Program.cs:125]", statusCode: 500);
    }
})
.WithName("SubmitExpense");

// POST /api/expenses/{id}/approve
apiGroup.MapPost("/expenses/{id}/approve", async (
    [FromServices] DatabaseService db,
    int id,
    [FromBody] ExpenseActionRequest request) =>
{
    try
    {
        var rowsAffected = await db.ApproveExpenseAsync(id, request.ReviewerId);
        if (rowsAffected == 0)
            return Results.NotFound();
        
        var expense = await db.GetExpenseByIdAsync(id);
        return Results.Ok(expense);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: $"{ex.Message} [Program.cs:149]", statusCode: 500);
    }
})
.WithName("ApproveExpense");

// POST /api/expenses/{id}/reject
apiGroup.MapPost("/expenses/{id}/reject", async (
    [FromServices] DatabaseService db,
    int id,
    [FromBody] ExpenseActionRequest request) =>
{
    try
    {
        var rowsAffected = await db.RejectExpenseAsync(id, request.ReviewerId);
        if (rowsAffected == 0)
            return Results.NotFound();
        
        var expense = await db.GetExpenseByIdAsync(id);
        return Results.Ok(expense);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: $"{ex.Message} [Program.cs:170]", statusCode: 500);
    }
})
.WithName("RejectExpense");

// DELETE /api/expenses/{id}
apiGroup.MapDelete("/expenses/{id}", async (
    [FromServices] DatabaseService db,
    int id) =>
{
    try
    {
        var rowsAffected = await db.DeleteExpenseAsync(id);
        return rowsAffected > 0 ? Results.NoContent() : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: $"{ex.Message} [Program.cs:187]", statusCode: 500);
    }
})
.WithName("DeleteExpense");

// GET /api/categories
apiGroup.MapGet("/categories", async ([FromServices] DatabaseService db) =>
{
    try
    {
        var categories = await db.GetCategoriesAsync();
        return Results.Ok(categories);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: $"{ex.Message} [Program.cs:202]", statusCode: 500);
    }
})
.WithName("GetCategories");

// GET /api/statuses
apiGroup.MapGet("/statuses", async ([FromServices] DatabaseService db) =>
{
    try
    {
        var statuses = await db.GetStatusesAsync();
        return Results.Ok(statuses);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: $"{ex.Message} [Program.cs:217]", statusCode: 500);
    }
})
.WithName("GetStatuses");

// POST /api/chat
apiGroup.MapPost("/chat", async (
    [FromServices] ChatService chatService,
    [FromBody] ChatRequest request) =>
{
    try
    {
        var response = await chatService.ProcessChatAsync(request);
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: $"{ex.Message} [Program.cs:234]", statusCode: 500);
    }
})
.WithName("Chat");

app.Run();
