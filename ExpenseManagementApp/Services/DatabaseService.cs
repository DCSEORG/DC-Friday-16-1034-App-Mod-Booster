using Microsoft.Data.SqlClient;
using ExpenseManagementApp.Models;
using System.Data;

namespace ExpenseManagementApp.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;
    private readonly bool _useDummyData;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        _logger = logger;
        _useDummyData = string.IsNullOrEmpty(_connectionString) || 
                        _connectionString.Contains("{server}") || 
                        _connectionString.Contains("{database}");
    }

    private SqlConnection GetConnection()
    {
        if (_useDummyData)
        {
            throw new InvalidOperationException("Database not configured. Using dummy data. [DatabaseService.cs:23]");
        }
        return new SqlConnection(_connectionString);
    }

    public async Task<List<Expense>> GetExpensesAsync(int? userId = null, int? statusId = null, 
        int? categoryId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (_useDummyData) return GetDummyExpenses();

        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("dbo.GetExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@StatusId", (object?)statusId ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", (object?)categoryId ?? DBNull.Value);
            command.Parameters.AddWithValue("@StartDate", (object?)startDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@EndDate", (object?)endDate ?? DBNull.Value);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            var expenses = new List<Expense>();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching expenses from database [DatabaseService.cs:58]");
            return GetDummyExpenses();
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        if (_useDummyData) return GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == expenseId);

        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("dbo.GetExpenseById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return MapExpense(reader);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching expense by ID from database [DatabaseService.cs:85]");
            return GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == expenseId);
        }
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        if (_useDummyData) return new Random().Next(1000, 9999);

        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("dbo.CreateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense in database [DatabaseService.cs:116]");
            throw new InvalidOperationException($"Failed to create expense. [DatabaseService.cs:117] Error: {ex.Message}", ex);
        }
    }

    public async Task<int> UpdateExpenseAsync(int expenseId, UpdateExpenseRequest request)
    {
        if (_useDummyData) return 1;

        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("dbo.UpdateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@CategoryId", (object?)request.CategoryId ?? DBNull.Value);
            command.Parameters.AddWithValue("@AmountMinor", request.Amount.HasValue ? (int)(request.Amount.Value * 100) : DBNull.Value);
            command.Parameters.AddWithValue("@ExpenseDate", (object?)request.ExpenseDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense in database [DatabaseService.cs:147]");
            throw new InvalidOperationException($"Failed to update expense. [DatabaseService.cs:148] Error: {ex.Message}", ex);
        }
    }

    public async Task<int> SubmitExpenseAsync(int expenseId)
    {
        if (_useDummyData) return 1;

        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("dbo.SubmitExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense in database [DatabaseService.cs:172]");
            throw new InvalidOperationException($"Failed to submit expense. [DatabaseService.cs:173] Error: {ex.Message}", ex);
        }
    }

    public async Task<int> ApproveExpenseAsync(int expenseId, int reviewerId)
    {
        if (_useDummyData) return 1;

        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("dbo.ApproveExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense in database [DatabaseService.cs:198]");
            throw new InvalidOperationException($"Failed to approve expense. [DatabaseService.cs:199] Error: {ex.Message}", ex);
        }
    }

    public async Task<int> RejectExpenseAsync(int expenseId, int reviewerId)
    {
        if (_useDummyData) return 1;

        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("dbo.RejectExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense in database [DatabaseService.cs:224]");
            throw new InvalidOperationException($"Failed to reject expense. [DatabaseService.cs:225] Error: {ex.Message}", ex);
        }
    }

    public async Task<int> DeleteExpenseAsync(int expenseId)
    {
        if (_useDummyData) return 1;

        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("dbo.DeleteExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense in database [DatabaseService.cs:250]");
            throw new InvalidOperationException($"Failed to delete expense. [DatabaseService.cs:251] Error: {ex.Message}", ex);
        }
    }

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        if (_useDummyData) return GetDummyCategories();

        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("dbo.GetCategories", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            var categories = new List<ExpenseCategory>();
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching categories from database [DatabaseService.cs:283]");
            return GetDummyCategories();
        }
    }

    public async Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        if (_useDummyData) return GetDummyStatuses();

        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("dbo.GetStatuses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            var statuses = new List<ExpenseStatus>();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
                });
            }
            return statuses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching statuses from database [DatabaseService.cs:315]");
            return GetDummyStatuses();
        }
    }

    public async Task<List<Expense>> GetPendingExpensesForReviewAsync(int? managerId = null)
    {
        if (_useDummyData) return GetDummyExpenses().Where(e => e.StatusName == "Submitted").ToList();

        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("dbo.GetPendingExpensesForReview", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ManagerId", (object?)managerId ?? DBNull.Value);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            var expenses = new List<Expense>();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending expenses from database [DatabaseService.cs:348]");
            return GetDummyExpenses().Where(e => e.StatusName == "Submitted").ToList();
        }
    }

    private Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            AmountDecimal = reader.GetDecimal(reader.GetOrdinal("AmountDecimal")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewerName = reader.IsDBNull(reader.GetOrdinal("ReviewerName")) ? null : reader.GetString(reader.GetOrdinal("ReviewerName")),
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 2540,
                Currency = "GBP",
                AmountDecimal = 25.40m,
                ExpenseDate = DateTime.Now.AddDays(-10),
                Description = "Taxi from airport to client site",
                SubmittedAt = DateTime.Now.AddDays(-9),
                CreatedAt = DateTime.Now.AddDays(-10)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 2,
                CategoryName = "Meals",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 1425,
                Currency = "GBP",
                AmountDecimal = 14.25m,
                ExpenseDate = DateTime.Now.AddDays(-30),
                Description = "Client lunch meeting",
                SubmittedAt = DateTime.Now.AddDays(-29),
                ReviewedBy = 2,
                ReviewerName = "Bob Manager",
                ReviewedAt = DateTime.Now.AddDays(-28),
                CreatedAt = DateTime.Now.AddDays(-30)
            },
            new Expense
            {
                ExpenseId = 3,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 3,
                CategoryName = "Supplies",
                StatusId = 1,
                StatusName = "Draft",
                AmountMinor = 799,
                Currency = "GBP",
                AmountDecimal = 7.99m,
                ExpenseDate = DateTime.Now.AddDays(-2),
                Description = "Office stationery",
                CreatedAt = DateTime.Now.AddDays(-2)
            },
            new Expense
            {
                ExpenseId = 4,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 4,
                CategoryName = "Accommodation",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 12300,
                Currency = "GBP",
                AmountDecimal = 123.00m,
                ExpenseDate = DateTime.Now.AddDays(-60),
                Description = "Hotel during client visit",
                SubmittedAt = DateTime.Now.AddDays(-59),
                ReviewedBy = 2,
                ReviewerName = "Bob Manager",
                ReviewedAt = DateTime.Now.AddDays(-58),
                CreatedAt = DateTime.Now.AddDays(-60)
            }
        };
    }

    private List<ExpenseCategory> GetDummyCategories()
    {
        return new List<ExpenseCategory>
        {
            new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new ExpenseCategory { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new ExpenseCategory { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new ExpenseCategory { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new ExpenseCategory { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    private List<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
            new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
            new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
            new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
        };
    }
}
