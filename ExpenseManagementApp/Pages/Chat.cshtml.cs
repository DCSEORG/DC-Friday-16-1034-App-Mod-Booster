using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagementApp.Pages;

public class ChatModel : PageModel
{
    private readonly IConfiguration _configuration;
    
    public bool HasError { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public ChatModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnGet()
    {
        var endpoint = _configuration["AzureOpenAI:Endpoint"];
        if (string.IsNullOrEmpty(endpoint) || endpoint.Contains("{resource-name}"))
        {
            HasError = true;
            ErrorMessage = "Azure OpenAI is not configured. The chat will work in limited mode. Please configure Azure OpenAI in appsettings.json. [Chat.cshtml.cs:23]";
        }
    }
}
