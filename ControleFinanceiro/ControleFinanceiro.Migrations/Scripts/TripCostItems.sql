CREATE TABLE [dbo].[trip_cost_items]
(
    [id] INT IDENTITY (1, 1) NOT NULL,
    [trip_budget_id] INT NOT NULL,
    [description] NVARCHAR (150) NOT NULL,
    [value_brl] DECIMAL (10, 2) NOT NULL,
    [fees_brl] DECIMAL (10, 2) NOT NULL,
    [value_usd] DECIMAL (10, 2) NULL,
    [installment_count] INT NOT NULL,
    CONSTRAINT [PK_trip_cost_items] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [CK_trip_cost_items_amounts] CHECK ([value_brl] >= 0 AND [fees_brl] >= 0 AND ([value_usd] IS NULL OR [value_usd] >= 0)),
    CONSTRAINT [CK_trip_cost_items_installment_count] CHECK ([installment_count] >= 1),
    CONSTRAINT [FK_trip_cost_items_trip_budgets] FOREIGN KEY ([trip_budget_id]) REFERENCES [dbo].[trip_budgets] ([id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_trip_cost_items_trip_budget_id] ON [dbo].[trip_cost_items] ([trip_budget_id]);
GO
