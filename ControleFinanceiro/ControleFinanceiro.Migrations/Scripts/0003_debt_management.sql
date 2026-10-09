SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.debts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.debts
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_debts PRIMARY KEY,
        descricao VARCHAR(150) NOT NULL,
        data_vencimento DATE NOT NULL,
        valor_original DECIMAL(10,2) NOT NULL,
        limite_credito DECIMAL(10,2) NULL
    );
END;

IF OBJECT_ID(N'dbo.debt_installments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.debt_installments
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_debt_installments PRIMARY KEY,
        debt_id INT NOT NULL,
        data_vencimento DATE NOT NULL,
        valor_previsto DECIMAL(10,2) NOT NULL,
        status VARCHAR(30) NOT NULL CONSTRAINT DF_debt_installments_status DEFAULT ('Pendente'),
        data_pagamento DATE NULL,
        CONSTRAINT CK_debt_installments_status CHECK (status IN ('Pendente', 'Pago')),
        CONSTRAINT FK_debt_installments_debts FOREIGN KEY (debt_id) REFERENCES dbo.debts (id) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'dbo.debt_early_payments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.debt_early_payments
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_debt_early_payments PRIMARY KEY,
        debt_id INT NOT NULL,
        data_pagamento DATE NOT NULL,
        valor DECIMAL(10,2) NOT NULL,
        CONSTRAINT FK_debt_early_payments_debts FOREIGN KEY (debt_id) REFERENCES dbo.debts (id) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_debt_installments_debt_id_data_vencimento' AND object_id = OBJECT_ID(N'dbo.debt_installments'))
    CREATE INDEX IX_debt_installments_debt_id_data_vencimento ON dbo.debt_installments (debt_id, data_vencimento);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_debt_early_payments_debt_id_data_pagamento' AND object_id = OBJECT_ID(N'dbo.debt_early_payments'))
    CREATE INDEX IX_debt_early_payments_debt_id_data_pagamento ON dbo.debt_early_payments (debt_id, data_pagamento);