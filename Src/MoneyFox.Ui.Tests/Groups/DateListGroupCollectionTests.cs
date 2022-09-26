namespace MoneyFox.Ui.Tests.Groups;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Common.Groups;
using FluentAssertions;
using MoneyFox.Ui.ViewModels.Payments;
using Xunit;

[ExcludeFromCodeCoverage]
public class DateListGroupCollectionTests
{
    [Fact]
    public void CreateGroupReturnsCorrectGroup()
    {
        // Arrange
        List<PaymentViewModel> paymentList = new()
        {
            new PaymentViewModel { Id = 1, Date = DateTime.Now }, new PaymentViewModel { Id = 2, Date = DateTime.Now.AddMonths(-1) }
        };

        // Act
        List<DateListGroupCollection<PaymentViewModel>> createdGroup = DateListGroupCollection<PaymentViewModel>.CreateGroups(
            items: paymentList,
            getKey: s => s.Date.ToString(format: "D", provider: CultureInfo.CurrentCulture),
            getSortKey: s => s.Date);

        // Assert
        _ = createdGroup.Should().HaveCount(2);
        createdGroup[0][0].Id.Should().Be(1);
        createdGroup[1][0].Id.Should().Be(2);
    }
}
