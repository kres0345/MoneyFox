﻿namespace MoneyFox.Tests.Presentation.ViewModels.Budget
{

    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using MediatR;
    using MoneyFox.Core.ApplicationCore.UseCases.BudgetCreation;
    using MoneyFox.Core.Common.Extensions;
    using MoneyFox.Core.Common.Messages;
    using MoneyFox.Core.Interfaces;
    using MoneyFox.ViewModels.Budget;
    using NSubstitute;
    using TestFramework.Budget;
    using Xunit;

    public class AddBudgetViewModelTests
    {
        private const int CATEGORY_ID = 10;
        private readonly ISender sender;
        private readonly INavigationService navigationService;

        private readonly AddBudgetViewModel viewModel;

        public AddBudgetViewModelTests()
        {
            sender = Substitute.For<ISender>();
            navigationService = Substitute.For<INavigationService>();
            viewModel = new AddBudgetViewModel(sender: sender, navigationService: navigationService);
        }

        [Fact]
        public void AddsSelectedCategoryToList()
        {
            // Act
            var categorySelectedMessage = new CategorySelectedMessage(new CategorySelectedDataSet(categoryId: CATEGORY_ID, name: "Beer"));
            viewModel.Receive(categorySelectedMessage);

            // Assert
            viewModel.SelectedCategories.Should().HaveCount(1);
            viewModel.SelectedCategories.Should().Contain(c => c.CategoryId == CATEGORY_ID);
        }

        [Fact]
        public void IgnoresSelectedCategory_WhenEntryWithSameIdAlreadyInList()
        {
            // Act
            var categorySelectedMessage = new CategorySelectedMessage(new CategorySelectedDataSet(categoryId: CATEGORY_ID, name: "Beer"));
            viewModel.Receive(categorySelectedMessage);
            viewModel.Receive(categorySelectedMessage);

            // Assert
            viewModel.SelectedCategories.Should().HaveCount(1);
            viewModel.SelectedCategories.Should().Contain(c => c.CategoryId == CATEGORY_ID);
        }

        [Fact]
        public async Task SendsCorrectSaveCommand()
        {
            // Capture
            CreateBudget.Query? passedQuery = null;
            await sender.Send(Arg.Do<CreateBudget.Query>(q => passedQuery = q));

            // Arrange
            var testBudget = new TestData.DefaultBudget();
            viewModel.SelectedBudget.Name = testBudget.Name;
            viewModel.SelectedBudget.SpendingLimit = testBudget.SpendingLimit;

            // Act
            viewModel.SelectedCategories.AddRange(testBudget.Categories.Select(c => new BudgetCategoryViewModel(categoryId: c, name: "Category")));
            await viewModel.SaveBudgetCommand.ExecuteAsync(null);

            // Assert
            passedQuery.Should().NotBeNull();
            passedQuery!.Name.Should().Be(testBudget.Name);
            passedQuery.SpendingLimit.Should().Be(testBudget.SpendingLimit);
            passedQuery.Categories.Should().BeEquivalentTo(testBudget.Categories);
        }
    }

}
