using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TruckStudio.Core;

namespace TruckStudio
{
    public partial class MainWindow : Window
    {
        private void LoadProfilesForSelectedGame()
        {
            var profiles = ProfileManager.GetProfiles(_currentGame);
            ProfileComboBox.ItemsSource = profiles;
            
            if (profiles.Count > 0)
            {
                ProfileComboBox.SelectedIndex = 0;
            }
            else
            {
                ProfileComboBox.SelectedItem = null;
                SaveComboBox.ItemsSource = null;
            }

            // Reset profile edit panel
            PanelProfileEdit.IsEnabled = false;
            PanelProfileEdit.Opacity = 0.5;
            TxtMoney.Text = "";
            TxtExperience.Text = "";

            // Reset other edit panels
            PanelTrucksEdit.IsEnabled = false;
            PanelTrucksEdit.Opacity = 0.5;
            PanelWorldEdit.IsEnabled = false;
            PanelWorldEdit.Opacity = 0.5;
            PanelTuningEdit.IsEnabled = false;
            PanelTuningEdit.Opacity = 0.5;
            PanelFreightEdit.IsEnabled = false;
            PanelFreightEdit.Opacity = 0.5;

            // Update label and translations
            TranslateUI();

            CheckConfigForTeleport();
        }

        private void GameRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            if (RadioEts2.IsChecked == true)
            {
                _currentGame = GameType.ETS2;
            }
            else if (RadioAts.IsChecked == true)
            {
                _currentGame = GameType.ATS;
            }

            LoadProfilesForSelectedGame();
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is ETS2Profile selectedProfile)
            {
                SaveComboBox.ItemsSource = selectedProfile.Saves;
                if (selectedProfile.Saves != null && selectedProfile.Saves.Count > 0)
                {
                    SaveComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                SaveComboBox.ItemsSource = null;
            }
        }

        private void LoadSave_Click(object sender, RoutedEventArgs e)
        {
            if (SaveComboBox.SelectedItem is ETS2Save selectedSave)
            {
                _currentSavePath = System.IO.Path.Combine(selectedSave.SavePath, "game.sii");
                _currentSaveContent = SiiDecryptor.DecryptFile(_currentSavePath);
                
                if (_currentSaveContent != null)
                {
                    // Debug: export decrypted game.sii to desktop (disabled in v0.3.0-alpha)
                    // string exportPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "game_debug.sii");
                    // System.IO.File.WriteAllText(exportPath, _currentSaveContent);

                    // Enable the editing panels
                    PanelProfileEdit.IsEnabled = true;
                    PanelProfileEdit.Opacity = 1;
                    PanelTrucksEdit.IsEnabled = true;
                    PanelTrucksEdit.Opacity = 1;
                    PanelTuningEdit.IsEnabled = true;
                    PanelTuningEdit.Opacity = 1;
                    PanelWorldEdit.IsEnabled = true;
                    PanelWorldEdit.Opacity = 1;

                    TxtMoney.Text = SaveParser.ExtractMoney(_currentSaveContent);
                    TxtExperience.Text = SaveParser.ExtractXP(_currentSaveContent);
                    TxtCargoWeight.Text = SaveParser.ExtractCargoWeight(_currentSaveContent);
                    TxtDeliveryTime.Text = SaveParser.ExtractDeliveryTime(_currentSaveContent);

                    // Enable Freight Market panel
                    PanelFreightEdit.IsEnabled = true;
                    PanelFreightEdit.Opacity = 1;

                    // Parse cities, companies, and cargoes
                    _cityCompanies = SaveParser.ExtractCitiesAndCompanies(_currentSaveContent);
                    _cargoes = SaveParser.ExtractCargoes(_currentSaveContent);

                    // Populate ComboBoxes
                    var citiesList = _cityCompanies.Keys.OrderBy(c => c).ToList();
                    CbSourceCity.ItemsSource = citiesList;
                    CbDestCity.ItemsSource = citiesList;
                    CbCargo.ItemsSource = _cargoes;

                    if (citiesList.Count > 0)
                    {
                        CbSourceCity.SelectedIndex = 0;
                        CbDestCity.SelectedIndex = 0;
                    }
                    if (_cargoes.Count > 0)
                    {
                        CbCargo.SelectedIndex = 0;
                    }

                    
                    ShowLocalizedMessageBox("Profile successfully loaded! You can now edit.", "¡Perfil cargado con éxito! Ya puedes editar los valores.", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ShowLocalizedMessageBox("Failed to decrypt game.sii", "No se pudo desencriptar game.sii", "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                ShowLocalizedMessageBox("Please select a save first.", "Por favor, selecciona una partida primero.", "Info", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SaveProfileChanges_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;
            if (WarnIfGameRunning()) return;

            // Money and XP must be plain positive integers (no separators, no decimals).
            // Values above the caps overflow the game's money/level math and crash the game when buying a truck.
            string moneyText = TxtMoney.Text.Trim();
            string xpText = TxtExperience.Text.Trim();

            if (string.IsNullOrEmpty(moneyText) ||
                !long.TryParse(moneyText, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out long money) ||
                money < 0)
            {
                ShowLocalizedMessageBox("Please enter a valid amount of money (whole numbers only, no separators).", "¡Por favor, ingresa un monto de dinero válido (solo números enteros, sin separadores)!", "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            const long maxMoney = 2000000000L;
            if (money > maxMoney)
            {
                money = maxMoney;
            }

            if (string.IsNullOrEmpty(xpText) ||
                !long.TryParse(xpText, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out long xp) ||
                xp < 0)
            {
                ShowLocalizedMessageBox("Please enter a valid amount of experience (whole numbers only, no separators).", "¡Por favor, ingresa una cantidad de experiencia válida (solo números enteros, sin separadores)!", "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            const long maxXp = 10000000L;
            if (xp > maxXp)
            {
                xp = maxXp;
            }

            _currentSaveContent = SaveParser.SetMoney(_currentSaveContent, money.ToString());
            _currentSaveContent = SaveParser.SetXP(_currentSaveContent, xp.ToString());

            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox($"Successfully saved profile data!\nMoney: {money:N0}\nXP: {xp:N0}", $"¡Datos guardados con éxito!\nDinero: {money:N0}\nXP: {xp:N0}", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
