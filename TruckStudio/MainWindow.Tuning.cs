using System;
using System.Windows;
using TruckStudio.Core;

namespace TruckStudio
{
    public partial class MainWindow : Window
    {
        private void MaxSkills_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            _currentSaveContent = SaveParser.MaxSkills(_currentSaveContent);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox("All player skills (ADR, Long Distance, etc) have been maxed out!", "¡Todas las habilidades del conductor (ADR, Larga Distancia, etc.) han sido maximizadas!", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InfiniteFuel_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            // Ask user for custom driving range / fuel amount
            string prompt = _currentLanguage == "es" ? "Ingresa la cantidad de combustible deseada en Litros / km (máx 35,000):" : "Enter desired fuel amount in Liters / km (max 35,000):";
            int? kms = ShowInputDialog(this, prompt, 35000);
            if (kms == null) return; // Cancelled

            // 35,000 corresponds to fuel_relative 35.0 (35,000 Liters in game)
            double fuelValue = kms.Value / 1000.0;

            _currentSaveContent = System.Text.RegularExpressions.Regex.Replace(
                _currentSaveContent,
                @"(?m)^(\s*fuel_relative:\s*)[^\r\n]*",
                $"$1 {fuelValue.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}");
            
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox($"Extended fuel (~{kms.Value:N0} L/km) applied to all your trucks!", $"¡Combustible extendido (~{kms.Value:N0} L/km) aplicado a todos tus camiones!", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static int? ShowInputDialog(Window owner, string prompt, int maxVal)
        {
            bool isDark = (owner as MainWindow)?._isDarkTheme ?? true;
            bool isEs = (owner as MainWindow)?._currentLanguage == "es";

            string bgColor = isDark ? "#0f172a" : "#f8fafc";
            string borderColor = isDark ? "#1e293b" : "#cbd5e1";
            string textColor = isDark ? "#cbd5e1" : "#475569";
            string inputColor = isDark ? "#0c1220" : "#ffffff";
            string inputText = isDark ? "#ffffff" : "#0f172a";
            string btnCancelColor = isDark ? "#334155" : "#cbd5e1";
            string btnCancelText = isDark ? "#ffffff" : "#0f172a";

            Window dialog = new Window
            {
                Title = isEs ? "Rango de Combustible Extendido" : "Extended Fuel Range",
                Width = 380,
                Height = 270,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                ResizeMode = ResizeMode.NoResize,
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(bgColor)),
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(borderColor)),
                BorderThickness = new Thickness(1.5)
            };

            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = prompt,
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(textColor)),
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            System.Windows.Controls.Grid.SetRow(textBlock, 0);
            grid.Children.Add(textBlock);

            var contentPanel = new System.Windows.Controls.StackPanel();

            var textBox = new System.Windows.Controls.TextBox
            {
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(inputColor)),
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(inputText)),
                BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(borderColor)),
                Padding = new Thickness(8),
                FontSize = 15,
                BorderThickness = new Thickness(1),
                Text = "35000",
                VerticalAlignment = VerticalAlignment.Center
            };
            contentPanel.Children.Add(textBox);

            var lblKm = new System.Windows.Controls.TextBlock
            {
                Text = isEs ? "Resultado aprox. en el juego (Litros / km):" : "Approximate in-game result (Liters / km):",
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(textColor)),
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 4)
            };
            contentPanel.Children.Add(lblKm);

            string calcBgColor = isDark ? "#1e293b" : "#e2e8f0";
            var calcTextBox = new System.Windows.Controls.TextBox
            {
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(calcBgColor)),
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10b981")),
                BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(borderColor)),
                Padding = new Thickness(8),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(1),
                IsEnabled = false,
                IsReadOnly = true,
                VerticalAlignment = VerticalAlignment.Center
            };
            contentPanel.Children.Add(calcTextBox);

            void UpdateCalculatedKm()
            {
                if (int.TryParse(textBox.Text, out int inputVal) && inputVal > 0)
                {
                    double approxKm = inputVal * 4.03115; // Match game multiplier (35,000 -> 141,090)
                    calcTextBox.Text = $"~{approxKm:N0}";
                }
                else
                {
                    calcTextBox.Text = "---";
                }
            }

            textBox.TextChanged += (s, e) => UpdateCalculatedKm();
            UpdateCalculatedKm();

            System.Windows.Controls.Grid.SetRow(contentPanel, 1);
            grid.Children.Add(contentPanel);

            // Set focus to the TextBox and select all text automatically
            textBox.Loaded += (s, e) => {
                textBox.Focus();
                textBox.SelectAll();
            };

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };
            System.Windows.Controls.Grid.SetRow(buttonPanel, 2);

            int? result = null;

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = isEs ? "Cancelar" : "Cancel",
                Width = 85,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(btnCancelColor)),
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(btnCancelText)),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            cancelButton.Click += (s, e) => dialog.Close();

            var applyButton = new System.Windows.Controls.Button
            {
                Content = isEs ? "Aplicar" : "Apply",
                Width = 85,
                Height = 32,
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3b82f6")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                IsDefault = true // Pressing Enter triggers this button
            };
            applyButton.Click += (s, e) =>
            {
                if (int.TryParse(textBox.Text, out int val) && val > 0 && val <= maxVal)
                {
                    result = val;
                    dialog.Close();
                }
                else
                {
                    string errTitle = isEs ? "Rango Inválido" : "Invalid Range";
                    string errMsg = isEs ? $"Por favor ingresa un número válido entre 1 y {maxVal:N0}." : $"Please enter a valid number between 1 and {maxVal:N0}.";
                    MessageBox.Show(errMsg, errTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(applyButton);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;
            dialog.ShowDialog();

            return result;
        }

        private void RestoreFuel_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            _currentSaveContent = System.Text.RegularExpressions.Regex.Replace(_currentSaveContent, @"(?m)^(\s*fuel_relative:\s*)[^\r\n]*", "$1 1");
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show("Fuel restored to standard 100% capacity!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
