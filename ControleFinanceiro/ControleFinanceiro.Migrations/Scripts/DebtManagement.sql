CREATE TABLE [dbo].[debts]
(
    [id] INT IDENTITY (1, 1) NOT NULL,
    [descricao] VARCHAR (150) NOT NULL,
    [data_vencimento] DATE NOT NULL,
    [valor_original] DECIMAL (10, 2) NOT NULL,
    [limite_credito] DECIMAL (10, 2) NULL,
    CONSTRAINT [PK_debts] PRIMARY KEY CLUSTERED ([id] ASC)
);
GO

CREATE TABLE [dbo].[debt_installments]
(
    [id] INT IDENTITY (1, 1) NOT NULL,
    [debt_id] INT NOT NULL,
    [data_vencimento] DATE NOT NULL,
    [valor_previsto] DECIMAL (10, 2) NOT NULL,
    [status] VARCHAR (30) NOT NULL CONSTRAINT [DF_debt_installments_status] DEFAULT ('Pendente'),
    [data_pagamento] DATE NULL,
    CONSTRAINT [PK_debt_installments] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [CK_debt_installments_status] CHECK ([status] IN ('Pendente', 'Pago')),
    CONSTRAINT [FK_debt_installments_debts] FOREIGN KEY ([debt_id]) REFERENCES [dbo].[debts] ([id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_debt_installments_debt_id_data_vencimento]
    ON [dbo].[debt_installments] ([debt_id] ASC, [data_vencimento] ASC);
GO

CREATE TABLE [dbo].[debt_early_payments]
(
    [id] INT IDENTITY (1, 1) NOT NULL,
    [debt_id] INT NOT NULL,
    [data_pagamento] DATE NOT NULL,
    [valor] DECIMAL (10, 2) NOT NULL,
    CONSTRAINT [PK_debt_early_payments] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [FK_debt_early_payments_debts] FOREIGN KEY ([debt_id]) REFERENCES [dbo].[debts] ([id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_debt_early_payments_debt_id_data_pagamento]
    ON [dbo].[debt_early_payments] ([debt_id] ASC, [data_pagamento] ASC);