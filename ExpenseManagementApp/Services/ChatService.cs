using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagementApp.Models;
using System.Text.Json;
using OpenAI.Chat;

namespace ExpenseManagementApp.Services;

public class ChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly DatabaseService _databaseService;
    private readonly bool _useAzureOpenAI;

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger, DatabaseService databaseService)
    {
        _configuration = configuration;
        _logger = logger;
        _databaseService = databaseService;
        
        var endpoint = _configuration["AzureOpenAI:Endpoint"];
        _useAzureOpenAI = !string.IsNullOrEmpty(endpoint) && !endpoint.Contains("{resource-name}");
    }

    private ChatClient GetChatClient()
    {
        if (!_useAzureOpenAI)
        {
            throw new InvalidOperationException("Azure OpenAI not configured. [ChatService.cs:25]");
        }

        var endpoint = _configuration["AzureOpenAI:Endpoint"];
        var deploymentName = _configuration["AzureOpenAI:DeploymentName"];
        
        var credential = new DefaultAzureCredential();
        var client = new AzureOpenAIClient(new Uri(endpoint!), credential);
        return client.GetChatClient(deploymentName);
    }

    public async Task<ChatResponse> ProcessChatAsync(ChatRequest request)
    {
        if (!_useAzureOpenAI)
        {
            return new ChatResponse
            {
                Message = "Azure OpenAI is not configured. Please set up the Azure OpenAI endpoint in appsettings.json. [ChatService.cs:44]"
            };
        }

        try
        {
            var chatClient = GetChatClient();
            
            var messages = new List<OpenAI.Chat.ChatMessage>();
            messages.Add(new SystemChatMessage(GetSystemPrompt()));
            
            foreach (var msg in request.Messages)
            {
                if (msg.Role.ToLower() == "user")
                    messages.Add(new UserChatMessage(msg.Content));
                else if (msg.Role.ToLower() == "assistant")
                    messages.Add(new AssistantChatMessage(msg.Content));
            }

            var chatOptions = new ChatCompletionOptions();
            chatOptions.Tools.Add(ChatTool.CreateFunctionTool(
                "get_expenses",
                "Get list of expenses with optional filters",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "statusName": {"type": "string", "description": "Filter by status: Draft, Submitted, Approved, Rejected"},
                        "categoryName": {"type": "string", "description": "Filter by category name"}
                    }
                }
                """)
            ));

            chatOptions.Tools.Add(ChatTool.CreateFunctionTool(
                "get_expense_by_id",
                "Get details of a specific expense by ID",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expenseId": {"type": "integer", "description": "The expense ID"}
                    },
                    "required": ["expenseId"]
                }
                """)
            ));

            chatOptions.Tools.Add(ChatTool.CreateFunctionTool(
                "create_expense",
                "Create a new expense",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "categoryName": {"type": "string", "description": "Category: Travel, Meals, Supplies, Accommodation, Other"},
                        "amount": {"type": "number", "description": "Amount in GBP"},
                        "description": {"type": "string", "description": "Expense description"},
                        "expenseDate": {"type": "string", "description": "Date in YYYY-MM-DD format"}
                    },
                    "required": ["categoryName", "amount", "expenseDate"]
                }
                """)
            ));

            chatOptions.Tools.Add(ChatTool.CreateFunctionTool(
                "submit_expense",
                "Submit an expense for approval",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expenseId": {"type": "integer", "description": "The expense ID to submit"}
                    },
                    "required": ["expenseId"]
                }
                """)
            ));

            chatOptions.Tools.Add(ChatTool.CreateFunctionTool(
                "approve_expense",
                "Approve an expense (manager only)",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expenseId": {"type": "integer", "description": "The expense ID to approve"}
                    },
                    "required": ["expenseId"]
                }
                """)
            ));

            chatOptions.Tools.Add(ChatTool.CreateFunctionTool(
                "reject_expense",
                "Reject an expense (manager only)",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expenseId": {"type": "integer", "description": "The expense ID to reject"}
                    },
                    "required": ["expenseId"]
                }
                """)
            ));

            var completion = await chatClient.CompleteChatAsync(messages, chatOptions);
            var responseMessage = completion.Value.Content[0].Text;
            
            if (completion.Value.FinishReason == ChatFinishReason.ToolCalls && completion.Value.ToolCalls.Count > 0)
            {
                var toolCall = completion.Value.ToolCalls[0];
                var functionName = toolCall.FunctionName;
                var functionArgs = toolCall.FunctionArguments;
                
                var result = await ExecuteFunctionAsync(functionName, functionArgs, request.UserId);
                
                return new ChatResponse
                {
                    Message = $"Executed {functionName} successfully.",
                    FunctionCalled = functionName,
                    FunctionResult = result
                };
            }

            return new ChatResponse
            {
                Message = responseMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request [ChatService.cs:179]");
            return new ChatResponse
            {
                Message = $"I encountered an error: {ex.Message} [ChatService.cs:182]"
            };
        }
    }

    private async Task<object?> ExecuteFunctionAsync(string functionName, BinaryData functionArgs, int userId)
    {
        try
        {
            var argsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(functionArgs.ToString());
            
            switch (functionName)
            {
                case "get_expenses":
                    var statusName = argsDict?.ContainsKey("statusName") == true ? argsDict["statusName"].GetString() : null;
                    var categoryName = argsDict?.ContainsKey("categoryName") == true ? argsDict["categoryName"].GetString() : null;
                    
                    var statuses = await _databaseService.GetStatusesAsync();
                    var categories = await _databaseService.GetCategoriesAsync();
                    
                    int? statusId = statusName != null ? statuses.FirstOrDefault(s => s.StatusName.Equals(statusName, StringComparison.OrdinalIgnoreCase))?.StatusId : null;
                    int? categoryId = categoryName != null ? categories.FirstOrDefault(c => c.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase))?.CategoryId : null;
                    
                    return await _databaseService.GetExpensesAsync(userId, statusId, categoryId);

                case "get_expense_by_id":
                    var expenseId = argsDict!["expenseId"].GetInt32();
                    return await _databaseService.GetExpenseByIdAsync(expenseId);

                case "create_expense":
                    var catName = argsDict!["categoryName"].GetString();
                    var amount = argsDict["amount"].GetDecimal();
                    var description = argsDict.ContainsKey("description") ? argsDict["description"].GetString() : null;
                    var expenseDate = DateTime.Parse(argsDict["expenseDate"].GetString()!);
                    
                    var cats = await _databaseService.GetCategoriesAsync();
                    var category = cats.FirstOrDefault(c => c.CategoryName.Equals(catName, StringComparison.OrdinalIgnoreCase));
                    
                    if (category == null)
                        throw new InvalidOperationException($"Category '{catName}' not found");
                    
                    var createRequest = new CreateExpenseRequest
                    {
                        UserId = userId,
                        CategoryId = category.CategoryId,
                        Amount = amount,
                        ExpenseDate = expenseDate,
                        Description = description
                    };
                    
                    var newExpenseId = await _databaseService.CreateExpenseAsync(createRequest);
                    return await _databaseService.GetExpenseByIdAsync(newExpenseId);

                case "submit_expense":
                    var submitExpenseId = argsDict!["expenseId"].GetInt32();
                    await _databaseService.SubmitExpenseAsync(submitExpenseId);
                    return await _databaseService.GetExpenseByIdAsync(submitExpenseId);

                case "approve_expense":
                    var approveExpenseId = argsDict!["expenseId"].GetInt32();
                    await _databaseService.ApproveExpenseAsync(approveExpenseId, userId);
                    return await _databaseService.GetExpenseByIdAsync(approveExpenseId);

                case "reject_expense":
                    var rejectExpenseId = argsDict!["expenseId"].GetInt32();
                    await _databaseService.RejectExpenseAsync(rejectExpenseId, userId);
                    return await _databaseService.GetExpenseByIdAsync(rejectExpenseId);

                default:
                    throw new InvalidOperationException($"Unknown function: {functionName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName} [ChatService.cs:260]", functionName);
            throw;
        }
    }

    private string GetSystemPrompt()
    {
        return @"You are a helpful expense management assistant. You can help users:
- View their expenses (draft, submitted, approved, rejected)
- Create new expenses
- Submit expenses for approval
- Approve or reject expenses (if they are a manager)

When creating expenses, always confirm the details before executing.
Always use British Pound (£) currency format.
Be conversational and helpful.";
    }
}
