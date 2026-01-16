using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;

namespace ExpenseManagementApp.Pages;

public class ApproveExpensesModel : PageModel
{
    private readonly DatabaseService _databaseService;
    
    public List<Expense> PendingExpenses { get; set; } = new();
    public List<Expense> RecentlyReviewedExpenses { get; set; } = new();
    public bool HasError { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public ApproveExpensesModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task OnGetAsync()
    {
        try
        {
            PendingExpenses = await _databaseService.GetPendingExpensesForReviewAsync(managerId: 2);
            
            var allExpenses = await _databaseService.GetExpensesAsync();
            RecentlyReviewedExpenses = allExpenses
                .Where(e => e.StatusName == "Approved" || e.StatusName == "Rejected")
                .OrderByDescending(e => e.ReviewedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to load expenses for review. {ex.Message} [ApproveExpenses.cshtml.cs:35]";
            PendingExpenses = new List<Expense>();
            RecentlyReviewedExpenses = new List<Expense>();
        }
    }
}
