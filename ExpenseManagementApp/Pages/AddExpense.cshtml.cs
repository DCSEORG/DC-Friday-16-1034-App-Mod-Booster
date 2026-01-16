using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;

namespace ExpenseManagementApp.Pages;

public class AddExpenseModel : PageModel
{
    private readonly DatabaseService _databaseService;
    
    public List<ExpenseCategory> Categories { get; set; } = new();
    public bool HasError { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public int CategoryId { get; set; }
    
    [BindProperty]
    public decimal Amount { get; set; }
    
    [BindProperty]
    public DateTime ExpenseDate { get; set; }
    
    [BindProperty]
    public string? Description { get; set; }

    public AddExpenseModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Categories = await _databaseService.GetCategoriesAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to load categories. {ex.Message} [AddExpense.cshtml.cs:43]";
            Categories = new List<ExpenseCategory>();
        }
    }

    public async Task<IActionResult> OnPostAsync(bool submitForApproval = false)
    {
        try
        {
            Categories = await _databaseService.GetCategoriesAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var request = new CreateExpenseRequest
            {
                UserId = 1, // Default user
                CategoryId = CategoryId,
                Amount = Amount,
                ExpenseDate = ExpenseDate,
                Description = Description
            };

            var expenseId = await _databaseService.CreateExpenseAsync(request);

            if (submitForApproval)
            {
                await _databaseService.SubmitExpenseAsync(expenseId);
                SuccessMessage = "Expense created and submitted for approval!";
            }
            else
            {
                SuccessMessage = "Expense saved as draft!";
            }

            // Clear form
            CategoryId = 0;
            Amount = 0;
            ExpenseDate = DateTime.Now;
            Description = null;

            return Page();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to create expense. {ex.Message} [AddExpense.cshtml.cs:88]";
            return Page();
        }
    }
}
