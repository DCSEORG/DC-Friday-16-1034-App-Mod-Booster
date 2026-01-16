using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;

namespace ExpenseManagementApp.Pages;

public class ListExpensesModel : PageModel
{
    private readonly DatabaseService _databaseService;
    
    public List<Expense> Expenses { get; set; } = new();
    public List<ExpenseCategory> Categories { get; set; } = new();
    public List<ExpenseStatus> Statuses { get; set; } = new();
    public bool HasError { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public int? StatusId { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    public ListExpensesModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Categories = await _databaseService.GetCategoriesAsync();
            Statuses = await _databaseService.GetStatusesAsync();
            Expenses = await _databaseService.GetExpensesAsync(
                userId: 1, // Default user
                statusId: StatusId,
                categoryId: CategoryId,
                startDate: StartDate,
                endDate: EndDate
            );
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to load expenses. {ex.Message} [ListExpenses.cshtml.cs:48]";
            Expenses = new List<Expense>();
            Categories = new List<ExpenseCategory>();
            Statuses = new List<ExpenseStatus>();
        }
    }
}
