using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace Ams2CareerCompanion.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        SystemCommands.MinimizeWindow(this);
    }

    private void MaximizeRestoreButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
            return;
        }

        SystemCommands.MaximizeWindow(this);
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TopBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }

        try
        {
            if (WindowState == WindowState.Maximized)
            {
                var cursorInWindow = e.GetPosition(this);
                var horizontalRatio = ActualWidth > 0
                    ? cursorInWindow.X / ActualWidth
                    : 0.5;

                var restoreBounds = RestoreBounds;
                var cursorOnScreen = PointToScreen(cursorInWindow);

                WindowState = WindowState.Normal;
                UpdateLayout();

                var restoredWidth = restoreBounds.Width > 0 ? restoreBounds.Width : ActualWidth;
                var restoredHeight = restoreBounds.Height > 0 ? restoreBounds.Height : ActualHeight;

                Left = cursorOnScreen.X - (restoredWidth * horizontalRatio);
                Top = Math.Max(0, cursorOnScreen.Y - Math.Min(18, restoredHeight * 0.08));
            }

            DragMove();
        }
        catch
        {
            // Ignore drag failures when initiated from non-draggable children.
        }
    }

    private void PageHostScrollViewer_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private void PortraitPickerListBox_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ListBox listBox)
        {
            return;
        }

        var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
        if (scrollViewer is null)
        {
            return;
        }

        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private static T? FindVisualChild<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var match = FindVisualChild<T>(child);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    public async Task ExportScreenshotsAsync(string outputDirectory)
    {
        if (DataContext is not ViewModels.MainViewModel viewModel)
        {
            return;
        }

        Directory.CreateDirectory(outputDirectory);

        var pages = new[]
        {
            (Index: 0, Name: "race-desk"),
            (Index: 2, Name: "garage"),
            (Index: 3, Name: "team-hq"),
            (Index: 5, Name: "history"),
            (Index: 6, Name: "settings")
        };

        foreach (var page in pages)
        {
            viewModel.SelectedPageIndex = page.Index;
            await Dispatcher.InvokeAsync(() => UpdateLayout(), System.Windows.Threading.DispatcherPriority.Loaded);
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render);

            var width = Math.Max(1, (int)Math.Ceiling(ActualWidth));
            var height = Math.Max(1, (int)Math.Ceiling(ActualHeight));
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(this);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            await using var stream = File.Create(Path.Combine(outputDirectory, $"{page.Name}.png"));
            encoder.Save(stream);
        }
    }
}
