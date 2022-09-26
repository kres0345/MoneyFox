namespace MoneyFox.Ui.Tests.ConverterLogic;

using System.Diagnostics.CodeAnalysis;
using Common.ConverterLogic;
using FluentAssertions;
using MoneyFox.Core.ApplicationCore.Domain.Aggregates.AccountAggregate;
using MoneyFox.Ui.ViewModels.Payments;
using Xunit;

[ExcludeFromCodeCoverage]
public class PaymentAmountConverterLogicTests
{
    [Theory]
    [InlineData(PaymentType.Income, "+", 2, 2)]
    [InlineData(PaymentType.Expense, "-", 2, 2)]
    [InlineData(PaymentType.Transfer, "-", 2, 2)]
    [InlineData(PaymentType.Transfer, "+", 2, 3)]
    public void GetCorrectSignForExpenseAndIncome(PaymentType type, string expectedResult, int chargedAccountId, int currentAccountId)
    {
        // Arrange
        var payment = new PaymentViewModel { Type = type, ChargedAccountId = chargedAccountId, CurrentAccountId = currentAccountId };

        // Act
        string result = PaymentAmountConverterLogic.GetAmountSign(payment);

        // Assert
        _ = result.Should().StartWith(expectedResult);
    }
}
