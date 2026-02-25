using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ShortcutHUD.Models;
using ShortcutHUD.Services;

namespace ShortcutHUD;

public partial class MainWindow : Window
{
    private readonly ShortcutDataService _shortcutDataService = new();
    private readonly SettingsService _settingsService = new();
    private readonly DispatcherTimer _closePopupTimer;
    private readonly DispatcherTimer _statusTimer;

    private AppSettings _settings = AppSettings.CreateDefault();
    private ShortcutRoot _allShortcuts = new();
    private string _dataErrorMessage = string.Empty;
    private bool _isPointerOverCategoryPopup;
    private bool _isPointerOverDetailPopup;
    private bool _isApplyingUiState;

    public ObservableCollection<ShortcutCategoryView> DisplayCategories { get; } = new();
    public ObservableCollection<ShortcutItem> CurrentCategoryItems { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _closePopupTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _closePopupTimer.Tick += ClosePopupTimer_Tick;

        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1600)
        };
        _statusTimer.Tick += StatusTimer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CategoryPopup.PlacementTarget = this;

        _isApplyingUiState = true;
        _settings = _settingsService.Load();
        ApplySettingsToWindow();
        _isApplyingUiState = false;

        LoadShortcuts();

        if (_settings.IsPinned)
        {
            OpenCategoryPopup();
        }
    }

    private void ApplySettingsToWindow()
    {
        if (double.IsFinite(_settings.WindowLeft))
        {
            Left = _settings.WindowLeft;
        }

        if (double.IsFinite(_settings.WindowTop))
        {
            Top = _settings.WindowTop;
        }

        var clampedOpacity = Math.Clamp(_settings.Opacity, 0.2, 1.0);
        Opacity = clampedOpacity;
        _settings.Opacity = clampedOpacity;

        UpdatePinState(_settings.IsPinned, persist: false);

        if (OpacityMenuSlider != null)
        {
            OpacityMenuSlider.Value = clampedOpacity * 100.0;
        }
    }

    private void LoadShortcuts()
    {
        var result = _shortcutDataService.LoadFromExecutableFolder();
        _allShortcuts = result.Data;
        _dataErrorMessage = result.ErrorMessage ?? string.Empty;

        RebuildCategoryList();

        if (!string.IsNullOrWhiteSpace(_dataErrorMessage))
        {
            ShowStatus("ショートカットデータの読込に失敗しました。");
        }
    }

    private void RebuildCategoryList()
    {
        DisplayCategories.Clear();

        foreach (var category in _allShortcuts.Categories)
        {
            var itemsCopy = category.Items is null ? new List<ShortcutItem>() : new List<ShortcutItem>(category.Items);

            DisplayCategories.Add(new ShortcutCategoryView
            {
                Name = category.Name,
                Items = itemsCopy
            });
        }

        if (!string.IsNullOrWhiteSpace(_dataErrorMessage))
        {
            InfoTextBlock.Text = _dataErrorMessage;
            InfoTextBlock.Visibility = Visibility.Visible;
        }
        else
        {
            InfoTextBlock.Text = string.Empty;
            InfoTextBlock.Visibility = Visibility.Collapsed;
        }

        NoCategoryTextBlock.Visibility =
            DisplayCategories.Count == 0 && string.IsNullOrWhiteSpace(_dataErrorMessage)
                ? Visibility.Visible
                : Visibility.Collapsed;

        CloseDetailPopup();
    }

    private void UpdatePinState(bool isPinned, bool persist)
    {
        _settings.IsPinned = isPinned;

        PinButton.Content = isPinned ? "📌" : "📍";
        PinToggleMenuItem.Header = isPinned ? "ピン留め: ON" : "ピン留め: OFF";

        if (isPinned)
        {
            OpenCategoryPopup();
        }
        else
        {
            SchedulePopupClose();
        }

        if (persist && !_isApplyingUiState)
        {
            SaveSettings();
        }
    }

    private void OpenCategoryPopup()
    {
        CategoryPopup.IsOpen = true;
    }

    private void CloseDetailPopup()
    {
        DetailPopup.IsOpen = false;
        CurrentCategoryItems.Clear();
        DetailHeaderTextBlock.Text = string.Empty;
        NoDetailTextBlock.Visibility = Visibility.Collapsed;
    }

    private void CloseAllPopups()
    {
        CloseDetailPopup();
        CategoryPopup.IsOpen = false;
    }

    private void SchedulePopupClose()
    {
        if (_settings.IsPinned)
        {
            return;
        }

        _closePopupTimer.Stop();
        _closePopupTimer.Start();
    }

    private void ClosePopupTimer_Tick(object? sender, EventArgs e)
    {
        _closePopupTimer.Stop();

        if (_settings.IsPinned)
        {
            return;
        }

        if (!HeaderBorder.IsMouseOver && !_isPointerOverCategoryPopup && !_isPointerOverDetailPopup)
        {
            CloseAllPopups();
        }
    }

    private void ShowStatus(string message)
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Visibility = Visibility.Visible;

        _statusTimer.Stop();
        _statusTimer.Start();
    }

    private void StatusTimer_Tick(object? sender, EventArgs e)
    {
        _statusTimer.Stop();
        StatusTextBlock.Visibility = Visibility.Collapsed;
        StatusTextBlock.Text = string.Empty;
    }

    private void HeaderBorder_MouseEnter(object sender, MouseEventArgs e)
    {
        _closePopupTimer.Stop();
        OpenCategoryPopup();
    }

    private void HeaderBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        SchedulePopupClose();
    }

    private void CategoryPopupBorder_MouseEnter(object sender, MouseEventArgs e)
    {
        _isPointerOverCategoryPopup = true;
        _closePopupTimer.Stop();
    }

    private void CategoryPopupBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        _isPointerOverCategoryPopup = false;
        SchedulePopupClose();
    }

    private void DetailPopupBorder_MouseEnter(object sender, MouseEventArgs e)
    {
        _isPointerOverDetailPopup = true;
        _closePopupTimer.Stop();
    }

    private void DetailPopupBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        _isPointerOverDetailPopup = false;
        SchedulePopupClose();
    }

    private void CategoryListBoxItem_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not ListBoxItem item || item.DataContext is not ShortcutCategoryView category)
        {
            return;
        }

        CurrentCategoryItems.Clear();
        foreach (var shortcut in category.Items)
        {
            CurrentCategoryItems.Add(shortcut);
        }

        DetailHeaderTextBlock.Text = category.Name;
        NoDetailTextBlock.Visibility = CurrentCategoryItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        DetailPopup.PlacementTarget = item;
        DetailPopup.IsOpen = true;
    }

    private void HeaderBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source && FindAncestor<Button>(source) is not null)
        {
            return;
        }

        try
        {
            DragMove();
            _settings.WindowLeft = Left;
            _settings.WindowTop = Top;
            SaveSettings();
        }
        catch
        {
        }
    }

    private static T? FindAncestor<T>(DependencyObject? child) where T : DependencyObject
    {
        var current = child;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        UpdatePinState(!_settings.IsPinned, persist: true);
    }

    private void PinToggleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        UpdatePinState(!_settings.IsPinned, persist: true);
    }

    private void HudContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        _isApplyingUiState = true;
        OpacityMenuSlider.Value = Opacity * 100.0;
        _isApplyingUiState = false;
    }

    private void OpacityMenuSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isApplyingUiState)
        {
            return;
        }

        var newOpacity = Math.Clamp(e.NewValue / 100.0, 0.2, 1.0);
        Opacity = newOpacity;
        _settings.Opacity = newOpacity;
        SaveSettings();
    }

    private void ReloadMenuItem_Click(object sender, RoutedEventArgs e)
    {
        LoadShortcuts();
        ShowStatus("ショートカットを再読込しました。");
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void ShortcutItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ShortcutItem item)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(item.Keys))
        {
            ShowStatus("コピー対象のキーが空です。");
            return;
        }

        try
        {
            Clipboard.SetText(item.Keys);
            ShowStatus($"コピー: {item.Keys}");
        }
        catch
        {
            ShowStatus("クリップボードへのコピーに失敗しました。");
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // ポリシー: ピンON中は常時表示を優先するため Esc では閉じない
        if (e.Key == Key.Escape && CategoryPopup.IsOpen && !_settings.IsPinned)
        {
            CloseAllPopups();
            e.Handled = true;
        }
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            CloseAllPopups();
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settings.Opacity = Opacity;

        SaveSettings();
    }

    private void SaveSettings()
    {
        _settingsService.Save(_settings);
    }
}
