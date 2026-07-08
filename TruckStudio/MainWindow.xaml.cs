using System;
using System.Linq;
using System.Windows;
using TruckStudio.Core;

namespace TruckStudio
{
    public partial class MainWindow : Window
    {
        private string _currentSavePath;
        private string _currentSaveContent;
        private System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> _cityCompanies;
        private System.Collections.Generic.List<string> _cargoes;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var profiles = ProfileManager.GetProfiles();
            ProfileComboBox.ItemsSource = profiles;
            
            if (profiles.Count > 0)
            {
                ProfileComboBox.SelectedIndex = 0;
            }

            CheckConfigForTeleport();
        }

        private void CheckConfigForTeleport()
        {
            try
            {
                string configPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Euro Truck Simulator 2", "config.cfg");
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

        private void ProfileComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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
                    // Debug: export decrypted game.sii to desktop
                    string exportPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "game_debug.sii");
                    System.IO.File.WriteAllText(exportPath, _currentSaveContent);

                    // Enable the editing panels
                    PanelProfileEdit.IsEnabled = true;
                    PanelProfileEdit.Opacity = 1;
                    PanelTrucksEdit.IsEnabled = true;
                    PanelTrucksEdit.Opacity = 1;
                    PanelTuningEdit.IsEnabled = true;
                    PanelTuningEdit.Opacity = 1;

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

                    
                    MessageBox.Show("Profile successfully loaded! You can now edit.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to decrypt game.sii", "Error");
                }
            }
            else
            {
                MessageBox.Show("Please select a save first.", "Info");
            }
        }

        private void SaveProfileChanges_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            _currentSaveContent = SaveParser.SetMoney(_currentSaveContent, TxtMoney.Text);
            _currentSaveContent = SaveParser.SetXP(_currentSaveContent, TxtExperience.Text);
            
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show($"Successfully saved profile data!\nMoney: {TxtMoney.Text} €\nXP: {TxtExperience.Text}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FixTrucks_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            _currentSaveContent = SaveParser.FixAllTrucksAndTrailers(_currentSaveContent);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show("All trucks and trailers have been fully repaired in the save file!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefillFuel_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            _currentSaveContent = SaveParser.RefillFuel(_currentSaveContent);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show("All trucks have been refueled to 100%!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    string docsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Euro Truck Simulator 2");
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
                                if (split.Length >= 7)
                                {
                                    // Normally bugs.txt doesn't have a full quaternion, but just in case
                                    // we could construct a rotation, but without a full quaternion it might be wrong.
                                }
                            }
                        }
                    }
                }
                catch { }

                if (x == "0" && y == "0" && z == "0")
                {
                    MessageBox.Show("Could not find coordinates in cams.txt or bugs.txt. Make sure you use Camera 0 to save your coordinates.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            _currentSaveContent = SaveParser.Teleport(_currentSaveContent, x, y, z, rotation);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show($"Truck teleported to coordinates: X:{x}, Y:{y}, Z:{z}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FixCargo_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            _currentSaveContent = SaveParser.FixCargoDamage(_currentSaveContent);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show("Current cargo damage has been reset to 0% in the save file!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void UnlockGarages_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            _currentSaveContent = System.Text.RegularExpressions.Regex.Replace(_currentSaveContent, @"(?m)^(\s*garage_state:\s*)[^\r\n]*", "$1 6");
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show("All garages have been unlocked and upgraded to maximum size!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UnlockDealers_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Unlock Dealerships is very complex and will be fully ready in Phase 6!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MaxSkills_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            _currentSaveContent = SaveParser.MaxSkills(_currentSaveContent);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show("All player skills (ADR, Long Distance, etc) have been maxed out!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InfiniteFuel_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            // Setting fuel_relative to 25 gives roughly 17,500 liters of fuel for a 700L tank.
            // At ~3.1 km/l, this yields ~54,000 km of driving range, matching the user's request
            // while adding only ~15 tons of mass, avoiding the extreme Havok physics crash.
            _currentSaveContent = System.Text.RegularExpressions.Regex.Replace(_currentSaveContent, @"(?m)^(\s*fuel_relative:\s*)[^\r\n]*", "$1 25");
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show("Extended fuel (~50,000 km of driving range) applied to all your trucks!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RestoreFuel_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            _currentSaveContent = System.Text.RegularExpressions.Regex.Replace(_currentSaveContent, @"(?m)^(\s*fuel_relative:\s*)[^\r\n]*", "$1 1");
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show("Fuel restored to standard 100% capacity!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void NavProfile_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "Player Profile";
            PageProfile.Visibility = Visibility.Visible;
            PageTrucks.Visibility = Visibility.Collapsed;
            PageWorld.Visibility = Visibility.Collapsed;
            PageTuning.Visibility = Visibility.Collapsed;
            PageFreight.Visibility = Visibility.Collapsed;
        }

        private void NavTrucks_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "Trucks & Trailers";
            PageProfile.Visibility = Visibility.Collapsed;
            PageTrucks.Visibility = Visibility.Visible;
            PageWorld.Visibility = Visibility.Collapsed;
            PageTuning.Visibility = Visibility.Collapsed;
            PageFreight.Visibility = Visibility.Collapsed;
        }

        private void NavWorld_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "World & Map";
            PageProfile.Visibility = Visibility.Collapsed;
            PageTrucks.Visibility = Visibility.Collapsed;
            PageWorld.Visibility = Visibility.Visible;
            PageTuning.Visibility = Visibility.Collapsed;
            PageFreight.Visibility = Visibility.Collapsed;
        }

        private void NavTuning_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "Pro Tuning";
            PageProfile.Visibility = Visibility.Collapsed;
            PageTrucks.Visibility = Visibility.Collapsed;
            PageWorld.Visibility = Visibility.Collapsed;
            PageTuning.Visibility = Visibility.Visible;
            PageFreight.Visibility = Visibility.Collapsed;
        }

        private void NavFreight_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "Freight Market";
            PageProfile.Visibility = Visibility.Collapsed;
            PageTrucks.Visibility = Visibility.Collapsed;
            PageWorld.Visibility = Visibility.Collapsed;
            PageTuning.Visibility = Visibility.Collapsed;
            PageFreight.Visibility = Visibility.Visible;
        }

        private void CbSourceCity_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_cityCompanies == null) return;
            string selectedCity = CbSourceCity.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedCity) && _cityCompanies.ContainsKey(selectedCity))
            {
                var companies = _cityCompanies[selectedCity].OrderBy(c => c).ToList();
                CbSourceCompany.ItemsSource = companies;
                if (companies.Count > 0)
                {
                    CbSourceCompany.SelectedIndex = 0;
                }
            }
            else
            {
                CbSourceCompany.ItemsSource = null;
            }
        }


        private void CbDestCity_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_cityCompanies == null) return;
            string selectedCity = CbDestCity.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedCity) && _cityCompanies.ContainsKey(selectedCity))
            {
                var companies = _cityCompanies[selectedCity].OrderBy(c => c).ToList();
                CbDestCompany.ItemsSource = companies;
                if (companies.Count > 0)
                {
                    CbDestCompany.SelectedIndex = 0;
                }
            }
            else
            {
                CbDestCompany.ItemsSource = null;
            }
        }

        private void InjectJob_Click(object sender, RoutedEventArgs e)
        {
            SaveParser.Log("UI Clicked: Inject Custom Job");
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent))
            {
                SaveParser.Log("UI: Save path or content is null!");
                MessageBox.Show("Please load a save file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string sourceCity = CbSourceCity.SelectedItem as string;
            string sourceCompany = CbSourceCompany.SelectedItem as string;
            string destCity = CbDestCity.SelectedItem as string;
            string destCompany = CbDestCompany.SelectedItem as string;
            string cargo = CbCargo.SelectedItem as string;
            int urgency = CbUrgency.SelectedIndex; // 0 = Normal, 1 = Important, 2 = Urgent

            SaveParser.Log($"UI Selected: Source={sourceCity} ({sourceCompany}), Dest={destCity} ({destCompany}), Cargo={cargo}, Urgency={urgency}");

            if (string.IsNullOrEmpty(sourceCity) || string.IsNullOrEmpty(sourceCompany) ||
                string.IsNullOrEmpty(destCity) || string.IsNullOrEmpty(destCompany) ||
                string.IsNullOrEmpty(cargo))
            {
                SaveParser.Log("UI: Validation error!");
                MessageBox.Show("Please fill all job settings before injecting.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Always use 5000 km: this guarantees the game gives enough delivery time for ANY
            // route in ETS2 Europe (Algeciras → Saint Petersburg = ~4400 km is the longest).
            // expiration = gameTime + 5000*2 + 4320 = gameTime + 14320 min (~10 days game time)
            string distance = "5000";
            SaveParser.Log("UI: Distance fixed=5000 km");

            try
            {
                SaveParser.Log("UI: Calling InjectFreightJob...");
                _currentSaveContent = SaveParser.InjectFreightJob(_currentSaveContent, sourceCity, sourceCompany, destCity, destCompany, cargo, urgency, distance);
                
                SaveParser.Log($"UI: Writing updated content to file: {_currentSavePath}...");
                System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);
                SaveParser.Log("UI: File successfully written!");

                string urgencyText = (CbUrgency.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Normal";
                SaveParser.Log("UI: Showing success MessageBox...");
                MessageBox.Show($"Custom Job successfully injected!\nRoute: {sourceCity} ({sourceCompany}) -> {destCity} ({destCompany})\nCargo: {cargo}\nUrgency: {urgencyText}\nDistance: {distance} km\n\nLoad this save in Euro Truck Simulator 2 and check {sourceCompany} in {sourceCity}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                SaveParser.Log("UI: MessageBox dismissed");
            }
            catch (Exception ex)
            {
                SaveParser.Log($"UI: Exception occurred: {ex}");
                MessageBox.Show($"Failed to inject job: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
