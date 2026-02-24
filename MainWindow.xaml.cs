using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    private bool _isPointerOverPopup;
    private bool _isApplyingUiState;

    public ObservableCollection<ShortcutCategoryView> DisplayCategories { get; } = new();

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
        ShortcutPopup.PlacementTarget = this;

        _isApplyingUiState = true;
        _settings = _settingsService.Load();
        ApplySettingsToWindow();
        _isApplyingUiState = false;

        LoadShortcuts();

        if (_settings.IsPinned)
        {
            OpenPopup();
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

        ApplyFilter();

        if (!string.IsNullOrWhiteSpace(_dataErrorMessage))
        {
            ShowStatus("ショートカットデータの読込に失敗しました。");
        }
    }

    private void ApplyFilter()
    {
        var query = (SearchTextBox.Text ?? string.Empty).Trim();

        DisplayCategories.Clear();

        foreach (var category in _allShortcuts.Categories)
        {
            var matchedItems = category.Items
                .Where(item => IsMatch(item, query))
                .ToList();

            if (matchedItems.Count == 0)
            {
                continue;
            }

            DisplayCategories.Add(new ShortcutCategoryView
            {
                Name = category.Name,
                Items = matchedItems
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

        NoResultTextBlock.Visibility =
            DisplayCategories.Count == 0 && string.IsNullOrWhiteSpace(_dataErrorMessage)
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private static bool IsMatch(ShortcutItem item, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return ContainsIgnoreCase(item.Name, query)
               || ContainsIgnoreCase(item.Keys, query)
               || ContainsIgnoreCase(item.Note, query);
    }

    private static bool ContainsIgnoreCase(string? source, string value)
    {
        return !string.IsNullOrWhiteSpace(source)
               && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void UpdatePinState(bool isPinned, bool persist)
    {
        _settings.IsPinned = isPinned;

        PinButton.Content = isPinned ? "📌" : "📍";
        PinToggleMenuItem.Header = isPinned ? "ピン留め: ON" : "ピン留め: OFF";

        if (isPinned)
        {
            OpenPopup();
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

    private void OpenPopup()
    {
        ShortcutPopup.IsOpen = true;
    }

    private void ClosePopup()
    {
        ShortcutPopup.IsOpen = false;
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

        if (!HeaderBorder.IsMouseOver && !_isPointerOverPopup)
        {
            ClosePopup();
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
        OpenPopup();
    }

    private void HeaderBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        SchedulePopupClose();
    }

    private void PopupRootBorder_MouseEnter(object sender, MouseEventArgs e)
    {
        _isPointerOverPopup = true;
        _closePopupTimer.Stop();
    }

    private void PopupRootBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        _isPointerOverPopup = false;
        SchedulePopupClose();
    }

    private void HeaderBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
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

    private void PinButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
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

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
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
        if (e.Key == Key.Escape && ShortcutPopup.IsOpen && !_settings.IsPinned)
        {
            ClosePopup();
            e.Handled = true;
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
