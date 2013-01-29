using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Samba.Domain.Models.Customers;
using Samba.Domain.Models.Tickets;
using Samba.Domain.Models.Transactions;
using Samba.Localization.Properties;
using Samba.Persistance.Data;

namespace Samba.Modules.BasicReports.Reports.AccountReport
{
    public abstract class AccountReportViewModelBase : ReportViewModelBase
    {
        protected override void CreateFilterGroups()
        {
            FilterGroups.Clear();
            FilterGroups.Add(CreateWorkPeriodFilterGroup());
        }

        protected IEnumerable<AccountData> GetBalancedAccounts(bool selectInternalAccounts)
        {
            var tickets = Dao.Query<Ticket>(x => x.CustomerId > 0, x => x.Payments);
            var paymentSum = tickets.GroupBy(x => x.CustomerId).Select(x =>
                new
                {
                    CustomerId = x.Key,
                    Amount = x.Sum(k => k.Payments.Where(y => y.PaymentType == 3).Sum(j => j.Amount))
                }).ToList();

            var transactions = Dao.Query<CashTransaction>().Where(x => x.CustomerId > 0);
            var transactionSum = transactions.GroupBy(x => x.CustomerId).Select(
                x =>
                new
                {
                    CustomerId = x.Key,
                    Amount = x.Sum(y => y.TransactionType == 1 ? y.Amount : 0 - y.Amount)
                }).ToList();

            var accountTransactions = Dao.Query<AccountTransaction>().Where(x => x.CustomerId > 0);
            var accountTransactionSum = accountTransactions.GroupBy(x => x.CustomerId).Select(
                x =>
                new
                {
                    CustomerId = x.Key,
                    Amount = x.Sum(y => y.TransactionType == 3 ? y.Amount : 0 - y.Amount)
                }).ToList();


            var customerIds = paymentSum.Select(x => x.CustomerId).Distinct();
            customerIds = customerIds.Union(transactionSum.Select(x => x.CustomerId).Distinct());
            customerIds = customerIds.Union(accountTransactionSum.Select(x => x.CustomerId).Distinct());

            var list = (from customerId in customerIds
                        let amount = transactionSum.Where(x => x.CustomerId == customerId).Sum(x => x.Amount)
                        let account = accountTransactionSum.Where(x => x.CustomerId == customerId).Sum(x => x.Amount)
                        let payment = paymentSum.Where(x => x.CustomerId == customerId).Sum(x => x.Amount)
                        select new { CustomerId = customerId, Amount = (amount + account + payment) })
                            .Where(x => x.Amount != 0).ToList();

            var cids = list.Select(x => x.CustomerId).ToList();

            var accounts = Dao.Select<Customer, AccountData>(
                    x => new AccountData { Id = x.Id, CustomerName = x.Name, PhoneNumber = x.PhoneNumber, Amount = 0 },
                    x => cids.Contains(x.Id) && x.InternalAccount == selectInternalAccounts);

            foreach (var accountData in accounts)
            {
                accountData.Amount = list.SingleOrDefault(x => x.CustomerId == accountData.Id).Amount;
            }

            return accounts;
        }

        public FlowDocument CreateReport(string reportHeader, bool? returnReceivables, bool selectInternalAccounts)
        {
            var report = new SimpleReport("8cm");
            report.AddHeader("Samba POS");
            report.AddHeader(reportHeader);
            report.AddHeader(string.Format(Resources.As_f, DateTime.Now));

            var accounts = GetBalancedAccounts(selectInternalAccounts);
            if (returnReceivables != null)
                accounts = returnReceivables.GetValueOrDefault(false) ?
                                accounts.Where(x => x.Amount < 0) :
                                accounts.Where(x => x.Amount > 0);

            report.AddColumTextAlignment("Tablo", TextAlignment.Left, TextAlignment.Left, TextAlignment.Right);
            report.AddColumnLength("Tablo", "35*", "35*", "30*");


            if (accounts.Count() > 0)
            {
                report.AddTable("Tablo", Resources.Accounts, "", "");

                var total = 0m;
                foreach (var account in accounts)
                {
                    total += Math.Abs(account.Amount);
                    report.AddRow("Tablo", account.PhoneNumber, account.CustomerName, Math.Abs(account.Amount).ToString(ReportContext.CurrencyFormat));
                }
                report.AddRow("Tablo", Resources.GrandTotal, "", total);
            }
            else
            {
                report.AddHeader(string.Format(Resources.NoTransactionsFoundFor_f, reportHeader));
            }

            return report.Document;
        }
    }
}
