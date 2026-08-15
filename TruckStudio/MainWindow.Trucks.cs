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
    }
}
