using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Entrada> Entradas => Set<Entrada>();
    public DbSet<ContaCasa> ContasCasa => Set<ContaCasa>();
    public DbSet<CartaoLancamento> CartaoLancamentos => Set<CartaoLancamento>();
    public DbSet<DespesaVariavel> DespesasVariaveis => Set<DespesaVariavel>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<DebtInstallment> DebtInstallments => Set<DebtInstallment>();
    public DbSet<DebtEarlyPayment> DebtEarlyPayments => Set<DebtEarlyPayment>();
    public DbSet<DebtOutflowLink> DebtOutflowLinks => Set<DebtOutflowLink>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<RecurringExpense> RecurringExpenses => Set<RecurringExpense>();
    public DbSet<RecurringExpenseOccurrence> RecurringExpenseOccurrences => Set<RecurringExpenseOccurrence>();
    public DbSet<TripBudget> TripBudgets => Set<TripBudget>();
    public DbSet<TripCostItem> TripCostItems => Set<TripCostItem>();
    public DbSet<TripInstallment> TripInstallments => Set<TripInstallment>();
    public DbSet<TripPayment> TripPayments => Set<TripPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Debt>(entity =>
        {
            entity.ToTable("debts");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Description).HasColumnName("descricao").HasMaxLength(150).IsRequired();
            entity.Property(item => item.DueDate).HasColumnName("data_vencimento").HasColumnType("date");
            entity.Property(item => item.OriginalAmount).HasColumnName("valor_original").HasPrecision(10, 2);
            entity.Property(item => item.CreditLimit).HasColumnName("limite_credito").HasPrecision(10, 2);
        });

        modelBuilder.Entity<DebtInstallment>(entity =>
        {
            entity.ToTable("debt_installments", table =>
                table.HasCheckConstraint("CK_debt_installments_status", "[status] IN ('Pendente', 'Pago')"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.DebtId).HasColumnName("debt_id");
            entity.Property(item => item.DueDate).HasColumnName("data_vencimento").HasColumnType("date");
            entity.Property(item => item.ScheduledAmount).HasColumnName("valor_previsto").HasPrecision(10, 2);
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
            entity.Property(item => item.PaymentDate).HasColumnName("data_pagamento").HasColumnType("date");
            entity.Ignore(item => item.PaidAmount);
            entity.Ignore(item => item.RemainingAmount);
            entity.HasOne(item => item.Debt)
                .WithMany(debt => debt.Installments)
                .HasForeignKey(item => item.DebtId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(item => new { item.DebtId, item.DueDate })
                .HasDatabaseName("IX_debt_installments_debt_id_data_vencimento");
        });

        modelBuilder.Entity<DebtEarlyPayment>(entity =>
        {
            entity.ToTable("debt_early_payments");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.DebtId).HasColumnName("debt_id");
            entity.Property(item => item.PaymentDate).HasColumnName("data_pagamento").HasColumnType("date");
            entity.Property(item => item.Amount).HasColumnName("valor").HasPrecision(10, 2);
            entity.HasOne(item => item.Debt)
                .WithMany(debt => debt.EarlyPayments)
                .HasForeignKey(item => item.DebtId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(item => new { item.DebtId, item.PaymentDate })
                .HasDatabaseName("IX_debt_early_payments_debt_id_data_pagamento");
        });

        modelBuilder.Entity<DebtOutflowLink>(entity =>
        {
            entity.ToTable("debt_outflow_links", table => table.HasCheckConstraint(
                "CK_debt_outflow_links_sources",
                "(([debt_installment_id] IS NOT NULL AND [debt_early_payment_id] IS NULL) OR ([debt_installment_id] IS NULL AND [debt_early_payment_id] IS NOT NULL)) AND (([conta_casa_id] IS NOT NULL AND [cartao_lancamento_id] IS NULL) OR ([conta_casa_id] IS NULL AND [cartao_lancamento_id] IS NOT NULL))"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.DebtInstallmentId).HasColumnName("debt_installment_id");
            entity.Property(item => item.DebtEarlyPaymentId).HasColumnName("debt_early_payment_id");
            entity.Property(item => item.ContaCasaId).HasColumnName("conta_casa_id");
            entity.Property(item => item.CartaoLancamentoId).HasColumnName("cartao_lancamento_id");
            entity.HasOne(item => item.DebtInstallment)
                .WithOne(installment => installment.OutflowLink)
                .HasForeignKey<DebtOutflowLink>(item => item.DebtInstallmentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_debt_outflow_links_debt_installments");
            entity.HasOne(item => item.DebtEarlyPayment)
                .WithOne(payment => payment.OutflowLink)
                .HasForeignKey<DebtOutflowLink>(item => item.DebtEarlyPaymentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_debt_outflow_links_debt_early_payments");
            entity.HasOne<ContaCasa>()
                .WithMany()
                .HasForeignKey(item => item.ContaCasaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_debt_outflow_links_contas_casa");
            entity.HasOne<CartaoLancamento>()
                .WithMany()
                .HasForeignKey(item => item.CartaoLancamentoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_debt_outflow_links_cartao_lancamentos");
            entity.HasIndex(item => item.DebtInstallmentId)
                .IsUnique()
                .HasFilter("[debt_installment_id] IS NOT NULL")
                .HasDatabaseName("UX_debt_outflow_links_debt_installment_id");
            entity.HasIndex(item => item.DebtEarlyPaymentId)
                .IsUnique()
                .HasFilter("[debt_early_payment_id] IS NOT NULL")
                .HasDatabaseName("UX_debt_outflow_links_debt_early_payment_id");
            entity.HasIndex(item => item.ContaCasaId)
                .IsUnique()
                .HasFilter("[conta_casa_id] IS NOT NULL")
                .HasDatabaseName("UX_debt_outflow_links_conta_casa_id");
            entity.HasIndex(item => item.CartaoLancamentoId)
                .IsUnique()
                .HasFilter("[cartao_lancamento_id] IS NOT NULL")
                .HasDatabaseName("UX_debt_outflow_links_cartao_lancamento_id");
        });

        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.ToTable("bank_accounts", table => table.HasCheckConstraint(
                "CK_bank_accounts_overdraft_values",
                "[overdraft_used] >= 0 AND [overdraft_limit] >= 0"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.BankName).HasColumnName("bank_name").HasMaxLength(100).IsRequired();
            entity.Property(item => item.AccountName).HasColumnName("account_name").HasMaxLength(100).IsRequired();
            entity.Property(item => item.AccountType).HasColumnName("account_type").HasMaxLength(30).IsRequired();
            entity.Property(item => item.BranchIdentifier).HasColumnName("branch_identifier").HasMaxLength(30).IsRequired();
            entity.Property(item => item.AccountIdentifier).HasColumnName("account_identifier").HasMaxLength(30).IsRequired();
            entity.Property(item => item.Balance).HasColumnName("balance").HasPrecision(10, 2);
            entity.Property(item => item.OverdraftUsed).HasColumnName("overdraft_used").HasPrecision(10, 2);
            entity.Property(item => item.OverdraftLimit).HasColumnName("overdraft_limit").HasPrecision(10, 2);
            entity.Ignore(item => item.AvailableOverdraft);
        });

        modelBuilder.Entity<CreditCard>(entity =>
        {
            entity.ToTable("credit_cards", table =>
            {
                table.HasCheckConstraint(
                    "CK_credit_cards_limits",
                    "[purchase_limit] >= 0 AND [local_cash_withdrawal_limit] >= 0 AND [international_cash_withdrawal_limit] >= 0");
                table.HasCheckConstraint("CK_credit_cards_due_day", "[due_day] BETWEEN 1 AND 31");
            });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Issuer).HasColumnName("issuer").HasMaxLength(100).IsRequired();
            entity.Property(item => item.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
            entity.Property(item => item.PurchaseLimit).HasColumnName("purchase_limit").HasPrecision(10, 2);
            entity.Property(item => item.LocalCashWithdrawalLimit).HasColumnName("local_cash_withdrawal_limit").HasPrecision(10, 2);
            entity.Property(item => item.InternationalCashWithdrawalLimit).HasColumnName("international_cash_withdrawal_limit").HasPrecision(10, 2);
            entity.Property(item => item.CreditPixEnabled).HasColumnName("credit_pix_enabled");
            entity.Property(item => item.BillPaymentEnabled).HasColumnName("bill_payment_enabled");
            entity.Property(item => item.DueDay).HasColumnName("due_day");
        });

        modelBuilder.Entity<RecurringExpense>(entity =>
        {
            entity.ToTable("recurring_expenses", table =>
            {
                table.HasCheckConstraint("CK_recurring_expenses_renewal_day", "[renewal_day] BETWEEN 1 AND 31");
                table.HasCheckConstraint("CK_recurring_expenses_charge_type", "[charge_type] IN (N'Serviço', N'Conta da casa')");
                table.HasCheckConstraint("CK_recurring_expenses_cadence", "[cadence] IN (N'Monthly', N'Yearly')");
            });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            entity.Property(item => item.ChargeType).HasColumnName("charge_type").HasMaxLength(30).IsRequired();
            entity.Property(item => item.Amount).HasColumnName("amount").HasPrecision(10, 2);
            entity.Property(item => item.PaymentType).HasColumnName("payment_type").HasMaxLength(50).IsRequired();
            entity.Property(item => item.RenewalDay).HasColumnName("renewal_day");
            entity.Property(item => item.Cadence).HasColumnName("cadence").HasMaxLength(20).IsRequired();
            entity.Property(item => item.InitialRenewalDate).HasColumnName("initial_renewal_date").HasColumnType("date");
        });

        modelBuilder.Entity<RecurringExpenseOccurrence>(entity =>
        {
            entity.ToTable("recurring_expense_occurrences", table => table.HasCheckConstraint(
                "CK_recurring_expense_occurrences_single_ledger_link",
                "[conta_casa_id] IS NULL OR [despesa_variavel_id] IS NULL"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.RecurringExpenseId).HasColumnName("recurring_expense_id");
            entity.Property(item => item.RenewalDate).HasColumnName("renewal_date").HasColumnType("date");
            entity.Property(item => item.Amount).HasColumnName("amount").HasPrecision(10, 2);
            entity.Property(item => item.ContaCasaId).HasColumnName("conta_casa_id");
            entity.Property(item => item.DespesaVariavelId).HasColumnName("despesa_variavel_id");
            entity.HasOne(item => item.RecurringExpense)
                .WithMany(expense => expense.Occurrences)
                .HasForeignKey(item => item.RecurringExpenseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_recurring_expense_occurrences_recurring_expenses");
            entity.HasOne<ContaCasa>()
                .WithMany()
                .HasForeignKey(item => item.ContaCasaId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_recurring_expense_occurrences_contas_casa");
            entity.HasOne<DespesaVariavel>()
                .WithMany()
                .HasForeignKey(item => item.DespesaVariavelId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_recurring_expense_occurrences_despesas_variaveis");
            entity.HasIndex(item => new { item.RecurringExpenseId, item.RenewalDate })
                .IsUnique()
                .HasDatabaseName("UX_recurring_expense_occurrences_period");
            entity.HasIndex(item => item.ContaCasaId)
                .IsUnique()
                .HasFilter("[conta_casa_id] IS NOT NULL")
                .HasDatabaseName("UX_recurring_expense_occurrences_conta_casa_id");
            entity.HasIndex(item => item.DespesaVariavelId)
                .IsUnique()
                .HasFilter("[despesa_variavel_id] IS NOT NULL")
                .HasDatabaseName("UX_recurring_expense_occurrences_despesa_variavel_id");
        });

        modelBuilder.Entity<TripBudget>(entity =>
        {
            entity.ToTable("trip_budgets", table => table.HasCheckConstraint(
                "CK_trip_budgets_clp_per_brl_quote",
                "[clp_per_brl_quote] IS NULL OR [clp_per_brl_quote] > 0"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            entity.Property(item => item.Destination).HasColumnName("destination").HasMaxLength(100).IsRequired();
            entity.Property(item => item.ClpPerBrlQuote).HasColumnName("clp_per_brl_quote").HasPrecision(12, 4);
            entity.Ignore(item => item.TotalBRL);
            entity.Ignore(item => item.PaidAmount);
            entity.Ignore(item => item.RemainingAmount);
            entity.Ignore(item => item.ClpTotal);
        });

        modelBuilder.Entity<TripCostItem>(entity =>
        {
            entity.ToTable("trip_cost_items", table =>
            {
                table.HasCheckConstraint("CK_trip_cost_items_amounts", "[value_brl] >= 0 AND [fees_brl] >= 0 AND ([value_usd] IS NULL OR [value_usd] >= 0)");
                table.HasCheckConstraint("CK_trip_cost_items_installment_count", "[installment_count] >= 1");
            });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.TripBudgetId).HasColumnName("trip_budget_id");
            entity.Property(item => item.Description).HasColumnName("description").HasMaxLength(150).IsRequired();
            entity.Property(item => item.ValueBRL).HasColumnName("value_brl").HasPrecision(10, 2);
            entity.Property(item => item.FeesBRL).HasColumnName("fees_brl").HasPrecision(10, 2);
            entity.Property(item => item.ValueUSD).HasColumnName("value_usd").HasPrecision(10, 2);
            entity.Property(item => item.InstallmentCount).HasColumnName("installment_count");
            entity.Ignore(item => item.TotalBRL);
            entity.Ignore(item => item.PaidAmount);
            entity.Ignore(item => item.RemainingAmount);
            entity.HasOne(item => item.TripBudget)
                .WithMany(budget => budget.Items)
                .HasForeignKey(item => item.TripBudgetId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_trip_cost_items_trip_budgets");
        });

        modelBuilder.Entity<TripInstallment>(entity =>
        {
            entity.ToTable("trip_installments", table => table.HasCheckConstraint(
                "CK_trip_installments_amount",
                "[amount] > 0"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.TripCostItemId).HasColumnName("trip_cost_item_id");
            entity.Property(item => item.DueDate).HasColumnName("due_date").HasColumnType("date");
            entity.Property(item => item.Amount).HasColumnName("amount").HasPrecision(10, 2);
            entity.HasOne(item => item.TripCostItem)
                .WithMany(costItem => costItem.Installments)
                .HasForeignKey(item => item.TripCostItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_trip_installments_trip_cost_items");
        });

        modelBuilder.Entity<TripPayment>(entity =>
        {
            entity.ToTable("trip_payments", table => table.HasCheckConstraint(
                "CK_trip_payments_amount",
                "[amount] > 0"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.TripCostItemId).HasColumnName("trip_cost_item_id");
            entity.Property(item => item.TripInstallmentId).HasColumnName("trip_installment_id");
            entity.Property(item => item.PaymentDate).HasColumnName("payment_date").HasColumnType("date");
            entity.Property(item => item.Amount).HasColumnName("amount").HasPrecision(10, 2);
            entity.HasOne(item => item.TripCostItem)
                .WithMany(costItem => costItem.Payments)
                .HasForeignKey(item => item.TripCostItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_trip_payments_trip_cost_items");
            entity.HasOne(item => item.TripInstallment)
                .WithMany(installment => installment.Payments)
                .HasForeignKey(item => item.TripInstallmentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_trip_payments_trip_installments");
        });

        modelBuilder.Entity<Entrada>(entity =>
        {
            entity.ToTable("entradas");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.DataPrevista).HasColumnName("data_prevista").HasColumnType("date");
            entity.Property(item => item.DataRecebimento).HasColumnName("data_recebimento").HasColumnType("date");
            entity.Property(item => item.Descricao).HasColumnName("descricao").HasMaxLength(150).IsRequired();
            entity.Property(item => item.Categoria).HasColumnName("categoria").HasMaxLength(50).IsRequired();
            entity.Property(item => item.ValorPrevisto).HasColumnName("valor_previsto").HasPrecision(10, 2);
            entity.Property(item => item.ValorRecebido).HasColumnName("valor_recebido").HasPrecision(10, 2);
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<ContaCasa>(entity =>
        {
            entity.ToTable("contas_casa");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.DataVencimento).HasColumnName("data_vencimento").HasColumnType("date");
            entity.Property(item => item.DataPagamento).HasColumnName("data_pagamento").HasColumnType("date");
            entity.Property(item => item.Descricao).HasColumnName("descricao").HasMaxLength(150).IsRequired();
            entity.Property(item => item.Categoria).HasColumnName("categoria").HasMaxLength(50).IsRequired();
            entity.Property(item => item.ValorPrevisto).HasColumnName("valor_previsto").HasPrecision(10, 2);
            entity.Property(item => item.ValorPago).HasColumnName("valor_pago").HasPrecision(10, 2);
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<CartaoLancamento>(entity =>
        {
            entity.ToTable("cartao_lancamentos");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.DataCompra).HasColumnName("data_compra").HasColumnType("date");
            entity.Property(item => item.Estabelecimento).HasColumnName("estabelecimento").HasMaxLength(150).IsRequired();
            entity.Property(item => item.Categoria).HasColumnName("categoria").HasMaxLength(50).IsRequired();
            entity.Property(item => item.NumeroParcela).HasColumnName("numero_parcela");
            entity.Property(item => item.TotalParcelas).HasColumnName("total_parcelas");
            entity.Property(item => item.ValorParcela).HasColumnName("valor_parcela").HasPrecision(10, 2);
            entity.Property(item => item.MesFatura).HasColumnName("mes_fatura").HasMaxLength(20).IsRequired();
            entity.Property(item => item.AnoFatura).HasColumnName("ano_fatura");
        });

        modelBuilder.Entity<DespesaVariavel>(entity =>
        {
            entity.ToTable("despesas_variaveis");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Data).HasColumnName("data").HasColumnType("date");
            entity.Property(item => item.Descricao).HasColumnName("descricao").HasMaxLength(150).IsRequired();
            entity.Property(item => item.Categoria).HasColumnName("categoria").HasMaxLength(50).IsRequired();
            entity.Property(item => item.Valor).HasColumnName("valor").HasPrecision(10, 2);
        });
    }
}