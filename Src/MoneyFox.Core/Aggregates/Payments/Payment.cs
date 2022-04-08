﻿namespace MoneyFox.Core.Aggregates.Payments
{
    using _Pending_.Exceptions;
    using JetBrains.Annotations;
    using NLog;
    using System;
    using System.ComponentModel.DataAnnotations;
    using Common.Interfaces;
    using Serilog;

    public class Payment : EntityBase, IAggregateRoot
    {
        [UsedImplicitly]
        private Payment() { }

        public Payment(DateTime date,
            decimal amount,
            PaymentType type,
            Account chargedAccount,
            Account? targetAccount = null,
            Category? category = null,
            string note = "",
            RecurringPayment? recurringPayment = null)
        {
            AssignValues(date, amount, type, chargedAccount, targetAccount, category, note);

            ClearPayment();

            if(recurringPayment != null)
            {
                RecurringPayment = recurringPayment;
                IsRecurring = true;
            }
            CreationTime = DateTime.Now;
        }

        public int Id { get; [UsedImplicitly] private set; }

        public int? CategoryId { get; [UsedImplicitly] private set; }

        public DateTime Date { get; private set; }

        public decimal Amount { get; private set; }

        public bool IsCleared { get; private set; }

        public PaymentType Type { get; private set; }

        public string? Note { get; private set; }

        public bool IsRecurring { get; private set; }

        public virtual Category? Category { get; private set; }

        [Required] public virtual Account ChargedAccount { get; private set; } = null!;

        public virtual Account? TargetAccount { get; private set; }

        public virtual RecurringPayment? RecurringPayment { get; private set; }



        [Obsolete("Will be removed")]
        public DateTime? ModificationDate { get; private set; }

        [Obsolete("Will be removed")]
        public DateTime CreationTime { get; [UsedImplicitly] private set; }

        public void UpdatePayment(DateTime date,
            decimal amount,
            PaymentType type,
            Account chargedAccount,
            Account? targetAccount = null,
            Category? category = null,
            string note = "")
        {
            if(ChargedAccount == null)
            {
                throw new InvalidOperationException("Uninitialized property: " + nameof(ChargedAccount));
            }

            ChargedAccount.RemovePaymentAmount(this);
            TargetAccount?.RemovePaymentAmount(this);

            AssignValues(date, amount, type, chargedAccount, targetAccount, category, note);

            ClearPayment();

            ModificationDate = DateTime.Now;
        }

        private void AssignValues(DateTime date,
            decimal amount,
            PaymentType type,
            Account chargedAccount,
            Account? targetAccount,
            Category? category,
            string note)
        {
            Date = date;
            Amount = amount;
            Type = type;
            Note = note;
            ChargedAccount = chargedAccount ?? throw new AccountNullException();
            TargetAccount = type == PaymentType.Transfer ? targetAccount : null;
            Category = category;

            ModificationDate = DateTime.Now;
        }

        public void AddRecurringPayment(PaymentRecurrence recurrence, DateTime? endDate = null)
        {
            RecurringPayment = new RecurringPayment(
                Date,
                Amount,
                Type,
                recurrence,
                ChargedAccount,
                Note ?? "",
                endDate,
                TargetAccount,
                Category,
                Date);
            IsRecurring = true;

            ModificationDate = DateTime.Now;
        }

        public void RemoveRecurringPayment()
        {
            RecurringPayment = null;
            IsRecurring = false;
        }

        public void ClearPayment()
        {
            IsCleared = Date.Date <= DateTime.Today.Date;

            if(ChargedAccount == null)
            {
                throw new InvalidOperationException("Uninitialized property: " + nameof(ChargedAccount));
            }

            ChargedAccount.AddPaymentAmount(this);

            if(Type == PaymentType.Transfer)
            {
                if(TargetAccount == null)
                {
                    Log.Warning("Target Account on clearing was null for payment {Id}", Id);
                    return;
                }

                TargetAccount.AddPaymentAmount(this);
            }

            ModificationDate = DateTime.Now;
        }
    }
}
