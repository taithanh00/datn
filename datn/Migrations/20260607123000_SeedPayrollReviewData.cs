using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    public partial class SeedPayrollReviewData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @CurrentMonth int = 6;
DECLARE @CurrentYear int = 2026;
DECLARE @PreviousMonth int = 5;
DECLARE @PreviousYear int = 2026;
DECLARE @CurrentStandardWorkingDays int = 26;
DECLARE @PreviousStandardWorkingDays int = 26;
DECLARE @SeedNote nvarchar(255) = N'SeedPayrollReviewData';

IF NOT EXISTS (SELECT 1 FROM PayrollPeriods WHERE [Month] = @CurrentMonth AND [Year] = @CurrentYear)
BEGIN
    INSERT INTO PayrollPeriods ([Month], [Year], IsLocked, LockedAtUtc)
    VALUES (@CurrentMonth, @CurrentYear, 0, NULL);
END;

IF NOT EXISTS (SELECT 1 FROM PayrollPeriods WHERE [Month] = @PreviousMonth AND [Year] = @PreviousYear)
BEGIN
    INSERT INTO PayrollPeriods ([Month], [Year], IsLocked, LockedAtUtc)
    VALUES (@PreviousMonth, @PreviousYear, 1, SYSUTCDATETIME());
END;
ELSE
BEGIN
    UPDATE PayrollPeriods
    SET IsLocked = 1,
        LockedAtUtc = COALESCE(LockedAtUtc, SYSUTCDATETIME())
    WHERE [Month] = @PreviousMonth AND [Year] = @PreviousYear;
END;

DECLARE @CurrentPeriodId int = (
    SELECT TOP (1) Id FROM PayrollPeriods WHERE [Month] = @CurrentMonth AND [Year] = @CurrentYear ORDER BY Id
);
DECLARE @PreviousPeriodId int = (
    SELECT TOP (1) Id FROM PayrollPeriods WHERE [Month] = @PreviousMonth AND [Year] = @PreviousYear ORDER BY Id
);

;WITH TeacherScope AS
(
    SELECT e.Id AS EmployeeId
    FROM Employees e
    INNER JOIN Accounts a ON a.Id = e.AccountId
    INNER JOIN Roles r ON r.Id = a.RoleId
    WHERE r.Name = N'Employee'
),
SeedDates AS
(
    SELECT CAST(DATEFROMPARTS(@CurrentYear, @CurrentMonth, 1) AS date) AS WorkDate UNION ALL
    SELECT CAST(DATEFROMPARTS(@CurrentYear, @CurrentMonth, 2) AS date) UNION ALL
    SELECT CAST(DATEFROMPARTS(@CurrentYear, @CurrentMonth, 3) AS date) UNION ALL
    SELECT CAST(DATEFROMPARTS(@CurrentYear, @CurrentMonth, 4) AS date) UNION ALL
    SELECT CAST(DATEFROMPARTS(@CurrentYear, @CurrentMonth, 5) AS date) UNION ALL
    SELECT CAST(DATEFROMPARTS(@CurrentYear, @CurrentMonth, 6) AS date)
)
INSERT INTO WorkAttendances
(
    EmployeeId,
    [Date],
    CheckInAtUtc,
    CheckOutAtUtc,
    WorkedMinutes,
    WorkUnit,
    IsLate,
    PenaltyAmount,
    [Status],
    Note,
    ReviewedByEmployeeId,
    ReviewedAtUtc,
    ReviewNote
)
SELECT
    t.EmployeeId,
    d.WorkDate,
    DATEADD(HOUR, 1, CAST(d.WorkDate AS datetime2)),
    DATEADD(HOUR, 9, CAST(d.WorkDate AS datetime2)),
    480,
    CAST(1.00 AS decimal(18,2)),
    0,
    CAST(0 AS decimal(18,2)),
    N'Approved',
    @SeedNote,
    NULL,
    SYSUTCDATETIME(),
    N'Dữ liệu mẫu để kiểm tra tính lương'
FROM TeacherScope t
CROSS JOIN SeedDates d
WHERE NOT EXISTS
(
    SELECT 1
    FROM WorkAttendances wa
    WHERE wa.EmployeeId = t.EmployeeId
      AND wa.[Date] = d.WorkDate
);

;WITH TeacherScope AS
(
    SELECT
        e.Id AS EmployeeId,
        COALESCE(e.BaseSalary, 0) AS BaseSalary
    FROM Employees e
    INNER JOIN Accounts a ON a.Id = e.AccountId
    INNER JOIN Roles r ON r.Id = a.RoleId
    WHERE r.Name = N'Employee'
),
CurrentApprovedWork AS
(
    SELECT
        wa.EmployeeId,
        SUM(COALESCE(wa.WorkUnit, 0)) AS WorkingDays,
        SUM(COALESCE(wa.PenaltyAmount, 0)) AS PenaltyAmount
    FROM WorkAttendances wa
    WHERE wa.[Status] = N'Approved'
      AND MONTH(wa.[Date]) = @CurrentMonth
      AND YEAR(wa.[Date]) = @CurrentYear
    GROUP BY wa.EmployeeId
),
CurrentSalarySource AS
(
    SELECT
        t.EmployeeId,
        CAST(COALESCE(w.WorkingDays, 0) AS decimal(18,2)) AS WorkingDays,
        CAST(ROUND(
            CASE WHEN @CurrentStandardWorkingDays = 0 THEN 0
                 ELSE (COALESCE(w.WorkingDays, 0) * t.BaseSalary / @CurrentStandardWorkingDays)
                      - COALESCE(w.PenaltyAmount, 0)
            END,
            0
        ) AS decimal(18,2)) AS SalaryAmount,
        t.BaseSalary AS BaseSalarySnapshot,
        CAST(COALESCE(w.PenaltyAmount, 0) AS decimal(18,2)) AS PenaltyAmount
    FROM TeacherScope t
    LEFT JOIN CurrentApprovedWork w ON w.EmployeeId = t.EmployeeId
)
MERGE Salaries AS target
USING CurrentSalarySource AS source
ON target.EmployeeId = source.EmployeeId
   AND target.PayrollPeriodId = @CurrentPeriodId
WHEN MATCHED AND target.Status NOT IN (2, 3) THEN
    UPDATE SET
        target.WorkingDays = source.WorkingDays,
        target.SalaryAmount = source.SalaryAmount,
        target.Status = 1,
        target.BaseSalarySnapshot = source.BaseSalarySnapshot,
        target.StandardWorkingDays = @CurrentStandardWorkingDays,
        target.PenaltyAmount = source.PenaltyAmount,
        target.CoverageBonusAmount = 0,
        target.CalculatedAtUtc = SYSUTCDATETIME(),
        target.PaymentNote = @SeedNote
WHEN NOT MATCHED THEN
    INSERT
    (
        EmployeeId,
        PayrollPeriodId,
        WorkingDays,
        SalaryAmount,
        Status,
        BaseSalarySnapshot,
        StandardWorkingDays,
        PenaltyAmount,
        CoverageBonusAmount,
        CalculatedAtUtc,
        LockedAtUtc,
        PaidAtUtc,
        PaymentMethod,
        PaymentNote
    )
    VALUES
    (
        source.EmployeeId,
        @CurrentPeriodId,
        source.WorkingDays,
        source.SalaryAmount,
        1,
        source.BaseSalarySnapshot,
        @CurrentStandardWorkingDays,
        source.PenaltyAmount,
        0,
        SYSUTCDATETIME(),
        NULL,
        NULL,
        NULL,
        @SeedNote
    );

;WITH TeacherScope AS
(
    SELECT
        e.Id AS EmployeeId,
        COALESCE(e.BaseSalary, 0) AS BaseSalary
    FROM Employees e
    INNER JOIN Accounts a ON a.Id = e.AccountId
    INNER JOIN Roles r ON r.Id = a.RoleId
    WHERE r.Name = N'Employee'
),
PreviousSalarySource AS
(
    SELECT
        EmployeeId,
        CAST(@PreviousStandardWorkingDays AS decimal(18,2)) AS WorkingDays,
        CAST(ROUND(BaseSalary, 0) AS decimal(18,2)) AS SalaryAmount,
        BaseSalary AS BaseSalarySnapshot
    FROM TeacherScope
)
MERGE Salaries AS target
USING PreviousSalarySource AS source
ON target.EmployeeId = source.EmployeeId
   AND target.PayrollPeriodId = @PreviousPeriodId
WHEN MATCHED THEN
    UPDATE SET
        target.WorkingDays = source.WorkingDays,
        target.SalaryAmount = source.SalaryAmount,
        target.Status = 3,
        target.BaseSalarySnapshot = source.BaseSalarySnapshot,
        target.StandardWorkingDays = @PreviousStandardWorkingDays,
        target.PenaltyAmount = 0,
        target.CoverageBonusAmount = 0,
        target.CalculatedAtUtc = COALESCE(target.CalculatedAtUtc, SYSUTCDATETIME()),
        target.LockedAtUtc = COALESCE(target.LockedAtUtc, SYSUTCDATETIME()),
        target.PaidAtUtc = COALESCE(target.PaidAtUtc, SYSUTCDATETIME()),
        target.PaymentMethod = N'Chuyển khoản',
        target.PaymentNote = @SeedNote
WHEN NOT MATCHED THEN
    INSERT
    (
        EmployeeId,
        PayrollPeriodId,
        WorkingDays,
        SalaryAmount,
        Status,
        BaseSalarySnapshot,
        StandardWorkingDays,
        PenaltyAmount,
        CoverageBonusAmount,
        CalculatedAtUtc,
        LockedAtUtc,
        PaidAtUtc,
        PaymentMethod,
        PaymentNote
    )
    VALUES
    (
        source.EmployeeId,
        @PreviousPeriodId,
        source.WorkingDays,
        source.SalaryAmount,
        3,
        source.BaseSalarySnapshot,
        @PreviousStandardWorkingDays,
        0,
        0,
        SYSUTCDATETIME(),
        SYSUTCDATETIME(),
        SYSUTCDATETIME(),
        N'Chuyển khoản',
        @SeedNote
    );
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @CurrentMonth int = 6;
DECLARE @CurrentYear int = 2026;
DECLARE @PreviousMonth int = 5;
DECLARE @PreviousYear int = 2026;
DECLARE @SeedNote nvarchar(255) = N'SeedPayrollReviewData';

DECLARE @CurrentPeriodId int = (
    SELECT TOP (1) Id FROM PayrollPeriods WHERE [Month] = @CurrentMonth AND [Year] = @CurrentYear ORDER BY Id
);
DECLARE @PreviousPeriodId int = (
    SELECT TOP (1) Id FROM PayrollPeriods WHERE [Month] = @PreviousMonth AND [Year] = @PreviousYear ORDER BY Id
);

DELETE FROM WorkAttendances
WHERE Note = @SeedNote
  AND MONTH([Date]) = @CurrentMonth
  AND YEAR([Date]) = @CurrentYear;

IF @CurrentPeriodId IS NOT NULL
BEGIN
    DELETE FROM Salaries
    WHERE PayrollPeriodId = @CurrentPeriodId
      AND PaymentNote = @SeedNote;
END;

IF @PreviousPeriodId IS NOT NULL
BEGIN
    DELETE FROM Salaries
    WHERE PayrollPeriodId = @PreviousPeriodId
      AND PaymentNote = @SeedNote;
END;
");
        }
    }
}
