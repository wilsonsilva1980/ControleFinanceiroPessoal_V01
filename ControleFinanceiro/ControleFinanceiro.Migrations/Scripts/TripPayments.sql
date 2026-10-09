CREATE TABLE [dbo].[trip_payments]
(
    [id] INT IDENTITY (1, 1) NOT NULL,
    [trip_cost_item_id] INT NOT NULL,
    [trip_installment_id] INT NULL,
    [payment_date] DATE NOT NULL,
    [amount] DECIMAL (10, 2) NOT NULL,
    CONSTRAINT [PK_trip_payments] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [CK_trip_payments_amount] CHECK ([amount] > 0),
    CONSTRAINT [FK_trip_payments_trip_cost_items] FOREIGN KEY ([trip_cost_item_id]) REFERENCES [dbo].[trip_cost_items] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_trip_payments_trip_installments] FOREIGN KEY ([trip_installment_id]) REFERENCES [dbo].[trip_installments] ([id])
);
GO

CREATE INDEX [IX_trip_payments_trip_cost_item_id_payment_date]
    ON [dbo].[trip_payments] ([trip_cost_item_id], [payment_date]);
GO
