using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Telerik.Windows.Controls;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Find the ScrollViewer in the RadTabControl's template
            var scrollViewer = radTabControl.Template.FindName("TabControlScroller", radTabControl) as ScrollViewer;
            if (scrollViewer != null)
            {
                // Find the RepeatButtons for scrolling left and right
                var scrollLeftButton = radTabControl.Template.FindName("ScrollLeftButton", radTabControl) as RepeatButton;
                var scrollRightButton = radTabControl.Template.FindName("ScrollRightButton", radTabControl) as RepeatButton;

                if (scrollLeftButton != null && scrollRightButton != null)
                {
                    // Attach click event handlers
                    scrollLeftButton.Click += ScrollLeftButton_Click;
                    scrollRightButton.Click += ScrollRightButton_Click;
                }
            }
        }

        private void ScrollLeftButton_Click(object sender, RoutedEventArgs e)
        {
            // Select the previous tab
            int newIndex = radTabControl.SelectedIndex - 1;
            if (newIndex < 0)
                newIndex = radTabControl.Items.Count - 1; // Wrap around to the last tab
            radTabControl.SelectedIndex = newIndex;

            // Optionally scroll to the selected tab
            EnsureTabVisible(newIndex);
        }

        private void ScrollRightButton_Click(object sender, RoutedEventArgs e)
        {
            // Select the next tab
            int newIndex = radTabControl.SelectedIndex + 1;
            if (newIndex >= radTabControl.Items.Count)
                newIndex = 0; // Wrap around to the first tab
            radTabControl.SelectedIndex = newIndex;

            // Optionally scroll to the selected tab
            EnsureTabVisible(newIndex);
        }

        private void EnsureTabVisible(int index)
        {
            var scrollViewer = radTabControl.Template.FindName("TabControlScroller", radTabControl) as ScrollViewer;
            if (scrollViewer != null)
            {
                // Calculate the offset to make the selected tab visible
                double offset = index * (scrollViewer.ScrollableWidth / radTabControl.Items.Count);
                scrollViewer.ScrollToHorizontalOffset(offset) ;
            }
        }
    }
}
