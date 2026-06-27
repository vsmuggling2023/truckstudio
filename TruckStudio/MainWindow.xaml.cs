using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using TruckStudio.Core;

namespace TruckStudio
{
    public partial class MainWindow : Window
    {
        private string _currentSavePath;
        private string _currentSaveContent;

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
                    // Enable the editing panels
                    PanelProfileEdit.IsEnabled = true;
                    PanelProfileEdit.Opacity = 1.0;
                    PanelTrucksEdit.IsEnabled = true;
                    PanelTrucksEdit.Opacity = 1.0;
                    PanelJobsEdit.IsEnabled = true;
                    PanelJobsEdit.Opacity = 1.0;
                    PanelWorldEdit.IsEnabled = true;
                    PanelWorldEdit.Opacity = 1.0;
                    PanelTuningEdit.IsEnabled = true;
                    PanelTuningEdit.Opacity = 1.0;

                    // Extract actual data
                    TxtMoney.Text = SaveParser.ExtractMoney(_currentSaveContent);
                    TxtExperience.Text = SaveParser.ExtractXP(_currentSaveContent);
                    
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

            MessageBox.Show("All trucks and trailers have been fully repaired and refueled in the save file!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
            PageJobs.Visibility = Visibility.Collapsed;
            PageWorld.Visibility = Visibility.Collapsed;
            PageTuning.Visibility = Visibility.Collapsed;
        }

        private void NavTrucks_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "Trucks & Trailers";
            PageProfile.Visibility = Visibility.Collapsed;
            PageTrucks.Visibility = Visibility.Visible;
            PageJobs.Visibility = Visibility.Collapsed;
            PageWorld.Visibility = Visibility.Collapsed;
            PageTuning.Visibility = Visibility.Collapsed;
        }

        private void NavJobs_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "Jobs Market";
            PageProfile.Visibility = Visibility.Collapsed;
            PageTrucks.Visibility = Visibility.Collapsed;
            PageJobs.Visibility = Visibility.Visible;
            PageWorld.Visibility = Visibility.Collapsed;
            PageTuning.Visibility = Visibility.Collapsed;
        }

        private void NavWorld_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "World & Map";
            PageProfile.Visibility = Visibility.Collapsed;
            PageTrucks.Visibility = Visibility.Collapsed;
            PageJobs.Visibility = Visibility.Collapsed;
            PageWorld.Visibility = Visibility.Visible;
            PageTuning.Visibility = Visibility.Collapsed;
        }

        private void NavTuning_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "Pro Tuning";
            PageProfile.Visibility = Visibility.Collapsed;
            PageTrucks.Visibility = Visibility.Collapsed;
            PageJobs.Visibility = Visibility.Collapsed;
            PageWorld.Visibility = Visibility.Collapsed;
            PageTuning.Visibility = Visibility.Visible;
        }
    }
}
