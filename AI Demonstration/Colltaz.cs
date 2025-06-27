using System;
using System.Windows;
using System.Windows.Threading;

namespace ThreeButtonCounter
{
    public partial class MainWindow : Window
    {
        private double counter = 1;  // Changed from long to double
        private Random random = new Random();
        private DispatcherTimer autoTimer = null!;
        private bool isAutoRunning = false;
        private GraphWindow? graphWindow;
        private CheckWindow? checkWindow;

        public MainWindow()
        {
            InitializeComponent();
            UpdateCounterDisplay();
            SetupAutoTimer();
        }

        private void SetupAutoTimer()
        {
            autoTimer = new DispatcherTimer();
            autoTimer.Interval = TimeSpan.FromMilliseconds(50);
            autoTimer.Tick += AutoTimer_Tick;
        }

        private void UpdateCounterDisplay()
        {
            CounterDisplay.Text = counter.ToString("N0");
            
            // Update graph if it's open (convert double to long for graph)
            if (graphWindow != null && graphWindow.IsVisible)
            {
                graphWindow.AddValue((long)counter);
            }
        }

        private void AddRandomButton_Click(object sender, RoutedEventArgs e)
        {
            int randomNumber = random.Next(1, 2147483647);
            counter = randomNumber;
            UpdateCounterDisplay();
        }

        // New method for custom number input
        private void SetCustomButton_Click(object sender, RoutedEventArgs e)
        {
            string input = CustomNumberInput.Text.Trim();
            
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Please enter a number.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Remove any formatting (commas, spaces)
            input = input.Replace(",", "").Replace(" ", "");

            if (double.TryParse(input, out double customNumber))
            {
                if (customNumber <= 0)
                {
                    MessageBox.Show("Please enter a positive number greater than 0.", "Invalid Number", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (customNumber > double.MaxValue)
                {
                    MessageBox.Show("Number is not processable by the computer!", "Number Too Large", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (customNumber > long.MaxValue)
                {
                    MessageBox.Show("The graphing tool beyond the long maximum value will fail to work. You may still use the tool but will not gain anything substantial out of it.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                
                counter = customNumber;
                UpdateCounterDisplay();
                CustomNumberInput.Text = ""; // Clear the input box
            }
            else
            {
                MessageBox.Show("Invalid number format. Please enter a valid positive number.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MultiplyAddButton_Click(object sender, RoutedEventArgs e)
        {
            counter = counter * 3 + 1;
            UpdateCounterDisplay();
        }

        private void DivideButton_Click(object sender, RoutedEventArgs e)
        {
            counter = Math.Floor(counter / 2);  // Use Math.Floor to ensure integer division
            UpdateCounterDisplay();
        }

        private void AutoButton_Click(object sender, RoutedEventArgs e)
        {
            if (isAutoRunning)
            {
                StopAuto();
            }
            else
            {
                StartAuto();
            }
        }

        private void ShowGraphButton_Click(object sender, RoutedEventArgs e)
        {
            if (graphWindow == null || !graphWindow.IsVisible)
            {
                graphWindow = new GraphWindow();
                graphWindow.Show();
                graphWindow.AddValue((long)counter); // Convert to long for graph
            }
            else
            {
                graphWindow.Activate();
            }
        }

        private void ShowCheckButton_Click(object sender, RoutedEventArgs e)
        {
            if (checkWindow == null || !checkWindow.IsVisible)
            {
                checkWindow = new CheckWindow();
                checkWindow.Show();
            }
            else
            {
                checkWindow.Activate();
            }
        }

        private void StartAuto()
        {
            isAutoRunning = true;
            AutoButton.Content = "Stop Auto";
            AutoStatus.Text = "Auto: Running (Buttons are disabled in Auto Mode)";
            
            AddRandomButton.IsEnabled = false;
            MultiplyAddButton.IsEnabled = false;
            DivideButton.IsEnabled = false;
            SetCustomButton.IsEnabled = false;  // Disable custom input during auto mode
            CustomNumberInput.IsEnabled = false;
            
            autoTimer.Start();
        }

        private void StopAuto()
        {
            isAutoRunning = false;
            AutoButton.Content = "Start Auto";
            AutoStatus.Text = "Auto: Off";
            
            AddRandomButton.IsEnabled = true;
            MultiplyAddButton.IsEnabled = true;
            DivideButton.IsEnabled = true;
            SetCustomButton.IsEnabled = true;  // Re-enable custom input
            CustomNumberInput.IsEnabled = true;
            
            autoTimer.Stop();
        }

        private void AutoTimer_Tick(object? sender, EventArgs e)
        {
            if (counter == 1)
            {
                // Auto-save if checkbox is checked and graph window is open
                if (AutoSaveCheckBox.IsChecked == true && graphWindow != null && graphWindow.IsVisible)
                {
                    graphWindow.AutoSaveLog();
                }
                
                int randomNumber = random.Next(2, 100000000);
                counter = randomNumber;
                UpdateCounterDisplay();
                return;
            }

            if (counter % 2 == 1)
            {
                counter = counter * 3 + 1;
            }
            else
            {
                counter = Math.Floor(counter / 2);
            }

            UpdateCounterDisplay();
        }
    }
}