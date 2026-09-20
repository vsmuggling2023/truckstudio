using System;
using System.Linq;
using System.Windows;
using TruckStudio.Core;

namespace TruckStudio
{
    public partial class MainWindow : Window
    {
        private void CheckConfigForTeleport()
        {
            try
            {
                string folder = _currentGame == GameType.ETS2 ? "Euro Truck Simulator 2" : "American Truck Simulator";
                string configPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), folder, "config.cfg");
                if (System.IO.File.Exists(configPath))
                {
                    string configContent = System.IO.File.ReadAllText(configPath);
                    bool hasConsole = configContent.Contains("uset g_console \"1\"");
                    bool hasDeveloper = configContent.Contains("uset g_developer \"1\"");

                    if (hasConsole && hasDeveloper)
                    {
                        PanelTeleportEnabled.Visibility = Visibility.Visible;
                        PanelTeleportDisabled.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        PanelTeleportEnabled.Visibility = Visibility.Collapsed;
                        PanelTeleportDisabled.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    PanelTeleportEnabled.Visibility = Visibility.Collapsed;
                    PanelTeleportDisabled.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                PanelTeleportEnabled.Visibility = Visibility.Collapsed;
                PanelTeleportDisabled.Visibility = Visibility.Visible;
            }
        }

        private void FixTrucks_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            _currentSaveContent = SaveParser.FixAllTrucksAndTrailers(_currentSaveContent);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox("All trucks and trailers have been fully repaired in the save file!", "¡Todos los camiones y remolques han sido reparados al 100%!", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefillFuel_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            _currentSaveContent = SaveParser.RefillFuel(_currentSaveContent);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox("All trucks have been refueled to 100%!", "¡Todos los camiones han sido reabastecidos al 100%!", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FixCargo_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            _currentSaveContent = SaveParser.FixCargoDamage(_currentSaveContent);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox("Current cargo damage has been reset to 0% in the save file!", "¡El daño de la carga activa ha sido restablecido al 0%!", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Teleport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            string x = "0";
            string y = "0";
            string z = "0";
            string rotation = null;

            // Try to read from cams.txt or bugs.txt
            {
                try
                {
                    string folder = _currentGame == GameType.ETS2 ? "Euro Truck Simulator 2" : "American Truck Simulator";
                    string docsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), folder);
                    string camsPath = System.IO.Path.Combine(docsPath, "cams.txt");
                    string bugsPath = System.IO.Path.Combine(docsPath, "bugs.txt");

                    string[] lines = null;
                    bool isBugsFormat = false;

                    if (System.IO.File.Exists(camsPath))
                    {
                        lines = System.IO.File.ReadAllLines(camsPath);
                    }
                    else if (System.IO.File.Exists(bugsPath))
                    {
                        lines = System.IO.File.ReadAllLines(bugsPath);
                        isBugsFormat = true;
                    }

                    if (lines != null && lines.Length > 0)
                    {
                        string lastLine = lines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l));
                        if (lastLine != null)
                        {
                            string removeSpaces = lastLine.Replace(" ", "");
                            var split = removeSpaces.Split(';');
                            
                            if (!isBugsFormat && split.Length >= 8)
                            {
                                x = split[1];
                                y = split[2];
                                z = split[3];
                                rotation = $"({split[4]}; {split[5]}, {split[6]}, {split[7]})";
                            }
                            else if (!isBugsFormat && split.Length >= 4)
                            {
                                x = split[1];
                                y = split[2];
                                z = split[3];
                            }
                            else if (isBugsFormat && split.Length >= 5)
                            {
                                x = split[2];
                                y = split[3];
                                z = split[4];
                            }
                        }
                    }
                }
                catch { }

                if (x == "0" && y == "0" && z == "0")
                {
                    ShowLocalizedMessageBox("Could not find coordinates in cams.txt or bugs.txt. Make sure you use Camera 0 to save your coordinates.", "No se encontraron coordenadas en cams.txt o bugs.txt. Asegúrate de usar la Cámara 0 para guardar tus coordenadas.", "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            _currentSaveContent = SaveParser.Teleport(_currentSaveContent, x, y, z, rotation);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox($"Truck teleported to coordinates: X:{x}, Y:{y}, Z:{z}!", $"¡Camión teletransportado a las coordenadas X:{x}, Y:{y}, Z:{z}!", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveCargoWeight_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            string weight = TxtCargoWeight.Text.Trim();
            if (string.IsNullOrEmpty(weight) || !double.TryParse(weight.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double _))
            {
                ShowLocalizedMessageBox("Please enter a valid weight in tons (numbers only).", "¡Por favor, ingresa un peso válido en toneladas (solo números)!", "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _currentSaveContent = SaveParser.SetCargoWeight(_currentSaveContent, weight);
                System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

                ShowLocalizedMessageBox("Active cargo weight has been updated successfully!", "¡El peso de la carga activa ha sido actualizado con éxito!", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update cargo weight: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveDeliveryTime_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            string timeStr = TxtDeliveryTime.Text.Trim();
            if (string.IsNullOrEmpty(timeStr) || !double.TryParse(timeStr.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double hours))
            {
                ShowLocalizedMessageBox("Please enter a valid time in hours (numbers only).", "¡Por favor, ingresa un tiempo válido en horas (solo números)!", "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (hours < 0)
            {
                ShowLocalizedMessageBox("Only positive numbers or zero are allowed.", "¡Solo se permiten números positivos o cero!", "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _currentSaveContent = SaveParser.SetDeliveryTime(_currentSaveContent, timeStr);
                System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

                ShowLocalizedMessageBox("Remaining delivery time has been updated successfully!", "¡El tiempo restante para la entrega ha sido actualizado con éxito!", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update delivery time: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static readonly string[] PlateColorChoices = new string[]
        {
            "FFFFFF", "111111", "BF2222", "F6C700", "1546A0",
            "177245", "E87722", "D9D9D9", "6B21A8", "0E7490"
        };

        public void PopulatePlateColorCombos()
        {
            PopulatePlateColorCombo(CbPlateBgColor);
            PopulatePlateColorCombo(CbPlateTextColor);

            // Same defaults as the reference tool: red plate, white text
            TxtLicensePlate.Text = "T-TOOLS";
            CbPlateBgColor.Text = "BF2222";
            CbPlateTextColor.Text = "FFFFFF";
            ChkPlateColoredMargin.IsChecked = false;
            ChkPlateApplyTrailer.IsChecked = false;
            UpdatePlatePreview();
        }

        private void PopulatePlateColorCombo(System.Windows.Controls.ComboBox combo)
        {
            foreach (string hex in PlateColorChoices)
            {
                var item = new System.Windows.Controls.ComboBoxItem { Tag = hex };
                var row = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                var swatch = new System.Windows.Shapes.Rectangle
                {
                    Width = 14,
                    Height = 14,
                    RadiusX = 2,
                    RadiusY = 2,
                    Fill = BrushFromHex(hex),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                var label = new System.Windows.Controls.TextBlock { Text = hex, VerticalAlignment = VerticalAlignment.Center };
                row.Children.Add(swatch);
                row.Children.Add(label);
                item.Content = row;
                combo.Items.Add(item);
            }
        }

        // Accepts "RRGGBB" or "#RRGGBB" (also AARRGGBB, last 6 chars win); falls back when invalid.
        private static string NormalizeHexColor(string input, string fallback)
        {
            if (string.IsNullOrWhiteSpace(input)) return fallback;
            string hex = input.Trim().TrimStart('#');
            if (hex.Length == 8) hex = hex.Substring(2);
            if (hex.Length != 6) return fallback;
            foreach (char c in hex)
            {
                if (!Uri.IsHexDigit(c)) return fallback;
            }
            return hex.ToUpperInvariant();
        }

        private static System.Windows.Media.SolidColorBrush BrushFromHex(string hex)
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#" + NormalizeHexColor(hex, "000000"));
            return new System.Windows.Media.SolidColorBrush(color);
        }

        private void UpdatePlatePreview()
        {
            try
            {
                string bg = NormalizeHexColor(ReadPlateColorInput(CbPlateBgColor), "FFFFFF");
                string tx = NormalizeHexColor(ReadPlateColorInput(CbPlateTextColor), "111111");

                PlatePreviewBorder.Background = BrushFromHex(bg);
                PlatePreviewBorder.BorderBrush = ChkPlateColoredMargin.IsChecked == true ? BrushFromHex(tx) : System.Windows.Media.Brushes.Transparent;
                PlatePreviewText.Foreground = BrushFromHex(tx);

                string text = TxtLicensePlate.Text.Trim();
                PlatePreviewText.Text = string.IsNullOrEmpty(text) ? " " : text;
            }
            catch { }
        }

        private static string ReadPlateColorInput(System.Windows.Controls.ComboBox combo)
        {
            if (!string.IsNullOrWhiteSpace(combo.Text)) return combo.Text;
            return (combo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string;
        }

        private void LicensePlate_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePlatePreview();
        }

        private void PlateColor_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdatePlatePreview();
        }

        private void PlateOptions_Changed(object sender, RoutedEventArgs e)
        {
            UpdatePlatePreview();
        }

        private void SaveLicensePlate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            // Characters that would break the SII markup
            string text = TxtLicensePlate.Text.Trim().Replace("\"", "").Replace("|", "").ToUpperInvariant();
            if (string.IsNullOrEmpty(text))
            {
                ShowLocalizedMessageBox("Please enter a text for the plate.", "Por favor, ingresa un texto para la matrícula.", "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string bg = NormalizeHexColor(ReadPlateColorInput(CbPlateBgColor), "FFFFFF");
            string tx = NormalizeHexColor(ReadPlateColorInput(CbPlateTextColor), "111111");
            bool colorMargin = ChkPlateColoredMargin.IsChecked == true;
            bool applyTrailer = ChkPlateApplyTrailer.IsChecked == true;

            if (SaveParser.ExtractLicensePlate(_currentSaveContent) == null)
            {
                ShowLocalizedMessageBox("Could not find a license plate accessory on the current truck in this save (modded truck?).",
                    "No se encontró un accesorio de matrícula en el camión actual de esta partida (¿camión modificado?).",
                    "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string updated = SaveParser.SetLicensePlate(_currentSaveContent, text, bg, tx, colorMargin, applyTrailer, out bool trailerApplied);
                if (updated == null)
                {
                    ShowLocalizedMessageBox("Could not find a license plate accessory on the current truck in this save (modded truck?).",
                        "No se encontró un accesorio de matrícula en el camión actual de esta partida (¿camión modificado?).",
                        "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _currentSaveContent = updated;
                System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

                string extraEn = (applyTrailer && !trailerApplied) ? "\n\nNo attached trailer with a plate was found, so only the truck was updated." : "";
                string extraEs = (applyTrailer && !trailerApplied) ? "\n\nNo se encontró remolque acoplado con matrícula, así que solo se actualizó el camión." : "";
                ShowLocalizedMessageBox("License plate updated successfully!" + extraEn,
                    "¡Matrícula actualizada con éxito!" + extraEs,
                    "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update license plate: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
