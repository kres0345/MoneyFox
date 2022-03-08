﻿namespace MoneyFox.Win.Pages.Payments;

using Core.Resources;
using Microsoft.UI.Xaml.Navigation;
using ViewModels.Payments;

public sealed partial class EditPaymentPage
{
    public override string Header => Strings.EditPaymentTitle;

    private EditPaymentViewModel ViewModel => (EditPaymentViewModel)DataContext;

    public EditPaymentPage()
    {
        InitializeComponent();
        DataContext = ViewModelLocator.EditPaymentVm;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        var vm = (PaymentViewModel)e.Parameter;
        ViewModel.InitializeCommand.Execute(vm.Id);
    }
}