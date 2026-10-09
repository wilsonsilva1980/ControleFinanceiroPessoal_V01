SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.recurring_expenses', N'cadence') IS NULL
BEGIN
    ALTER TABLE dbo.recurring_expenses
        ADD cadence NVARCHAR(20) NOT NULL
            CONSTRAINT DF_recurring_expenses_cadence DEFAULT (N'Monthly');
END;

IF COL_LENGTH(N'dbo.recurring_expenses', N'initial_renewal_date') IS NULL
BEGIN
    ALTER TABLE dbo.recurring_expenses
        ADD initial_renewal_date DATE NULL;

    EXEC sys.sp_executesql N'UPDATE dbo.recurring_expenses
        SET initial_renewal_date = DATEFROMPARTS(1900, 1, renewal_day)
        WHERE initial_renewal_date IS NULL;';

    ALTER TABLE dbo.recurring_expenses
        ALTER COLUMN initial_renewal_date DATE NOT NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_recurring_expenses_cadence'
      AND parent_object_id = OBJECT_ID(N'dbo.recurring_expenses')
)
BEGIN
    EXEC sys.sp_executesql N'ALTER TABLE dbo.recurring_expenses
        ADD CONSTRAINT CK_recurring_expenses_cadence
            CHECK (cadence IN (N''Monthly'', N''Yearly''));';
END;
