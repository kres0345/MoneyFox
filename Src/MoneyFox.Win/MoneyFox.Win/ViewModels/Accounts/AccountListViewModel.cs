﻿namespace MoneyFox.Win.ViewModels.Accounts;

using AutoMapper;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core._Pending_.Common.Facades;
using Core._Pending_.Common.Interfaces;
using Core._Pending_.Common.Messages;
using Core.Commands.Accounts.DeleteAccountById;
using Core.Queries.Accounts.GetAccountEndOfMonthBalance;
using Core.Queries.Accounts.GetExcludedAccount;
using Core.Queries.Accounts.GetIncludedAccount;
using Core.Resources;
using Groups;
using Interfaces;
using MediatR;
using NLog;
using Payments;
using Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

public class AccountListViewModel : ObservableRecipient, IAccountListViewModel
{
    private readonly Logger logManager = LogManager.GetCurrentClassLogger();

    private readonly IMediator mediator;
    private readonly IMapper mapper;
    private readonly IDialogService dialogService;
    private readonly ISettingsFacade settingsFacade;
    private readonly INavigationService navigationService;

    private bool isRunning;

    private ObservableCollection<AlphaGroupListGroupCollection<AccountViewModel>> accounts = new();

    public AccountListViewModel(IMediator mediator,
        IMapper mapper,
        IBalanceCalculationService balanceCalculationService,
        IDialogService dialogService,
        ISettingsFacade settingsFacade,
        INavigationService navigationService)
    {
        this.mediator = mediator;
        this.mapper = mapper;
        this.dialogService = dialogService;
        this.navigationService = navigationService;
        this.settingsFacade = settingsFacade;

        BalanceViewModel = new BalanceViewModel(balanceCalculationService);
        ViewActionViewModel = new AccountListViewActionViewModel(this.navigationService);
        IsActive = true;
    }

    protected override void OnActivated() =>
        Messenger.Register<AccountListViewModel, ReloadMessage>(
            this,
            (r, m) => r.LoadDataCommand.ExecuteAsync(null));

    protected override void OnDeactivated() => Messenger.Unregister<ReloadMessage>(this);

    public IBalanceViewModel BalanceViewModel { get; }

    public IAccountListViewActionViewModel ViewActionViewModel { get; }

    public ObservableCollection<AlphaGroupListGroupCollection<AccountViewModel>> Accounts
    {
        get => accounts;
        private set
        {
            if(accounts == value)
            {
                return;
            }

            accounts = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasNoAccounts));
        }
    }

    public bool HasNoAccounts => !Accounts.Any();

    public AsyncRelayCommand LoadDataCommand => new(LoadAsync);

    public RelayCommand<AccountViewModel> OpenOverviewCommand => new(GoToPaymentOverView);

    public RelayCommand<AccountViewModel> EditAccountCommand => new(EditAccount);

    public AsyncRelayCommand<AccountViewModel> DeleteAccountCommand => new(DeleteAsync);

    private void EditAccount(AccountViewModel accountViewModel) =>
        navigationService.Navigate<EditAccountViewModel>(accountViewModel.Id);

    private async Task LoadAsync()
    {
        try
        {
            if(isRunning)
            {
                return;
            }

            isRunning = true;
            await BalanceViewModel.UpdateBalanceCommand.ExecuteAsync(null!);

            var includedAlphaGroup =
                new AlphaGroupListGroupCollection<AccountViewModel>(Strings.IncludedAccountsHeader);
            includedAlphaGroup.AddRange(
                mapper.Map<List<AccountViewModel>>(await mediator.Send(new GetIncludedAccountQuery())));
            includedAlphaGroup.ForEach(
                async x =>
                    x.EndOfMonthBalance = await mediator.Send(new GetAccountEndOfMonthBalanceQuery(x.Id)));

            var excludedAlphaGroup =
                new AlphaGroupListGroupCollection<AccountViewModel>(Strings.ExcludedAccountsHeader);
            excludedAlphaGroup.AddRange(
                mapper.Map<List<AccountViewModel>>(await mediator.Send(new GetExcludedAccountQuery())));
            excludedAlphaGroup.ForEach(
                async x =>
                    x.EndOfMonthBalance = await mediator.Send(new GetAccountEndOfMonthBalanceQuery(x.Id)));

            Accounts.Clear();

            if(includedAlphaGroup.Any())
            {
                Accounts.Add(includedAlphaGroup);
            }

            if(excludedAlphaGroup.Any())
            {
                Accounts.Add(excludedAlphaGroup);
            }

            OnPropertyChanged(nameof(HasNoAccounts));
        }
        catch(Exception ex)
        {
            logManager.Error(ex);
            await dialogService.ShowMessageAsync(Strings.GeneralErrorTitle, ex.ToString());
        }
        finally
        {
            isRunning = false;
        }
    }

    private void GoToPaymentOverView(AccountViewModel accountViewModel)
    {
        if(accountViewModel == null)
        {
            return;
        }

        navigationService.Navigate<PaymentListViewModel>(accountViewModel.Id);
    }

    private async Task DeleteAsync(AccountViewModel accountToDelete)
    {
        if(accountToDelete == null)
        {
            return;
        }

        if(await dialogService.ShowConfirmMessageAsync(
               Strings.DeleteTitle,
               Strings.DeleteAccountConfirmationMessage))
        {
            await mediator.Send(new DeactivateAccountByIdCommand(accountToDelete.Id));
            logManager.Info("Account with Id {id} deleted.", accountToDelete.Id);

            Accounts.Clear();
            await LoadAsync();

            settingsFacade.LastDatabaseUpdate = DateTime.Now;
        }
    }
}