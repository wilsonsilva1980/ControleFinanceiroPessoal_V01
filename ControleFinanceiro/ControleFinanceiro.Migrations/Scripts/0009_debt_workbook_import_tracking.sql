SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.debt_import_batches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.debt_import_batches
    (
        id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_debt_import_batches PRIMARY KEY,
        source_file_hash CHAR(64) NOT NULL,
        created_at_utc DATETIME2(7) NOT NULL
            CONSTRAINT DF_debt_import_batches_created_at_utc DEFAULT (SYSUTCDATETIME())
    );
END;

IF OBJECT_ID(N'dbo.debt_import_records', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.debt_import_records
    (
        source_key CHAR(64) NOT NULL CONSTRAINT PK_debt_import_records PRIMARY KEY,
        content_hash CHAR(64) NOT NULL,
        batch_id UNIQUEIDENTIFIER NOT NULL,
        record_kind VARCHAR(20) NOT NULL,
        debt_id INT NOT NULL,
        debt_installment_id INT NULL,
        owns_entity BIT NOT NULL,
        created_at_utc DATETIME2(7) NOT NULL
            CONSTRAINT DF_debt_import_records_created_at_utc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_debt_import_records_record_kind CHECK
        (
            (record_kind = 'Debt' AND debt_installment_id IS NULL)
            OR (record_kind = 'Installment' AND debt_installment_id IS NOT NULL)
        ),
        CONSTRAINT FK_debt_import_records_batches FOREIGN KEY (batch_id)
            REFERENCES dbo.debt_import_batches (id),
        CONSTRAINT FK_debt_import_records_debts FOREIGN KEY (debt_id)
            REFERENCES dbo.debts (id),
        CONSTRAINT FK_debt_import_records_installments FOREIGN KEY (debt_installment_id)
            REFERENCES dbo.debt_installments (id)
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_debt_import_records_batch_id'
      AND object_id = OBJECT_ID(N'dbo.debt_import_records')
)
    CREATE INDEX IX_debt_import_records_batch_id
        ON dbo.debt_import_records (batch_id, record_kind);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_debt_import_records_debt_installment_id'
      AND object_id = OBJECT_ID(N'dbo.debt_import_records')
)
    CREATE UNIQUE INDEX UX_debt_import_records_debt_installment_id
        ON dbo.debt_import_records (debt_installment_id)
        WHERE debt_installment_id IS NOT NULL;
