CREATE TABLE [dbo].[trip_installments]
(
    [id] INT IDENTITY (1, 1) NOT NULL,
    [trip_cost_item_id] INT NOT NULL,
    [due_date] DATE NOT NULL,
    [amount] DECIMAL (10, 2) NOT NULL,
    CONSTRAINT [PK_trip_installments] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [CK_trip_installments_amount] CHECK ([amount] > 0),
    CONSTRAINT [FK_trip_installments_trip_cost_items] FOREIGN KEY ([trip_cost_item_id]) REFERENCES [dbo].[trip_cost_items] ([id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_trip_installments_trip_cost_item_id_due_date]
    ON [dbo].[trip_installments] ([trip_cost_item_id], [due_date]);
GO
