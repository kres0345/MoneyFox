﻿using System;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.StartScreen;
using MoneyFox.Foundation.Constants;
using NLog;
using MoneyFox.Application.Resources;

namespace MoneyFox.Uwp.Services
{
    internal static class JumpListService
    {
        private const string INCOME_ICON = "ms-appx:///Assets/IncomeTileIcon.png";
        private const string EXPENSE_ICON = "ms-appx:///Assets/SpendingTileIcon.png";
        private const string TRANSFER_ICON = "ms-appx:///Assets/TransferTileIcon.png";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static async Task InitializeAsync()
        {
            if (!ApiInformation.IsTypePresent("Windows.UI.StartScreen.JumpList")) return;

            try
            {
                var jumpList = await JumpList.LoadCurrentAsync();
                jumpList.Items.Clear();
                jumpList.SystemGroupKind = JumpListSystemGroupKind.None;

                var listItemAddIncome = JumpListItem.CreateWithArguments(AppConstants.ADD_INCOME_TILE_ID,
                                                                         Strings.AddIncomeLabel);
                listItemAddIncome.Logo = new Uri(INCOME_ICON);
                jumpList.Items.Add(listItemAddIncome);

                var listItemAddSpending = JumpListItem.CreateWithArguments(AppConstants.ADD_EXPENSE_TILE_ID,
                                                                           Strings.AddExpenseLabel);
                listItemAddSpending.Logo = new Uri(EXPENSE_ICON);
                jumpList.Items.Add(listItemAddSpending);

                var listItemAddTransfer = JumpListItem.CreateWithArguments(AppConstants.ADD_TRANSFER_TILE_ID,
                                                                           Strings.AddTransferLabel);
                listItemAddTransfer.Logo = new Uri(TRANSFER_ICON);
                jumpList.Items.Add(listItemAddTransfer);

                await jumpList.SaveAsync();
            }
            catch(Exception ex)
            {
                logger.Warn(ex);
            }
        }
    }
}
