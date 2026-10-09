using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControleFinanceiro.DebtImport;

public sealed record DebtWorkbookImportResult(
    Guid? BatchId,
    int DebtsInserted,
    int InstallmentsInserted,
    int AlreadyImported,
    int UndatedEarlyPaymentsSkipped);

public sealed class DebtWorkbookImporter(string connectionString)
{
    private const string LockResource = "ControleFinanceiro.DebtWorkbookImport";

    public async Task<DebtWorkbookImportResult> ImportAsync(
        string workbookPath,
        DebtWorkbookImportPlan plan,
        CancellationToken cancellationToken = default)
    {
        string fileHash;
        await using (var workbookStream = File.OpenRead(workbookPath))
        {
            fileHash = Convert.ToHexString(await SHA256.HashDataAsync(workbookStream, cancellationToken));
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await AcquireLockAsync(connection, transaction, cancellationToken);
            var batchId = Guid.NewGuid();
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO dbo.debt_import_batches (id, source_file_hash)
                VALUES (@BatchId, @FileHash);
                """,
                new { BatchId = batchId, FileHash = fileHash },
                transaction,
                cancellationToken: cancellationToken));

            var debtsInserted = 0;
            var installmentsInserted = 0;
            var alreadyImported = 0;
            var sourceRecordsAdded = 0;

            foreach (var debt in plan.Debts)
            {
                var contentHash = HashDebt(debt);
                var sourceRecord = await FindSourceRecordAsync(
                    connection,
                    transaction,
                    debt.SourceKey,
                    cancellationToken);
                int debtId;
                if (sourceRecord is not null)
                {
                    ValidateSourceRecord(sourceRecord, contentHash, "Debt");
                    debtId = sourceRecord.DebtId;
                    alreadyImported++;
                }
                else
                {
                    var reusableIds = (await connection.QueryAsync<int>(new CommandDefinition(
                        """
                        SELECT TOP (2) id
                        FROM dbo.debts WITH (UPDLOCK, HOLDLOCK)
                        WHERE descricao = @Description
                          AND data_vencimento = @FirstDueDate
                          AND valor_original = @OriginalValue
                          AND limite_credito IS NULL;
                        """,
                        new
                        {
                            Description = debt.Description,
                            FirstDueDate = debt.FirstDueDate,
                            OriginalValue = debt.OriginalValue
                        },
                        transaction,
                        cancellationToken: cancellationToken))).ToArray();

                    if (reusableIds.Length > 1)
                    {
                        throw new InvalidOperationException(
                            "More than one existing debt matches a workbook record; resolve the duplicate before importing.");
                    }

                    if (reusableIds.Length == 1)
                    {
                        debtId = reusableIds[0];
                    }
                    else
                    {
                        debtId = await connection.QuerySingleAsync<int>(new CommandDefinition(
                            """
                            INSERT INTO dbo.debts
                                (descricao, data_vencimento, valor_original, limite_credito)
                            OUTPUT INSERTED.id
                            VALUES (@Description, @FirstDueDate, @OriginalValue, NULL);
                            """,
                            new
                            {
                                Description = debt.Description,
                                FirstDueDate = debt.FirstDueDate,
                                OriginalValue = debt.OriginalValue
                            },
                            transaction,
                            cancellationToken: cancellationToken));
                        debtsInserted++;
                        await InsertSourceRecordAsync(
                            connection,
                            transaction,
                            batchId,
                            debt.SourceKey,
                            contentHash,
                            "Debt",
                            debtId,
                            debtInstallmentId: null,
                            ownsEntity: true,
                            cancellationToken);
                        sourceRecordsAdded++;
                    }

                    if (reusableIds.Length == 1)
                    {
                        await InsertSourceRecordAsync(
                            connection,
                            transaction,
                            batchId,
                            debt.SourceKey,
                            contentHash,
                            "Debt",
                            debtId,
                            debtInstallmentId: null,
                            ownsEntity: false,
                            cancellationToken);
                        sourceRecordsAdded++;
                    }
                }

                foreach (var installment in debt.Installments)
                {
                    var installmentHash = HashInstallment(installment);
                    sourceRecord = await FindSourceRecordAsync(
                        connection,
                        transaction,
                        installment.SourceKey,
                        cancellationToken);
                    if (sourceRecord is not null)
                    {
                        ValidateSourceRecord(sourceRecord, installmentHash, "Installment");
                        if (sourceRecord.DebtId != debtId)
                        {
                            throw new InvalidOperationException(
                                "An imported installment is associated with a different debt than the workbook.");
                        }

                        alreadyImported++;
                        continue;
                    }

                    var matchingInstallments = (await connection.QueryAsync<int>(new CommandDefinition(
                        """
                        SELECT TOP (2) id
                        FROM dbo.debt_installments WITH (UPDLOCK, HOLDLOCK)
                        WHERE debt_id = @DebtId
                          AND data_vencimento = @DueDate
                          AND valor_previsto = @Amount
                          AND status = @Status
                          AND data_pagamento IS NULL;
                        """,
                        new
                        {
                            DebtId = debtId,
                            DueDate = installment.DueDate,
                            Amount = installment.Amount,
                            Status = installment.Status
                        },
                        transaction,
                        cancellationToken: cancellationToken))).ToArray();

                    if (matchingInstallments.Length > 1)
                    {
                        throw new InvalidOperationException(
                            "More than one existing installment matches a workbook record; resolve the duplicate before importing.");
                    }

                    int installmentId;
                    var ownsEntity = matchingInstallments.Length == 0;
                    if (ownsEntity)
                    {
                        installmentId = await connection.QuerySingleAsync<int>(new CommandDefinition(
                            """
                            INSERT INTO dbo.debt_installments
                                (debt_id, data_vencimento, valor_previsto, status, data_pagamento)
                            OUTPUT INSERTED.id
                            VALUES (@DebtId, @DueDate, @Amount, @Status, NULL);
                            """,
                            new
                            {
                                DebtId = debtId,
                                DueDate = installment.DueDate,
                                Amount = installment.Amount,
                                Status = installment.Status
                            },
                            transaction,
                            cancellationToken: cancellationToken));
                        installmentsInserted++;
                    }
                    else
                    {
                        installmentId = matchingInstallments[0];
                    }

                    await InsertSourceRecordAsync(
                        connection,
                        transaction,
                        batchId,
                        installment.SourceKey,
                        installmentHash,
                        "Installment",
                        debtId,
                        installmentId,
                        ownsEntity,
                        cancellationToken);
                    sourceRecordsAdded++;
                }
            }

            if (sourceRecordsAdded == 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    "DELETE FROM dbo.debt_import_batches WHERE id = @BatchId;",
                    new { BatchId = batchId },
                    transaction,
                    cancellationToken: cancellationToken));
                batchId = Guid.Empty;
            }

            await transaction.CommitAsync(cancellationToken);
            return new DebtWorkbookImportResult(
                batchId == Guid.Empty ? null : batchId,
                debtsInserted,
                installmentsInserted,
                alreadyImported,
                plan.UndatedEarlyPaymentsSkipped);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task RollbackAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await AcquireLockAsync(connection, transaction, cancellationToken);
            var batchExists = await connection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition(
                    "SELECT 1 FROM dbo.debt_import_batches WITH (UPDLOCK, HOLDLOCK) WHERE id = @BatchId;",
                    new { BatchId = batchId },
                    transaction,
                    cancellationToken: cancellationToken));
            if (batchExists is null)
            {
                throw new InvalidOperationException($"Import batch '{batchId}' was not found.");
            }

            var hasUntrackedChildren = await connection.QuerySingleAsync<bool>(new CommandDefinition(
                """
                SELECT CONVERT(BIT, CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.debt_import_records parent
                    WHERE parent.batch_id = @BatchId
                      AND parent.record_kind = 'Debt'
                      AND parent.owns_entity = 1
                      AND
                      (
                          EXISTS
                          (
                              SELECT 1 FROM dbo.debt_installments child
                              WHERE child.debt_id = parent.debt_id
                                AND NOT EXISTS
                                (
                                    SELECT 1 FROM dbo.debt_import_records imported
                                    WHERE imported.batch_id = @BatchId
                                      AND imported.record_kind = 'Installment'
                                      AND imported.debt_installment_id = child.id
                                      AND imported.owns_entity = 1
                                )
                          )
                          OR EXISTS
                          (
                              SELECT 1 FROM dbo.debt_early_payments child
                              WHERE child.debt_id = parent.debt_id
                          )
                      )
                ) THEN 1 ELSE 0 END);
                """,
                new { BatchId = batchId },
                transaction,
                cancellationToken: cancellationToken));
            if (hasUntrackedChildren)
            {
                throw new InvalidOperationException(
                    "The batch created a debt that now has other installments or payments; remove those dependencies before rollback.");
            }

            var hasLinkedOutflows = await connection.QuerySingleAsync<bool>(new CommandDefinition(
                """
                SELECT CONVERT(BIT, CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.debt_outflow_links link
                    WHERE EXISTS
                    (
                        SELECT 1 FROM dbo.debt_import_records imported
                        WHERE imported.batch_id = @BatchId
                          AND imported.owns_entity = 1
                          AND imported.debt_installment_id = link.debt_installment_id
                    )
                ) THEN 1 ELSE 0 END);
                """,
                new { BatchId = batchId },
                transaction,
                cancellationToken: cancellationToken));
            if (hasLinkedOutflows)
            {
                throw new InvalidOperationException(
                    "The batch contains debt payments linked to financial outflows; remove those links before rollback.");
            }

            await connection.ExecuteAsync(new CommandDefinition(
                """
                DELETE installment
                FROM dbo.debt_installments installment
                INNER JOIN dbo.debt_import_records imported
                    ON imported.debt_installment_id = installment.id
                WHERE imported.batch_id = @BatchId
                  AND imported.record_kind = 'Installment'
                  AND imported.owns_entity = 1;

                DELETE debt
                FROM dbo.debts debt
                INNER JOIN dbo.debt_import_records imported
                    ON imported.debt_id = debt.id
                WHERE imported.batch_id = @BatchId
                  AND imported.record_kind = 'Debt'
                  AND imported.owns_entity = 1;

                DELETE FROM dbo.debt_import_records WHERE batch_id = @BatchId;
                DELETE FROM dbo.debt_import_batches WHERE id = @BatchId;
                """,
                new { BatchId = batchId },
                transaction,
                cancellationToken: cancellationToken));
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task AcquireLockAsync(
        SqlConnection connection,
        System.Data.Common.DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var lockResult = await connection.QuerySingleAsync<int>(new CommandDefinition(
            """
            DECLARE @LockResult INT;
            EXEC @LockResult = sys.sp_getapplock
                @Resource = @Resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 300000;
            SELECT @LockResult;
            """,
            new { Resource = LockResource },
            transaction,
            cancellationToken: cancellationToken));

        if (lockResult < 0)
        {
            throw new InvalidOperationException("Could not acquire the debt workbook import lock.");
        }
    }

    private static async Task<SourceRecord?> FindSourceRecordAsync(
        SqlConnection connection,
        System.Data.Common.DbTransaction transaction,
        string sourceKey,
        CancellationToken cancellationToken)
    {
        return await connection.QuerySingleOrDefaultAsync<SourceRecord>(new CommandDefinition(
            """
            SELECT source_key AS SourceKey,
                   content_hash AS ContentHash,
                   record_kind AS RecordKind,
                   debt_id AS DebtId,
                   debt_installment_id AS DebtInstallmentId,
                   owns_entity AS OwnsEntity
            FROM dbo.debt_import_records WITH (UPDLOCK, HOLDLOCK)
            WHERE source_key = @SourceKey;
            """,
            new { SourceKey = sourceKey },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task InsertSourceRecordAsync(
        SqlConnection connection,
        System.Data.Common.DbTransaction transaction,
        Guid batchId,
        string sourceKey,
        string contentHash,
        string recordKind,
        int debtId,
        int? debtInstallmentId,
        bool ownsEntity,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.debt_import_records
                (source_key, content_hash, batch_id, record_kind, debt_id, debt_installment_id, owns_entity)
            VALUES
                (@SourceKey, @ContentHash, @BatchId, @RecordKind, @DebtId, @DebtInstallmentId, @OwnsEntity);
            """,
            new
            {
                SourceKey = sourceKey,
                ContentHash = contentHash,
                BatchId = batchId,
                RecordKind = recordKind,
                DebtId = debtId,
                DebtInstallmentId = debtInstallmentId,
                OwnsEntity = ownsEntity
            },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static void ValidateSourceRecord(SourceRecord record, string contentHash, string expectedKind)
    {
        if (!string.Equals(record.RecordKind, expectedKind, StringComparison.Ordinal)
            || !string.Equals(record.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A previously imported workbook record has changed. Roll back its import batch before importing the updated workbook.");
        }
    }

    private static string HashDebt(DebtWorkbookDebt debt)
    {
        return Hash(string.Join(
            "|",
            debt.Description,
            debt.FirstDueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            debt.OriginalValue.ToString("0.00", CultureInfo.InvariantCulture),
            debt.InstallmentCount.ToString(CultureInfo.InvariantCulture)));
    }

    private static string HashInstallment(DebtWorkbookInstallment installment)
    {
        return Hash(string.Join(
            "|",
            installment.Number.ToString(CultureInfo.InvariantCulture),
            installment.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            installment.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            installment.Status));
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private sealed record SourceRecord(
        string SourceKey,
        string ContentHash,
        string RecordKind,
        int DebtId,
        int? DebtInstallmentId,
        bool OwnsEntity);
}
