SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.bank_accounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.bank_accounts
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_bank_accounts PRIMARY KEY,
        bank_name NVARCHAR(100) NOT NULL,
        account_name NVARCHAR(100) NOT NULL,
        account_type NVARCHAR(30) NOT NULL,
        branch_identifier NVARCHAR(30) NOT NULL,
        account_identifier NVARCHAR(30) NOT NULL,
        balance DECIMAL(10,2) NOT NULL,
        overdraft_used DECIMAL(10,2) NOT NULL,
        overdraft_limit DECIMAL(10,2) NOT NULL,
        CONSTRAINT CK_bank_accounts_overdraft_values CHECK (overdraft_used >= 0 AND overdraft_limit >= 0)
    );
END;

IF OBJECT_ID(N'dbo.credit_cards', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.credit_cards
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_credit_cards PRIMARY KEY,
        issuer NVARCHAR(100) NOT NULL,
        display_name NVARCHAR(100) NOT NULL,
        purchase_limit DECIMAL(10,2) NOT NULL,
        local_cash_withdrawal_limit DECIMAL(10,2) NOT NULL,
        international_cash_withdrawal_limit DECIMAL(10,2) NOT NULL,
        credit_pix_enabled BIT NOT NULL,
        bill_payment_enabled BIT NOT NULL,
        due_day INT NOT NULL,
        CONSTRAINT CK_credit_cards_limits CHECK (purchase_limit >= 0 AND local_cash_withdrawal_limit >= 0 AND international_cash_withdrawal_limit >= 0),
        CONSTRAINT CK_credit_cards_due_day CHECK (due_day BETWEEN 1 AND 31)
    );
END;