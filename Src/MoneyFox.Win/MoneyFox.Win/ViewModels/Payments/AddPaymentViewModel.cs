﻿namespace MoneyFox.Win.ViewModels.Payments;

using AutoMapper;
using CommunityToolkit.Mvvm.Input;
using Core._Pending_.Exceptions;
using Core.Aggregates.Payments;
using Core.Commands.Payments.CreatePayment;
using Core.Common.Interfaces;
using Core.Resources;
using MediatR;
using NLog;
using Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Queries;
using Utilities;

public class AddPaymentViewModel : ModifyPaymentViewModel
{
    private readonly IMediator mediator;
    private readonly IMapper mapper;
    private readonly IDialogService dialogService;

    public AddPaymentViewModel(
        IMediator mediator,
        IMapper mapper,
        IDialogService dialogService,
        INavigationService navigationService) : base(mediator, mapper, dialogService, navigationService)
    {
        this.mediator = mediator;
        this.mapper = mapper;
        this.dialogService = dialogService;
    }

    public PaymentType PaymentType { get; set; }

    public RelayCommand InitializeCommand => new(async () => await InitializeAsync());

    protected override async Task InitializeAsync()
    {
        Title = PaymentTypeHelper.GetViewTitleForType(PaymentType, false);
        AmountString = HelperFunctions.FormatLargeNumbers(SelectedPayment.Amount);
        SelectedPayment.Type = PaymentType;

        await base.InitializeAsync();

        SelectedPayment.ChargedAccount = ChargedAccounts.FirstOrDefault();

        if(SelectedPayment.IsTransfer)
        {
            SelectedItemChangedCommand.Execute(null);
            SelectedPayment.TargetAccount = TargetAccounts.FirstOrDefault();
        }
    }

    protected override async Task SavePaymentAsync()
    {
        try
        {
            IsBusy = true;
            var payment = new Payment(
                SelectedPayment.Date,
                SelectedPayment.Amount,
                SelectedPayment.Type,
                await mediator.Send(new GetAccountByIdQuery(
                    SelectedPayment.ChargedAccount.Id)),
                SelectedPayment.TargetAccount != null
                    ? await mediator.Send(new GetAccountByIdQuery(SelectedPayment.TargetAccount.Id))
                    : null,
                mapper.Map<Category>(SelectedPayment.Category),
                SelectedPayment.Note);

            if(SelectedPayment.IsRecurring && SelectedPayment.RecurringPayment != null)
            {
                payment.AddRecurringPayment(
                    SelectedPayment.RecurringPayment.Recurrence,
                    SelectedPayment.RecurringPayment.IsEndless
                        ? null
                        : SelectedPayment.RecurringPayment.EndDate);
            }

            await mediator.Send(new CreatePaymentCommand(payment));
        }
        catch(InvalidEndDateException)
        {
            await dialogService.ShowMessageAsync(Strings.InvalidEnddateTitle, Strings.InvalidEnddateMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
