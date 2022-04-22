﻿namespace MoneyFox.Tests.ViewModels
{

    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using AutoMapper;
    using Core.Aggregates;
    using Core.Aggregates.CategoryAggregate;
    using Core.Common.Interfaces;
    using Core.Queries;
    using FluentAssertions;
    using MediatR;
    using MoneyFox.ViewModels.Categories;
    using NSubstitute;
    using Xunit;

    [ExcludeFromCodeCoverage]
    public class CategoryListViewModelTests
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;
        private readonly IDialogService dialogService;

        public CategoryListViewModelTests()
        {
            mediator = Substitute.For<IMediator>();
            mapper = Substitute.For<IMapper>();
            dialogService = Substitute.For<IDialogService>();
        }

        [Fact]
        public void ListNotNullOnCtor()
        {
            // Arrange
            // Act
            var viewModel = new CategoryListViewModel(mediator: mediator, mapper: mapper, dialogService: dialogService);

            // Assert
            viewModel.Categories.Should().NotBeNull();
        }

        [Fact]
        public async Task ItemLoadedInInit()
        {
            // Arrange
            mapper.Map<List<CategoryViewModel>>(Arg.Any<List<Category>>()).Returns(new List<CategoryViewModel> { new CategoryViewModel { Name = "asdf" } });
            var viewModel = new CategoryListViewModel(mediator: mediator, mapper: mapper, dialogService: dialogService);

            // Act
            await viewModel.InitializeAsync();

            // Assert
            await mediator.Received(1).Send(Arg.Any<GetCategoryBySearchTermQuery>());
            mapper.Received(1).Map<List<CategoryViewModel>>(Arg.Any<List<Category>>());
        }
    }

}
