SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.entradas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.entradas
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_entradas PRIMARY KEY,
        data_prevista DATE NOT NULL,
        data_recebimento DATE NULL,
        descricao VARCHAR(150) NOT NULL,
        categoria VARCHAR(50) NOT NULL,
        valor_previsto DECIMAL(10, 2) NOT NULL CONSTRAINT DF_entradas_valor_previsto DEFAULT (0.00),
        valor_recebido DECIMAL(10, 2) NULL CONSTRAINT DF_entradas_valor_recebido DEFAULT (0.00),
        status VARCHAR(30) NOT NULL CONSTRAINT DF_entradas_status DEFAULT ('Pendente')
    );
END;

IF OBJECT_ID(N'dbo.contas_casa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.contas_casa
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_contas_casa PRIMARY KEY,
        data_vencimento DATE NOT NULL,
        data_pagamento DATE NULL,
        descricao VARCHAR(150) NOT NULL,
        categoria VARCHAR(50) NOT NULL,
        valor_previsto DECIMAL(10, 2) NOT NULL CONSTRAINT DF_contas_casa_valor_previsto DEFAULT (0.00),
        valor_pago DECIMAL(10, 2) NULL CONSTRAINT DF_contas_casa_valor_pago DEFAULT (0.00),
        status VARCHAR(30) NOT NULL CONSTRAINT DF_contas_casa_status DEFAULT ('Pendente')
    );
END;

IF OBJECT_ID(N'dbo.cartao_lancamentos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.cartao_lancamentos
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_cartao_lancamentos PRIMARY KEY,
        data_compra DATE NOT NULL,
        estabelecimento VARCHAR(150) NOT NULL,
        categoria VARCHAR(50) NOT NULL,
        numero_parcela INT NOT NULL CONSTRAINT DF_cartao_lancamentos_numero_parcela DEFAULT (1),
        total_parcelas INT NOT NULL CONSTRAINT DF_cartao_lancamentos_total_parcelas DEFAULT (1),
        valor_parcela DECIMAL(10, 2) NOT NULL,
        mes_fatura VARCHAR(20) NOT NULL,
        ano_fatura INT NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.despesas_variaveis', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.despesas_variaveis
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_despesas_variaveis PRIMARY KEY,
        data DATE NOT NULL,
        descricao VARCHAR(150) NOT NULL,
        categoria VARCHAR(50) NOT NULL,
        valor DECIMAL(10, 2) NOT NULL
    );
END;
