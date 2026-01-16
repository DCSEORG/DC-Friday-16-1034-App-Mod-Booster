-- Stored Procedures for Expense Management System
-- All database operations should use these stored procedures
-- No direct T-SQL in application code

SET NOCOUNT ON;
GO

-- =============================================
-- Expense Procedures
-- =============================================

-- Get all expenses with filters
CREATE OR ALTER PROCEDURE dbo.GetExpenses
    @UserId INT = NULL,
    @StatusId INT = NULL,
    @CategoryId INT = NULL,
    @StartDate DATE = NULL,
    @EndDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        e.Currency,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountDecimal,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        r.UserName AS ReviewerName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
    WHERE (@UserId IS NULL OR e.UserId = @UserId)
        AND (@StatusId IS NULL OR e.StatusId = @StatusId)
        AND (@CategoryId IS NULL OR e.CategoryId = @CategoryId)
        AND (@StartDate IS NULL OR e.ExpenseDate >= @StartDate)
        AND (@EndDate IS NULL OR e.ExpenseDate <= @EndDate)
    ORDER BY e.ExpenseDate DESC, e.CreatedAt DESC;
END
GO

-- Get expense by ID
CREATE OR ALTER PROCEDURE dbo.GetExpenseById
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        e.Currency,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountDecimal,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        r.UserName AS ReviewerName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
    WHERE e.ExpenseId = @ExpenseId;
END
GO

-- Create new expense
CREATE OR ALTER PROCEDURE dbo.CreateExpense
    @UserId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @StatusId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Default to Draft status if not specified
    IF @StatusId IS NULL
        SET @StatusId = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft');
    
    INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, CreatedAt)
    VALUES (@UserId, @CategoryId, @StatusId, @AmountMinor, 'GBP', @ExpenseDate, @Description, SYSUTCDATETIME());
    
    SELECT SCOPE_IDENTITY() AS ExpenseId;
END
GO

-- Update expense
CREATE OR ALTER PROCEDURE dbo.UpdateExpense
    @ExpenseId INT,
    @CategoryId INT = NULL,
    @AmountMinor INT = NULL,
    @ExpenseDate DATE = NULL,
    @Description NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Expenses
    SET 
        CategoryId = ISNULL(@CategoryId, CategoryId),
        AmountMinor = ISNULL(@AmountMinor, AmountMinor),
        ExpenseDate = ISNULL(@ExpenseDate, ExpenseDate),
        Description = ISNULL(@Description, Description)
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Submit expense
CREATE OR ALTER PROCEDURE dbo.SubmitExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SubmittedStatusId INT = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted');
    
    UPDATE dbo.Expenses
    SET 
        StatusId = @SubmittedStatusId,
        SubmittedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Approve expense
CREATE OR ALTER PROCEDURE dbo.ApproveExpense
    @ExpenseId INT,
    @ReviewerId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ApprovedStatusId INT = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Approved');
    
    UPDATE dbo.Expenses
    SET 
        StatusId = @ApprovedStatusId,
        ReviewedBy = @ReviewerId,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Reject expense
CREATE OR ALTER PROCEDURE dbo.RejectExpense
    @ExpenseId INT,
    @ReviewerId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RejectedStatusId INT = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Rejected');
    
    UPDATE dbo.Expenses
    SET 
        StatusId = @RejectedStatusId,
        ReviewedBy = @ReviewerId,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Delete expense
CREATE OR ALTER PROCEDURE dbo.DeleteExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM dbo.Expenses WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- Category Procedures
-- =============================================

-- Get all active categories
CREATE OR ALTER PROCEDURE dbo.GetCategories
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT CategoryId, CategoryName, IsActive
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END
GO

-- =============================================
-- Status Procedures
-- =============================================

-- Get all statuses
CREATE OR ALTER PROCEDURE dbo.GetStatuses
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT StatusId, StatusName
    FROM dbo.ExpenseStatus
    ORDER BY StatusId;
END
GO

-- =============================================
-- User Procedures
-- =============================================

-- Get all users
CREATE OR ALTER PROCEDURE dbo.GetUsers
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.ManagerId,
        m.UserName AS ManagerName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    LEFT JOIN dbo.Users m ON u.ManagerId = m.UserId
    WHERE u.IsActive = 1
    ORDER BY u.UserName;
END
GO

-- Get user by ID
CREATE OR ALTER PROCEDURE dbo.GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.ManagerId,
        m.UserName AS ManagerName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    LEFT JOIN dbo.Users m ON u.ManagerId = m.UserId
    WHERE u.UserId = @UserId;
END
GO

-- Get pending expenses for manager review
CREATE OR ALTER PROCEDURE dbo.GetPendingExpensesForReview
    @ManagerId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        e.Currency,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountDecimal,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE s.StatusName = 'Submitted'
        AND (@ManagerId IS NULL OR u.ManagerId = @ManagerId)
    ORDER BY e.SubmittedAt ASC;
END
GO

-- =============================================
-- Reporting Procedures
-- =============================================

-- Get expense summary by status
CREATE OR ALTER PROCEDURE dbo.GetExpenseSummaryByStatus
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        s.StatusName,
        COUNT(*) AS ExpenseCount,
        SUM(e.AmountMinor) AS TotalAmountMinor,
        CAST(SUM(e.AmountMinor) / 100.0 AS DECIMAL(10,2)) AS TotalAmount
    FROM dbo.Expenses e
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE (@UserId IS NULL OR e.UserId = @UserId)
    GROUP BY s.StatusName, s.StatusId
    ORDER BY s.StatusId;
END
GO

-- Get expense summary by category
CREATE OR ALTER PROCEDURE dbo.GetExpenseSummaryByCategory
    @UserId INT = NULL,
    @StartDate DATE = NULL,
    @EndDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        c.CategoryName,
        COUNT(*) AS ExpenseCount,
        SUM(e.AmountMinor) AS TotalAmountMinor,
        CAST(SUM(e.AmountMinor) / 100.0 AS DECIMAL(10,2)) AS TotalAmount
    FROM dbo.Expenses e
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    WHERE (@UserId IS NULL OR e.UserId = @UserId)
        AND (@StartDate IS NULL OR e.ExpenseDate >= @StartDate)
        AND (@EndDate IS NULL OR e.ExpenseDate <= @EndDate)
    GROUP BY c.CategoryName
    ORDER BY TotalAmountMinor DESC;
END
GO

PRINT 'All stored procedures created successfully!';
GO
