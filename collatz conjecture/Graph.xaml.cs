using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace ThreeButtonCounter
{
    public partial class GraphWindow : Window
    {
        private List<long> currentSequence;
        private List<string> verboseLog;
        private const double MARGIN = 40;
        private long currentStartingNumber = 0;
        private DateTime sequenceStartTime;
        private bool autoFocus = true;
        private const double FOCUS_WINDOW_MULTIPLIER = 3.0; // Show 3x above and below current value


        public GraphWindow()
        {
            InitializeComponent();
            currentSequence = new List<long>();
            verboseLog = new List<string>();
            LogEvent("Graph window initialized");
        }
        
        private void GetScaleValues(out double maxValue, out double minValue)
        {
            if (autoFocus && currentSequence.Count > 0)
            {
                long currentValue = currentSequence.Last();
        
                // Create a dynamic window around the current value
                // This ensures the current value is always visible and centered
                double windowSize = Math.Max(currentValue * FOCUS_WINDOW_MULTIPLIER, 100); // Minimum window of 100
        
                maxValue = currentValue + windowSize;
                minValue = Math.Max(1, currentValue - windowSize); // Keep minimum at 1 to avoid zero/negative
        
                // If we have recent high values, expand the window to include them
                if (currentSequence.Count > 10)
                {
                    var recentValues = currentSequence.Skip(Math.Max(0, currentSequence.Count - 20)).ToList();
                    long recentMax = recentValues.Max();
                    long recentMin = recentValues.Min();
            
                    // Expand window if recent values go beyond our calculated range
                    if (recentMax > maxValue)
                        maxValue = recentMax + (recentMax * 0.2); // Add 20% padding above max
                    if (recentMin < minValue)
                        minValue = Math.Max(1, recentMin - (recentMin * 0.2)); // Add 20% padding below min
                }
            }
            else
            {
                // Auto focus is off - show full range from 0 to max
                maxValue = currentSequence.Count > 0 ? currentSequence.Max() : 1;
                minValue = 0;
            }
        }
        
        public void AddValue(long value)
        {
            var timestamp = DateTime.Now;
            
            // If this is a new sequence (value is random and sequence was empty or ended at 1)
            if (currentSequence.Count == 0 || (currentSequence.LastOrDefault() == 1 && value > 1))
            {
                if (currentSequence.Count > 0)
                {
                    LogEvent($"Previous sequence completed. Final length: {currentSequence.Count} steps");
                }
                
                // Start new sequence
                currentSequence.Clear();
                currentSequence.Add(value);
                currentStartingNumber = value;
                sequenceStartTime = timestamp;
                
                LogEvent($"Started new sequence");
                LogEvent($"Starting number: {value:N0}");
                LogEvent($"Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss.fff}");
                LogEvent($"Step 0: {value:N0}");
                
                UpdateSequenceInfo();
            }
            else
            {
                // Continue current sequence
                currentSequence.Add(value);
                int stepNumber = currentSequence.Count - 1;
                long previousValue = currentSequence[stepNumber - 1];
                
                string operation;
                if (previousValue == 1)
                {
                    operation = "New random start";
                }
                else if (previousValue % 2 == 1)
                {
                    operation = $"3×{previousValue:N0}+1";
                }
                else
                {
                    operation = $"{previousValue:N0}÷2";
                }
                
                LogEvent($"Step {stepNumber}: {value:N0} (from {operation})");
                
                if (value == 1)
                {
                    var duration = timestamp - sequenceStartTime;
                    LogEvent($"Number == 1");
                    LogEvent($"Total steps: {stepNumber}");
                    LogEvent($"Duration: {duration.TotalSeconds:F3} seconds");
                    LogEvent($"Max value in sequence: {currentSequence.Max():N0}");
                }
            }

            // Update the graph
            DrawGraph();
            UpdateSequenceInfo();
            UpdateCurrentValue(value);
        }

        private void LogEvent(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            verboseLog.Add($"[{timestamp}] {message}");
            LogStatusInfo.Text = $"Log: {verboseLog.Count} entries";
        }

        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();

            if (currentSequence.Count < 2) return;

            // Use the actual canvas size from the container
            double canvasWidth = GraphCanvas.ActualWidth > 0 ? GraphCanvas.ActualWidth : 800;
            double canvasHeight = GraphCanvas.ActualHeight > 0 ? GraphCanvas.ActualHeight : 600;

            // Always fit the graph to the available canvas size
            GraphCanvas.Width = canvasWidth;
            GraphCanvas.Height = canvasHeight;

            // Calculate scales to fit all data within the canvas
            CalculateScalesToFit(canvasWidth, canvasHeight);

            // Draw grid lines
            DrawGrid(canvasWidth, canvasHeight);

            // Draw the sequence
            DrawSequenceLine(canvasWidth, canvasHeight);

            // Draw points
            DrawPoints(canvasWidth, canvasHeight);

            // Update scale info
            UpdateScaleInfo();
        }

        private void CalculateScalesToFit(double canvasWidth, double canvasHeight)
        {
            if (currentSequence.Count == 0) return;

            double maxValue = currentSequence.Max();
            int maxSteps = currentSequence.Count - 1;

            // Calculate usable area within margins
            double usableWidth = canvasWidth - (2 * MARGIN);
            double usableHeight = canvasHeight - (2 * MARGIN);

            // Ensure we never divide by zero and fit everything within the canvas
            double xScale = maxSteps > 0 ? usableWidth / maxSteps : 1.0;
            double yScale = maxValue > 0 ? usableHeight / maxValue : 1.0;
        }

        private void DrawGrid(double canvasWidth, double canvasHeight)
        {
            var gridBrush = new SolidColorBrush(Color.FromArgb(100, 128, 128, 128));
            var axisBrush = Brushes.Black;

            // Draw X-axis
            var xAxis = new Line
            {
                X1 = MARGIN,
                Y1 = canvasHeight - MARGIN,
                X2 = canvasWidth - MARGIN,
                Y2 = canvasHeight - MARGIN,
                Stroke = axisBrush,
                StrokeThickness = 2
            };
            GraphCanvas.Children.Add(xAxis);

            // Draw Y-axis
            var yAxis = new Line
            {
                X1 = MARGIN,
                Y1 = MARGIN,
                X2 = MARGIN,
                Y2 = canvasHeight - MARGIN,
                Stroke = axisBrush,
                StrokeThickness = 2
            };
            GraphCanvas.Children.Add(yAxis);

            // Draw lightweight grid
            var gridLines = 10;
            
            // Vertical grid lines
            for (int i = 1; i < gridLines; i++)
            {
                double x = MARGIN + (i * (canvasWidth - 2 * MARGIN) / gridLines);
                var gridLine = new Line
                {
                    X1 = x, Y1 = MARGIN,
                    X2 = x, Y2 = canvasHeight - MARGIN,
                    Stroke = gridBrush, StrokeThickness = 1
                };
                GraphCanvas.Children.Add(gridLine);
            }

            // Horizontal grid lines
            for (int i = 1; i < gridLines; i++)
            {
                double y = MARGIN + (i * (canvasHeight - 2 * MARGIN) / gridLines);
                var gridLine = new Line
                {
                    X1 = MARGIN, Y1 = y,
                    X2 = canvasWidth - MARGIN, Y2 = y,
                    Stroke = gridBrush, StrokeThickness = 1
                };
                GraphCanvas.Children.Add(gridLine);
            }
        }

        private void DrawSequenceLine(double canvasWidth, double canvasHeight)
        {
            if (currentSequence.Count < 2) return;

            double maxValue = currentSequence.Max();
            int maxSteps = currentSequence.Count - 1;
            
            // Calculate scales to fit within canvas
            double usableWidth = canvasWidth - (2 * MARGIN);
            double usableHeight = canvasHeight - (2 * MARGIN);
            double xScale = maxSteps > 0 ? usableWidth / maxSteps : 1.0;
            double yScale = maxValue > 0 ? usableHeight / maxValue : 1.0;

            var polyline = new Polyline
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                Fill = null
            };

            for (int i = 0; i < currentSequence.Count; i++)
            {
                double x = MARGIN + (i * xScale);
                double y = (canvasHeight - MARGIN) - (currentSequence[i] * yScale);
                polyline.Points.Add(new Point(x, y));
            }

            GraphCanvas.Children.Add(polyline);
        }

        private void DrawPoints(double canvasWidth, double canvasHeight)
        {
            double maxValue = currentSequence.Max();
            int maxSteps = currentSequence.Count - 1;
            
            // Calculate scales to fit within canvas
            double usableWidth = canvasWidth - (2 * MARGIN);
            double usableHeight = canvasHeight - (2 * MARGIN);
            double xScale = maxSteps > 0 ? usableWidth / maxSteps : 1.0;
            double yScale = maxValue > 0 ? usableHeight / maxValue : 1.0;

            // Adaptive point size based on number of points
            double pointSize = Math.Max(2, Math.Min(8, 200.0 / currentSequence.Count));
            
            for (int i = 0; i < currentSequence.Count; i++)
            {
                double x = MARGIN + (i * xScale);
                double y = (canvasHeight - MARGIN) - (currentSequence[i] * yScale);

                var point = new Ellipse
                {
                    Width = pointSize,
                    Height = pointSize,
                    Fill = i == currentSequence.Count - 1 ? Brushes.Red : 
                           currentSequence[i] == 1 ? Brushes.Green : Brushes.DarkBlue
                };

                point.SetValue(Canvas.LeftProperty, x - pointSize/2);
                point.SetValue(Canvas.TopProperty, y - pointSize/2);
                
                GraphCanvas.Children.Add(point);
            }
        }

        public void AutoSaveLog()
        {
            try
            {
                string downloadsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                string fileName = System.IO.Path.Combine(downloadsPath, $"AutoSave_{DateTimeOffset.Now.ToUnixTimeSeconds()}.collatz");
                var logContent = new StringBuilder();
                logContent.AppendLine("Auto-Saved Log:");
                logContent.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logContent.AppendLine($"Total log entries: {verboseLog.Count}");
                logContent.AppendLine();
        
                if (currentSequence.Count > 0)
                {
                    logContent.AppendLine("=== Sequence Summary ===");
                    logContent.AppendLine($"Starting number: {currentStartingNumber:N0}");
                    logContent.AppendLine($"Final length: {currentSequence.Count} steps");
                    logContent.AppendLine($"Max value: {currentSequence.Max():N0}");
                    logContent.AppendLine($"Reached 1: Yes");
                    logContent.AppendLine();
            
                    logContent.AppendLine("Steps:");
                    for (int i = 0; i < currentSequence.Count; i++)
                    {
                        logContent.AppendLine($"Step {i}: {currentSequence[i]:N0}");
                    }
                    logContent.AppendLine();
                }
        
                logContent.AppendLine("=== Technical Log ===");
                foreach (var logEntry in verboseLog)
                {
                    logContent.AppendLine(logEntry);
                }

                File.WriteAllText(fileName, logContent.ToString());
                LogEvent($"Auto-saved log to: {fileName}");
                LogStatusInfo.Text = $"Log: Auto-saved ({verboseLog.Count} entries)";
            }
            catch (Exception ex)
            {
                LogEvent($"Error auto-saving log: {ex.Message}");
            }
        }
        
        private void UpdateSequenceInfo()
        {
            if (currentSequence.Count > 0)
            {
                long startValue = currentSequence.First();
                int steps = currentSequence.Count - 1;
                long maxValue = currentSequence.Max();
                
                SequenceInfo.Text = $"Starting: {startValue:N0}, Steps: {steps}";
                MaxValueInfo.Text = $"Max: {maxValue:N0}";
            }
        }

        private void UpdateCurrentValue(long value)
        {
            CurrentValueInfo.Text = $"Current: {value:N0}";
        }

        private void UpdateScaleInfo()
        {
            if (autoFocus)
            {
                ScaleInfo.Text = "Credits: PrincipalistDeveloper";
            }
            else
            {
                ScaleInfo.Text = "Credits: PrincipalistDeveloper";
            }
        }

        // Simplified zoom controls - now they just trigger auto-fit
        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("Auto-fit view requested");
            DrawGraph();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("Auto-fit view requested");
            DrawGraph();
        }

        private void FitToViewButton_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("Auto-fit view requested");
            DrawGraph();
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("Auto-fit view requested");
            DrawGraph();
        }

        private void SaveLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string downloadsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                string fileName = System.IO.Path.Combine(downloadsPath, $"{DateTimeOffset.Now.ToUnixTimeSeconds()}.collatz");
                var logContent = new StringBuilder();
                logContent.AppendLine("Log Begin:");
                logContent.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logContent.AppendLine($"Total log entries: {verboseLog.Count}");
                logContent.AppendLine();
                
                if (currentSequence.Count > 0)
                {
                    logContent.AppendLine("=== Current Sequence Summary ===");
                    logContent.AppendLine($"Starting number: {currentStartingNumber:N0}");
                    logContent.AppendLine($"Current length: {currentSequence.Count} steps");
                    logContent.AppendLine($"Max value: {currentSequence.Max():N0}");
                    logContent.AppendLine($"Current value: {currentSequence.Last():N0}");
                    logContent.AppendLine();
                    
                    logContent.AppendLine("=== Sequence Values ===");
                    for (int i = 0; i < currentSequence.Count; i++)
                    {
                        logContent.AppendLine($"Step {i}: {currentSequence[i]:N0}");
                    }
                    logContent.AppendLine();
                }
                
                logContent.AppendLine("=== Technical Log ===");
                foreach (var logEntry in verboseLog)
                {
                    logContent.AppendLine(logEntry);
                }

                File.WriteAllText(fileName, logContent.ToString());
                LogEvent($"Log saved to: {fileName}");
                LogStatusInfo.Text = $"Log: Saved ({verboseLog.Count} entries)";
                
                MessageBox.Show($"Log saved successfully!\n\nFile: {fileName}\nEntries: {verboseLog.Count}. Important Note: The file is saved under the extension .collatz. Simply open it with a text editor.", 
                              "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogEvent($"Error saving log: {ex.Message}");
                MessageBox.Show($"Error saving log file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            currentSequence.Clear();
            verboseLog.Clear();
            GraphCanvas.Children.Clear();
            SequenceInfo.Text = "Starting Number: 0, Steps: 0";
            MaxValueInfo.Text = "Max Value: 0";
            CurrentValueInfo.Text = "Current: 0";
            LogStatusInfo.Text = "Log: Ready";
            ScaleInfo.Text = "Scale: Auto-fit";
            
            LogEvent("Graph and log cleared");
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            
            if (currentSequence.Count > 0)
            {
                DrawGraph();
            }
        }
    }
}