CREATE TABLE [dbo].[recurring_expenses]
(
    [id] INT IDENTITY (1, 1) NOT NULL,
    [name] NVARCHAR (150) NOT NULL,
    [charge_type] NVARCHAR (30) NOT NULL,
    [amount] DECIMAL (10, 2) NOT NULL,
    [payment_type] NVARCHAR (50) NOT NULL,
    [renewal_day] INT NOT NULL,
    [cadence] NVARCHAR (20) NOT NULL,
    [initial_renewal_date] DATE NOT NULL,
    CONSTRAINT [PK_recurring_expenses] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [CK_recurring_expenses_renewal_day] CHECK ([renewal_day] BETWEEN 1 AND 31),
    CONSTRAINT [CK_recurring_expenses_cadence] CHECK ([cadence] IN (N'Monthly', N'Yearly')),
    CONSTRAINT [CK_recurring_expenses_charge_type] CHECK ([charge_type] IN (N'Serviço', N'Conta da casa'))
);
GO

CREATE TABLE [dbo].[recurring_expense_occurrences]
(
    [id] INT IDENTITY (1, 1) NOT NULL,
    [recurring_expense_id] INT NOT NULL,
    [renewal_date] DATE NOT NULL,
    [amount] DECIMAL (10, 2) NOT NULL,
    [conta_casa_id] INT NULL,
    [despesa_variavel_id] INT NULL,
    CONSTRAINT [PK_recurring_expense_occurrences] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [CK_recurring_expense_occurrences_single_ledger_link] CHECK ([conta_casa_id] IS NULL OR [despesa_variavel_id] IS NULL),
    CONSTRAINT [FK_recurring_expense_occurrences_recurring_expenses] FOREIGN KEY ([recurring_expense_id]) REFERENCES [dbo].[recurring_expenses] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_recurring_expense_occurrences_contas_casa] FOREIGN KEY ([conta_casa_id]) REFERENCES [dbo].[contas_casa] ([id]) ON DELETE SET NULL,
    CONSTRAINT [FK_recurring_expense_occurrences_despesas_variaveis] FOREIGN KEY ([despesa_variavel_id]) REFERENCES [dbo].[despesas_variaveis] ([id]) ON DELETE SET NULL
);
GO

CREATE UNIQUE INDEX [UX_recurring_expense_occurrences_period]
    ON [dbo].[recurring_expense_occurrences] ([recurring_expense_id] ASC, [renewal_date] ASC);
GO

CREATE UNIQUE INDEX [UX_recurring_expense_occurrences_conta_casa_id]
    ON [dbo].[recurring_expense_occurrences] ([conta_casa_id] ASC)
    WHERE [conta_casa_id] IS NOT NULL;
GO

CREATE UNIQUE INDEX [UX_recurring_expense_occurrences_despesa_variavel_id]
    ON [dbo].[recurring_expense_occurrences] ([despesa_variavel_id] ASC)
    WHERE [despesa_variavel_id] IS NOT NULL;