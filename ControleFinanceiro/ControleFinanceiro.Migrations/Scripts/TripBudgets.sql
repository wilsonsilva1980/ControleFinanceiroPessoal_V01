CREATE TABLE [dbo].[trip_budgets]
(
    [id] INT IDENTITY (1, 1) NOT NULL,
    [name] NVARCHAR (150) NOT NULL,
    [destination] NVARCHAR (100) NOT NULL,
    [clp_per_brl_quote] DECIMAL (12, 4) NULL,
    CONSTRAINT [PK_trip_budgets] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [CK_trip_budgets_clp_per_brl_quote] CHECK ([clp_per_brl_quote] IS NULL OR [clp_per_brl_quote] > 0)
);
GO
