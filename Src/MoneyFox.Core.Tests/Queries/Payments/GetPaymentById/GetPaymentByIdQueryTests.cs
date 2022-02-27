﻿namespace MoneyFox.Core.Tests.Queries.Payments.GetPaymentById
{
    using Core._Pending_.Common.Interfaces;
    using Core._Pending_.Exceptions;
    using Core.Aggregates;
    using Core.Aggregates.Payments;
    using Core.Queries.Payments.GetPaymentById;
    using FluentAssertions;
    using Infrastructure;
    using MoneyFox.Infrastructure.Persistence;
    using Moq;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Xunit;

    [ExcludeFromCodeCoverage]
    public class GetPaymentByIdQueryTests : IDisposable
    {
        private readonly AppDbContext context;
        private readonly Mock<IContextAdapter> contextAdapterMock;

        public GetPaymentByIdQueryTests()
        {
            context = InMemoryAppDbContextFactory.Create();

            contextAdapterMock = new Mock<IContextAdapter>();
            contextAdapterMock.SetupGet(x => x.Context).Returns(context);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) => InMemoryAppDbContextFactory.Destroy(context);

        [Fact]
        public async Task GetCategory_CategoryNotFound() =>
            // Arrange
            // Act / Assert
            await Assert.ThrowsAsync<PaymentNotFoundException>(
                async () =>
                    await new GetPaymentByIdQuery.Handler(contextAdapterMock.Object).Handle(
                        new GetPaymentByIdQuery(999),
                        default));

        [Fact]
        public async Task GetCategory_CategoryFound()
        {
            // Arrange
            var payment1 = new Payment(DateTime.Now, 20, PaymentType.Expense, new Account("test", 80));
            await context.AddAsync(payment1);
            await context.SaveChangesAsync();

            // Act
            Payment result =
                await new GetPaymentByIdQuery.Handler(contextAdapterMock.Object).Handle(
                    new GetPaymentByIdQuery(payment1.Id),
                    default);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(payment1.Id);
        }
    }
}