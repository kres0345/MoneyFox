﻿namespace MoneyFox.Tests.ViewModels.Settings
{
    using Core._Pending_.Common.Facades;
    using Core._Pending_.Common.Interfaces;
    using FluentAssertions;
    using MoneyFox.ViewModels.Settings;
    using NSubstitute;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using Xunit;

    [ExcludeFromCodeCoverage]
    public class SettingsViewModelTests
    {
        [Fact]
        public void CollectionNotNullAfterCtor()
        {
            // Arrange
            var settingsFacade = Substitute.For<ISettingsFacade>();
            var dialogService = Substitute.For<IDialogService>();

            // Act
            var viewModel = new SettingsViewModel(settingsFacade, dialogService);

            // Assert
            viewModel.AvailableCultures.Should().NotBeNull();
        }

        [Fact]
        public void UpdateSettingsOnSet()
        {
            // Arrange
            var settingsFacade = Substitute.For<ISettingsFacade>();
            var dialogService = Substitute.For<IDialogService>();
            var viewModel = new SettingsViewModel(settingsFacade, dialogService);

            // Act
            var newCulture = new CultureInfo("de-CH");
            viewModel.SelectedCulture = newCulture;

            // Assert
            settingsFacade.Received(1).DefaultCulture = newCulture.Name;
        }
    }
}