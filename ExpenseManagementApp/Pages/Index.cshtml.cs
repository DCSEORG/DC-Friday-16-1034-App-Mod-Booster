using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;

namespace ExpenseManagementApp.Pages;

public class IndexModel : PageModel
{
    private readonly DatabaseService _databaseService;
    
    public List<Expense> Expenses { get; set; } = new();
    public bool HasError { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public IndexModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Expenses = await _databaseService.GetExpensesAsync(userId: 1);
        }
        catch (InvalidOperationException ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            Expenses = new List<Expense>();
        }
    }
}
