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
                    // Debug: export decrypted game.sii to desktop (disabled in v0.2.2-alpha)
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

            _currentSaveContent = SaveParser.SetMoney(_currentSaveContent, TxtMoney.Text);
            _currentSaveContent = SaveParser.SetXP(_currentSaveContent, TxtExperience.Text);
            
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox($"Successfully saved profile data!\nMoney: {TxtMoney.Text}\nXP: {TxtExperience.Text}", $"¡Datos guardados con éxito!\nDinero: {TxtMoney.Text}\nXP: {TxtExperience.Text}", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
