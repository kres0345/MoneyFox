﻿namespace MoneyFox.Win.ViewModels.DataBackup;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core._Pending_.Common.Facades;
using Core.Common.Exceptions;
using Core.Common.Interfaces;
using Core.Interfaces;
using Core.Resources;
using Microsoft.AppCenter.Crashes;
using NLog;
using System;
using System.Threading.Tasks;
using Core.DbBackup;
using Serilog;

public class BackupViewModel : ObservableObject, IBackupViewModel
{
    private readonly IBackupService backupService;
    private readonly IConnectivityAdapter connectivity;
    private readonly IDialogService dialogService;
    private readonly ISettingsFacade settingsFacade;
    private readonly IToastService toastService;
    private bool backupAvailable;
    private UserAccount userAccount;

    private DateTime backupLastModified;
    private bool isLoadingBackupAvailability;

    public BackupViewModel(
        IBackupService backupService,
        IDialogService dialogService,
        IConnectivityAdapter connectivity,
        ISettingsFacade settingsFacade,
        IToastService toastService)
    {
        this.backupService = backupService;
        this.dialogService = dialogService;
        this.connectivity = connectivity;
        this.settingsFacade = settingsFacade;
        this.toastService = toastService;
    }

    public AsyncRelayCommand InitializeCommand => new(async () => await InitializeAsync());

    public AsyncRelayCommand LoginCommand => new(async () => await LoginAsync());

    public AsyncRelayCommand LogoutCommand => new(async () => await LogoutAsync());

    public AsyncRelayCommand BackupCommand => new(async () => await CreateBackupAsync());

    public AsyncRelayCommand RestoreCommand => new(async () => await RestoreBackupAsync());

    public DateTime BackupLastModified
    {
        get => backupLastModified;
        private set
        {
            if(backupLastModified == value)
            {
                return;
            }

            backupLastModified = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoadingBackupAvailability
    {
        get => isLoadingBackupAvailability;
        private set
        {
            if(isLoadingBackupAvailability == value)
            {
                return;
            }

            isLoadingBackupAvailability = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoggedIn => settingsFacade.IsLoggedInToBackupService;

    public bool BackupAvailable
    {
        get => backupAvailable;
        private set
        {
            if(backupAvailable == value)
            {
                return;
            }

            backupAvailable = value;
            OnPropertyChanged();
        }
    }

    public bool IsAutoBackupEnabled
    {
        get => settingsFacade.IsBackupAutouploadEnabled;
        set
        {
            if(settingsFacade.IsBackupAutouploadEnabled == value)
            {
                return;
            }

            settingsFacade.IsBackupAutouploadEnabled = value;
            OnPropertyChanged();
        }
    }

    public UserAccount UserAccount
    {
        get => userAccount;
        set
        {
            if(userAccount == value)
            {
                return;
            }

            userAccount = value;
            OnPropertyChanged();
        }
    }

    private async Task InitializeAsync() => await LoadedAsync();

    private async Task LoadedAsync()
    {
        if(!IsLoggedIn)
        {
            OnPropertyChanged(nameof(IsLoggedIn));
            return;
        }

        if(!connectivity.IsConnected)
        {
            await dialogService.ShowMessageAsync(Strings.NoNetworkTitle, Strings.NoNetworkMessage);
        }

        IsLoadingBackupAvailability = true;
        try
        {
            BackupAvailable = await backupService.IsBackupExistingAsync();
            BackupLastModified = await backupService.GetBackupDateAsync();
            UserAccount = await backupService.GetUserAccount();
        }
        catch(BackupAuthenticationFailedException ex)
        {
            await backupService.LogoutAsync();
            await dialogService.ShowMessageAsync(
                Strings.AuthenticationFailedTitle,
                Strings.ErrorMessageAuthenticationFailed);
        }
        catch(Exception ex)
        {
            if(ex.StackTrace == "4f37.717b")
            {
                await backupService.LogoutAsync();
                await dialogService.ShowMessageAsync(
                    Strings.AuthenticationFailedTitle,
                    Strings.ErrorMessageAuthenticationFailed);
            }

            Log.Error(ex, "Issue on loading backup view");
        }

        IsLoadingBackupAvailability = false;
    }

    private async Task LoginAsync()
    {
        if(!connectivity.IsConnected)
        {
            await dialogService.ShowMessageAsync(Strings.NoNetworkTitle, Strings.NoNetworkMessage);
        }

        try
        {
            await backupService.LoginAsync();
            UserAccount = await backupService.GetUserAccount();
        }
        catch(BackupOperationCanceledException)
        {
            await dialogService.ShowMessageAsync(Strings.CanceledTitle, Strings.LoginCanceledMessage);
        }
        catch(Exception ex)
        {
            await dialogService.ShowMessageAsync(
                Strings.LoginFailedTitle,
                string.Format(Strings.UnknownErrorMessage, ex.Message));
            Crashes.TrackError(ex);
        }

        OnPropertyChanged(nameof(IsLoggedIn));
        await LoadedAsync();
    }

    private async Task LogoutAsync()
    {
        try
        {
            await backupService.LogoutAsync();
        }
        catch(BackupOperationCanceledException)
        {
            await dialogService.ShowMessageAsync(Strings.CanceledTitle, Strings.LogoutCanceledMessage);
        }
        catch(Exception ex)
        {
            await dialogService.ShowMessageAsync(Strings.GeneralErrorTitle, ex.Message);
            Crashes.TrackError(ex);
        }

        // ReSharper disable once ExplicitCallerInfoArgument
        OnPropertyChanged(nameof(IsLoggedIn));
    }

    private async Task CreateBackupAsync()
    {
        if(!await ShowOverwriteBackupInfoAsync())
        {
            return;
        }

        await dialogService.ShowLoadingDialogAsync();

        try
        {
            await backupService.UploadBackupAsync(BackupMode.Manual);
            await toastService.ShowToastAsync(Strings.BackupCreatedMessage);

            BackupLastModified = DateTime.Now;
        }
        catch(BackupOperationCanceledException)
        {
            await dialogService.ShowMessageAsync(Strings.CanceledTitle, Strings.UploadBackupCanceledMessage);
        }
        catch(Exception ex)
        {
            await dialogService.ShowMessageAsync(Strings.BackupFailedTitle, ex.Message);
            Crashes.TrackError(ex);
        }

        await dialogService.HideLoadingDialogAsync();
    }

    private async Task RestoreBackupAsync()
    {
        if(!await ShowOverwriteDataInfoAsync())
        {
            return;
        }

        DateTime backupDate = await backupService.GetBackupDateAsync();
        if(settingsFacade.LastDatabaseUpdate <= backupDate || await ShowForceOverrideConfirmationAsync())
        {
            await dialogService.ShowLoadingDialogAsync();

            try
            {
                await backupService.RestoreBackupAsync(BackupMode.Manual);
                await toastService.ShowToastAsync(Strings.BackupRestoredMessage);
            }
            catch(BackupOperationCanceledException)
            {
                await dialogService.ShowMessageAsync(Strings.CanceledTitle, Strings.RestoreBackupCanceledMessage);
            }
            catch(Exception ex)
            {
                await dialogService.ShowMessageAsync(Strings.BackupFailedTitle, ex.Message);
                Crashes.TrackError(ex);
            }
        }
        else
        {
            Log.Information("Restore Backup canceled by the user due to newer local data");
        }

        await dialogService.HideLoadingDialogAsync();
    }

    private async Task<bool> ShowOverwriteBackupInfoAsync()
        => await dialogService.ShowConfirmMessageAsync(
            Strings.OverwriteTitle,
            Strings.OverwriteBackupMessage,
            Strings.YesLabel,
            Strings.NoLabel);

    private async Task<bool> ShowOverwriteDataInfoAsync()
        => await dialogService.ShowConfirmMessageAsync(
            Strings.OverwriteTitle,
            Strings.OverwriteDataMessage,
            Strings.YesLabel,
            Strings.NoLabel);

    private async Task<bool> ShowForceOverrideConfirmationAsync()
        => await dialogService.ShowConfirmMessageAsync(
            Strings.ForceOverrideBackupTitle,
            Strings.ForceOverrideBackupMessage,
            Strings.YesLabel,
            Strings.NoLabel);
}
