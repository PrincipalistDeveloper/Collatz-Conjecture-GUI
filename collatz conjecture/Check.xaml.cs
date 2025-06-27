using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace ThreeButtonCounter
{
    public partial class CheckWindow : Window
    {
        private string? selectedFilePath;

        public CheckWindow()
        {
            InitializeComponent();
        }

        
        
        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Collatz File",
                Filter = "Collatz files (*.collatz)|*.collatz|All files (*.*)|*.*",
                DefaultExt = "collatz"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;
                FilePathText.Text = Path.GetFileName(selectedFilePath);
                FilePathText.Foreground = System.Windows.Media.Brushes.Black;
                FilePathText.FontStyle = FontStyles.Normal;
                ValidateButton.IsEnabled = true;
                ValidationStatus.Text = "File selected - Ready to validate";
                ValidationStatus.Foreground = System.Windows.Media.Brushes.Blue;
            }
        }
        
        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath))
                return;

            try
            {
                ValidationStatus.Text = "Validating...";
                ValidationStatus.Foreground = System.Windows.Media.Brushes.Orange;
                
                var result = ParseStepsSection(selectedFilePath);
                
                if (result.success)
                {
                    var validation = ValidateCollatzSequence(result.steps);
                    DisplayResults(result.steps, validation);
                }
                else
                {
                    ValidationStatus.Text = "File parsing failed";
                    ValidationStatus.Foreground = System.Windows.Media.Brushes.Red;
                    ResultsText.Text = result.errorMessage;
                }
            }
            catch (Exception ex)
            {
                ValidationStatus.Text = "Validation error";
                ValidationStatus.Foreground = System.Windows.Media.Brushes.Red;
                ResultsText.Text = $"Error: {ex.Message}";
            }
        }

        private (bool success, List<long> steps, string errorMessage) ParseStepsSection(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                var steps = new List<long>();
                bool inStepsSection = false;
                int lineNumber = 0;

                foreach (var line in lines)
                {
                    lineNumber++;
                    var trimmedLine = line.Trim();

                    // Look for the Steps section
                    if (trimmedLine.Equals("=== Sequence Values ===", StringComparison.OrdinalIgnoreCase) || 
                        trimmedLine.Equals("Steps:", StringComparison.OrdinalIgnoreCase))
                    {
                        inStepsSection = true;
                        continue;
                    }

                    // For other devs: Make sure to update this if you choose to change anything with the log section!
                    if (inStepsSection && (trimmedLine.StartsWith("===") || trimmedLine.EndsWith(":")))
                    {
                        if (trimmedLine.Equals("=== Technical Log ===", StringComparison.OrdinalIgnoreCase))
                        {
                            break; // End of Steps section
                        }
                    }

                    // Parse steps in the Steps section
                    if (inStepsSection && !string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        string numberStr = trimmedLine;

                        if (trimmedLine.Contains(":"))
                        {
                            var parts = trimmedLine.Split(':');
                            if (parts.Length >= 2)
                            {
                                numberStr = parts[1].Trim();
                            }
                        }
                        else if (trimmedLine.Contains(".") && char.IsDigit(trimmedLine[0]))
                        {
                            var dotIndex = trimmedLine.IndexOf('.');
                            if (dotIndex > 0)
                            {
                                numberStr = trimmedLine.Substring(dotIndex + 1).Trim();
                            }
                        }
                        
                        numberStr = numberStr.Replace(",", "").Replace(" ", "");

                        if (long.TryParse(numberStr, out long value))
                        {
                            steps.Add(value);
                        }
                        else if (!string.IsNullOrEmpty(numberStr))
                        {
                            return (false, steps, $"Invalid number format on line {lineNumber}: '{trimmedLine}'");
                        }
                    }
                }

                if (!inStepsSection)
                {
                    return (false, steps, "No '=== Sequence Values ===' section found in the file. The file must contain a section labeled '=== Sequence Values ===' with the sequence values.");
                }

                if (steps.Count == 0)
                {
                    return (false, steps, "No valid steps found in the Steps section.");
                }

                return (true, steps, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, new List<long>(), $"Error reading file: {ex.Message}");
            }
        }

        private (bool isValid, List<string> errors, Dictionary<string, object> stats) ValidateCollatzSequence(List<long> steps)
        {
            var errors = new List<string>();
            var stats = new Dictionary<string, object>();

            if (steps.Count == 0)
            {
                errors.Add("Empty sequence");
                return (false, errors, stats);
            }

            // Statistics
            stats["TotalSteps"] = steps.Count;
            stats["StartingNumber"] = steps[0];
            stats["EndingNumber"] = steps[steps.Count - 1];
            stats["MaxValue"] = steps.Max();
            stats["MinValue"] = steps.Min();
            
            for (int i = 0; i < steps.Count - 1; i++)
            {
                long current = steps[i];
                long next = steps[i + 1];
                int stepNumber = i + 1;

                // Check for invalid values
                if (current <= 0)
                {
                    errors.Add($"Step {stepNumber}: Invalid value {current:N0} (must be positive)");
                    continue;
                }

                // Calculate expected next value based on Collatz rules
                long expectedNext;
                string operation;

                if (current % 2 == 0)
                {
                    expectedNext = current / 2;
                    operation = "÷2";
                }
                else
                {
                    expectedNext = current * 3 + 1;
                    operation = "×3+1";
                }
                
                if (next != expectedNext)
                {
                    errors.Add($"Step {stepNumber}: {current:N0} {operation} = {expectedNext:N0}, but got {next:N0}");
                }
            }
            
            bool reaches1 = steps.Contains(1);
            stats["Reaches1"] = reaches1;

            if (reaches1)
            {
                int indexOf1 = steps.IndexOf(1);
                stats["StepsTo1"] = indexOf1 + 1;
                
                if (indexOf1 < steps.Count - 1)
                {
                    var remainingSteps = steps.Skip(indexOf1).ToList();
                    bool validCycle = ValidateCollatzCycle(remainingSteps);
                    stats["ValidCycleAfter1"] = validCycle;
                    
                    if (!validCycle)
                    {
                        errors.Add("Invalid cycle after reaching 1 (should be 1→4→2→1→4→2→1...)");
                    }
                }
            }
            
            CheckForTamperingIndicators(steps, errors);

            bool isValid = errors.Count == 0;
            return (isValid, errors, stats);
        }

        private bool ValidateCollatzCycle(List<long> stepsFrom1)
        {
            var expectedPattern = new long[] { 1, 4, 2 };
            
            for (int i = 0; i < stepsFrom1.Count; i++)
            {
                if (stepsFrom1[i] != expectedPattern[i % expectedPattern.Length])
                {
                    return false;
                }
            }
            return true;
        }

        private void CheckForTamperingIndicators(List<long> steps, List<string> errors)
        {

            for (int i = 1; i < steps.Count; i++)
            {
                long prev = steps[i - 1];
                long curr = steps[i];

                // Check for impossible transitions that don't follow either rule
                if (prev % 2 == 0) // Previous was even
                {
                    if (curr != prev / 2)
                    {
                        // Already caught by main validation, but this is a tampering indicator
                        continue;
                    }
                }
                else // Previous was odd
                {
                    if (curr != prev * 3 + 1)
                    {
                        // Already caught by main validation, but this is a tampering indicator
                        continue;
                    }
                }

                // Check for suspicious patterns (e.g., numbers that are too perfect)
                // This is just an example - you could add more sophisticated checks
                if (i > 2 && curr == steps[i - 2] && curr != 1 && curr != 2 && curr != 4)
                {
                    errors.Add($"Suspicious pattern detected: repeating value {curr:N0} at positions {i - 1} and {i + 1}");
                }
            }
        }

        private void DisplayResults(List<long> steps, (bool isValid, List<string> errors, Dictionary<string, object> stats) validation)
        {
            var result = new StringBuilder();
            
            result.AppendLine("=== Output ===");
            result.AppendLine();
            
            // File info
            result.AppendLine($"File: {Path.GetFileName(selectedFilePath)}");
            result.AppendLine($"Validation Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            result.AppendLine();
            
            // Statistics
            result.AppendLine("Sequence Statistics:");
            result.AppendLine($"Starting Number: {validation.stats["StartingNumber"]:N0}");
            result.AppendLine($"Total Steps: {validation.stats["TotalSteps"]:N0}");
            result.AppendLine($"Maximum Value: {validation.stats["MaxValue"]:N0}");
            result.AppendLine($"Minimum Value: {validation.stats["MinValue"]:N0}");
            
            if (validation.stats.ContainsKey("Reaches1") && (bool)validation.stats["Reaches1"])
            {
                result.AppendLine($"Steps to reach 1: {validation.stats["StepsTo1"]:N0}");
            }
            else
            {
                result.AppendLine("Sequence does not reach 1. (Likely due to stopping before the formula can run its course).");
            }
            
            result.AppendLine();
            
            // Validation Result
            if (validation.isValid)
            {
                result.AppendLine(".collatz file does not contain any mathematical errors.");
                ValidationStatus.Text = "File does not contain any mathematical errors";
                ValidationStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                result.AppendLine(".collatz file contains mathematical errors.");
                result.AppendLine($"Found {validation.errors.Count} error(s):");
                result.AppendLine();
                
                foreach (var error in validation.errors.Take(20))
                {
                    result.AppendLine($"  • {error}");
                }
                
                if (validation.errors.Count > 20)
                {
                    result.AppendLine($"  ... and {validation.errors.Count - 20} more errors");
                }
                
                ValidationStatus.Text = $"Validation failed with ({validation.errors.Count} error(s))";
                ValidationStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            
            result.AppendLine();
            result.AppendLine("=== Validation Results ===");
            
            ResultsText.Text = result.ToString();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            selectedFilePath = null;
            FilePathText.Text = "No file selected";
            FilePathText.Foreground = System.Windows.Media.Brushes.Gray;
            FilePathText.FontStyle = FontStyles.Italic;
            ValidateButton.IsEnabled = false;
            ValidationStatus.Text = "Ready to validate";
            ValidationStatus.Foreground = System.Windows.Media.Brushes.Blue;
            ResultsText.Text = string.Empty;
        }
    }
}