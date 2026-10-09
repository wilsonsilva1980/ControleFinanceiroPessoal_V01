SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.debt_outflow_links', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.debt_outflow_links
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_debt_outflow_links PRIMARY KEY,
        debt_installment_id INT NULL,
        debt_early_payment_id INT NULL,
        conta_casa_id INT NULL,
        cartao_lancamento_id INT NULL,
        CONSTRAINT CK_debt_outflow_links_sources CHECK
        (
            ((debt_installment_id IS NOT NULL AND debt_early_payment_id IS NULL) OR (debt_installment_id IS NULL AND debt_early_payment_id IS NOT NULL))
            AND ((conta_casa_id IS NOT NULL AND cartao_lancamento_id IS NULL) OR (conta_casa_id IS NULL AND cartao_lancamento_id IS NOT NULL))
        ),
        CONSTRAINT FK_debt_outflow_links_debt_installments FOREIGN KEY (debt_installment_id) REFERENCES dbo.debt_installments (id),
        CONSTRAINT FK_debt_outflow_links_debt_early_payments FOREIGN KEY (debt_early_payment_id) REFERENCES dbo.debt_early_payments (id),
        CONSTRAINT FK_debt_outflow_links_contas_casa FOREIGN KEY (conta_casa_id) REFERENCES dbo.contas_casa (id) ON DELETE CASCADE,
        CONSTRAINT FK_debt_outflow_links_cartao_lancamentos FOREIGN KEY (cartao_lancamento_id) REFERENCES dbo.cartao_lancamentos (id) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_debt_outflow_links_debt_installment_id' AND object_id = OBJECT_ID(N'dbo.debt_outflow_links'))
    CREATE UNIQUE INDEX UX_debt_outflow_links_debt_installment_id ON dbo.debt_outflow_links (debt_installment_id) WHERE debt_installment_id IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_debt_outflow_links_debt_early_payment_id' AND object_id = OBJECT_ID(N'dbo.debt_outflow_links'))
    CREATE UNIQUE INDEX UX_debt_outflow_links_debt_early_payment_id ON dbo.debt_outflow_links (debt_early_payment_id) WHERE debt_early_payment_id IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_debt_outflow_links_conta_casa_id' AND object_id = OBJECT_ID(N'dbo.debt_outflow_links'))
    CREATE UNIQUE INDEX UX_debt_outflow_links_conta_casa_id ON dbo.debt_outflow_links (conta_casa_id) WHERE conta_casa_id IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_debt_outflow_links_cartao_lancamento_id' AND object_id = OBJECT_ID(N'dbo.debt_outflow_links'))
    CREATE UNIQUE INDEX UX_debt_outflow_links_cartao_lancamento_id ON dbo.debt_outflow_links (cartao_lancamento_id) WHERE cartao_lancamento_id IS NOT NULL;