SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.trip_budgets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.trip_budgets
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_trip_budgets PRIMARY KEY,
        name NVARCHAR(150) NOT NULL,
        destination NVARCHAR(100) NOT NULL,
        clp_per_brl_quote DECIMAL(12,4) NULL,
        CONSTRAINT CK_trip_budgets_clp_per_brl_quote CHECK (clp_per_brl_quote IS NULL OR clp_per_brl_quote > 0)
    );
END;

IF OBJECT_ID(N'dbo.trip_cost_items', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.trip_cost_items
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_trip_cost_items PRIMARY KEY,
        trip_budget_id INT NOT NULL,
        description NVARCHAR(150) NOT NULL,
        value_brl DECIMAL(10,2) NOT NULL,
        fees_brl DECIMAL(10,2) NOT NULL,
        value_usd DECIMAL(10,2) NULL,
        installment_count INT NOT NULL,
        CONSTRAINT CK_trip_cost_items_amounts CHECK (value_brl >= 0 AND fees_brl >= 0 AND (value_usd IS NULL OR value_usd >= 0)),
        CONSTRAINT CK_trip_cost_items_installment_count CHECK (installment_count >= 1),
        CONSTRAINT FK_trip_cost_items_trip_budgets FOREIGN KEY (trip_budget_id) REFERENCES dbo.trip_budgets (id) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'dbo.trip_installments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.trip_installments
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_trip_installments PRIMARY KEY,
        trip_cost_item_id INT NOT NULL,
        due_date DATE NOT NULL,
        amount DECIMAL(10,2) NOT NULL,
        CONSTRAINT CK_trip_installments_amount CHECK (amount > 0),
        CONSTRAINT FK_trip_installments_trip_cost_items FOREIGN KEY (trip_cost_item_id) REFERENCES dbo.trip_cost_items (id) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'dbo.trip_payments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.trip_payments
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_trip_payments PRIMARY KEY,
        trip_cost_item_id INT NOT NULL,
        trip_installment_id INT NULL,
        payment_date DATE NOT NULL,
        amount DECIMAL(10,2) NOT NULL,
        CONSTRAINT CK_trip_payments_amount CHECK (amount > 0),
        CONSTRAINT FK_trip_payments_trip_cost_items FOREIGN KEY (trip_cost_item_id) REFERENCES dbo.trip_cost_items (id) ON DELETE CASCADE,
        CONSTRAINT FK_trip_payments_trip_installments FOREIGN KEY (trip_installment_id) REFERENCES dbo.trip_installments (id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_trip_cost_items_trip_budget_id' AND object_id = OBJECT_ID(N'dbo.trip_cost_items'))
    CREATE INDEX IX_trip_cost_items_trip_budget_id ON dbo.trip_cost_items (trip_budget_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_trip_installments_trip_cost_item_id_due_date' AND object_id = OBJECT_ID(N'dbo.trip_installments'))
    CREATE INDEX IX_trip_installments_trip_cost_item_id_due_date ON dbo.trip_installments (trip_cost_item_id, due_date);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_trip_payments_trip_cost_item_id_payment_date' AND object_id = OBJECT_ID(N'dbo.trip_payments'))
    CREATE INDEX IX_trip_payments_trip_cost_item_id_payment_date ON dbo.trip_payments (trip_cost_item_id, payment_date);
