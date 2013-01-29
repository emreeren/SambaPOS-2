using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Samba.Domain;
using Samba.Domain.Models.Customers;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Domain.Models.Transactions;
using Samba.Localization.Properties;
using Samba.Persistance.Data;

namespace Samba.Services
{
    public class CashTransactionData
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public int PaymentType { get; set; }
        public int TransactionType { get; set; }
        public decimal Amount { get; set; }
        public string CustomerName { get; set; }
    }

    public class CashService
    {
        public dynamic GetCurrentCashOperationData()
        {
            if (AppServices.MainDataContext.CurrentWorkPeriod == null)
                return new[] { 0m, 0m, 0m };

            var startDate = AppServices.MainDataContext.CurrentWorkPeriod.StartDate;

            var cashAmount = Dao.Sum<Payment>(x => x.Amount,
                                                 x =>
                                                 x.PaymentType == (int)PaymentType.Cash &&
                                                 x.Date > startDate);

            var creditCardAmount = Dao.Sum<Payment>(x => x.Amount,
                                                 x =>
                                                 x.PaymentType == (int)PaymentType.CreditCard &&
                                                 x.Date > startDate);

            var ticketAmount = Dao.Sum<Payment>(x => x.Amount,
                                                 x =>
                                                 x.PaymentType == (int)PaymentType.Ticket &&
                                                 x.Date > startDate);

            return new[] { cashAmount, creditCardAmount, ticketAmount };
        }

        public void AddIncome(int customerId, decimal amount, string description, PaymentType paymentType)
        {
            AddTransaction(customerId, amount, description, paymentType, TransactionType.Income);
        }

        public void AddExpense(int customerId, decimal amount, string description, PaymentType paymentType)
        {
            AddTransaction(customerId, amount, description, paymentType, TransactionType.Expense);
        }

        public void AddLiability(int customerId, decimal amount, string description)
        {
            AddTransaction(customerId, amount, description, 0, TransactionType.Liability);
        }

        public void AddReceivable(int customerId, decimal amount, string description)
        {
            AddTransaction(customerId, amount, description, 0, TransactionType.Receivable);
        }

        public IEnumerable<CashTransaction> GetTransactions(WorkPeriod workPeriod)
        {
            Debug.Assert(workPeriod != null);
            if (workPeriod.StartDate == workPeriod.EndDate)
                return Dao.Query<CashTransaction>(x => x.Date >= workPeriod.StartDate);
            return Dao.Query<CashTransaction>(x => x.Date >= workPeriod.StartDate && x.Date < workPeriod.EndDate);
        }

        public IEnumerable<CashTransactionData> GetTransactionsWithCustomerData(WorkPeriod workPeriod)
        {
            var wp = new WorkPeriod() { StartDate = workPeriod.StartDate, EndDate = workPeriod.EndDate };
            if (wp.StartDate == wp.EndDate) wp.EndDate = DateTime.Now;
            using (var workspace = WorkspaceFactory.CreateReadOnly())
            {
                var lines = from ct in workspace.Queryable<CashTransaction>()
                            join customer in workspace.Queryable<Customer>() on ct.CustomerId equals customer.Id into ctC
                            from customer in ctC.DefaultIfEmpty()
                            where ct.Date >= wp.StartDate && ct.Date < wp.EndDate
                            select new { CashTransaction = ct, Customer = customer };

                return lines.ToList().Select(x => new CashTransactionData
                                       {
                                           Amount = x.CashTransaction.Amount,
                                           CustomerName = x.Customer != null ? x.Customer.Name : "",
                                           Date = x.CashTransaction.Date,
                                           Name = x.CashTransaction.Name,
                                           PaymentType = x.CashTransaction.PaymentType,
                                           TransactionType = x.CashTransaction.TransactionType
                                       });
            }
        }

        private static void AddTransaction(int customerId, decimal amount, string description, PaymentType paymentType, TransactionType transactionType)
        {
            using (var workspace = WorkspaceFactory.Create())
            {
                if (transactionType == TransactionType.Income || transactionType == TransactionType.Expense)
                {
                    var c = new CashTransaction
                    {
                        Amount = amount,
                        Date = DateTime.Now,
                        Name = description,
                        PaymentType = (int)paymentType,
                        TransactionType = (int)transactionType,
                        UserId = AppServices.CurrentLoggedInUser.Id,
                        CustomerId = customerId
                    };
                    workspace.Add(c);
                }
                else
                {
                    var c = new AccountTransaction
                    {
                        Amount = amount,
                        Date = DateTime.Now,
                        Name = description,
                        TransactionType = (int)transactionType,
                        UserId = AppServices.CurrentLoggedInUser.Id,
                        CustomerId = customerId
                    };
                    workspace.Add(c);
                }

                workspace.CommitChanges();
            }
        }

        public static decimal GetAccountBalance(int accountId)
        {
            using (var w = WorkspaceFactory.CreateReadOnly())
            {
                var p = w.Queryable<Ticket>().Where(x => x.CustomerId == accountId).SelectMany(x => x.Payments).Where(x => x.PaymentType == 3).Sum(x => (decimal?)x.Amount); //.Sum(x => x.Payments.Where(y => y.PaymentType == 3).Sum(y => y.Amount)));
                var a = w.Queryable<AccountTransaction>().Where(x => x.CustomerId == accountId).Sum(x => (decimal?)(x.TransactionType == 3 ? x.Amount : 0 - x.Amount));
                var t = w.Queryable<CashTransaction>().Where(x => x.CustomerId == accountId).Sum(x => (decimal?)(x.TransactionType == 1 ? x.Amount : 0 - x.Amount));
                return p.GetValueOrDefault(0) + a.GetValueOrDefault(0) + t.GetValueOrDefault(0);
            }

            //var paymentSum = Dao.Query<Ticket>(x => x.CustomerId == accountId, x => x.Payments).Sum(x => x.Payments.Where(y => y.PaymentType == 3).Sum(y => y.Amount));
            //var transactionSum = Dao.Query<CashTransaction>().Where(x => x.CustomerId == accountId).Sum(x => x.TransactionType == 1 ? x.Amount : 0 - x.Amount);
            //var accountTransactionSum = Dao.Query<AccountTransaction>().Where(x => x.CustomerId == accountId).Sum(x => x.TransactionType == 3 ? x.Amount : 0 - x.Amount);
            //return paymentSum + transactionSum + accountTransactionSum;
        }
    }
}
