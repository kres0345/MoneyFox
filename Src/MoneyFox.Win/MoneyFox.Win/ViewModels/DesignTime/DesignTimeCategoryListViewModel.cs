﻿using CommunityToolkit.Mvvm.Input;
using MoneyFox.Win.Groups;
using MoneyFox.Win.ViewModels.Categories;
using System.Collections.ObjectModel;

namespace MoneyFox.Win.ViewModels.DesignTime
{
    public class DesignTimeCategoryListViewModel : ICategoryListViewModel
    {
        public ObservableCollection<AlphaGroupListGroupCollection<CategoryViewModel>> CategoryList
            => new ObservableCollection<AlphaGroupListGroupCollection<CategoryViewModel>>
            {
                new AlphaGroupListGroupCollection<CategoryViewModel>("A")
                {
                    new CategoryViewModel { Name = "Auto" }
                },
                new AlphaGroupListGroupCollection<CategoryViewModel>("E")
                {
                    new CategoryViewModel { Name = "Einkaufen" }
                }
            };

        public RelayCommand AppearingCommand { get; } = null!;

        public RelayCommand<CategoryViewModel> ItemClickCommand { get; } = null!;

        public AsyncRelayCommand<string> SearchCommand { get; } = null!;

        public CategoryViewModel SelectedCategory { get; set; } = null!;

        public string SearchText { get; set; } = "";

        public bool IsCategoriesEmpty => false;
    }
}