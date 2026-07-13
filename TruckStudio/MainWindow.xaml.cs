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

        private void VisitGarages_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            // status 2 = small garage (1 slot) — the minimum valid purchased state in ETS2.
            // Setting undiscovered garages (status 0) to 2 unlocks every city on the map
            // without giving you the full max upgrade, so you can still upgrade manually.
            _currentSaveContent = UpdateGarageStatus(_currentSaveContent, GarageMode.Visit);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show("All garages have been unlocked as Small (1-slot) garages!\nEvery city is now accessible. Use 'Upgrade All Owned' to max them out.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpgradeGarages_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            // Only upgrades garages you already own (status 2-5) to max (6).
            // Garages at 0 (undiscovered) are left untouched.
            _currentSaveContent = UpdateGarageStatus(_currentSaveContent, GarageMode.Upgrade);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show("All owned garages have been upgraded to maximum size (6 slots)!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BuyAllGarages_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            // Set every garage to status 6 = fully purchased + max upgraded.
            _currentSaveContent = UpdateGarageStatus(_currentSaveContent, GarageMode.BuyAll);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show("All garages have been purchased and upgraded to maximum size!\nYou now own every garage across the entire ETS2 map.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private enum GarageMode { Visit, Upgrade, BuyAll }

        /// <summary>
        /// Iterates every garage block in the save and updates its 'status' field.
        /// Visit   : Keep status unchanged (stays 0 if unowned), but adds city to visited_cities.
        /// Upgrade : status 2 (Small) or 6 (Tiny) → 3 (Large, max size). Skip 0.
        /// BuyAll  : status → 3 (Large, max size). Also adds city to visited_cities.
        /// Uses a state-machine to only touch 'status' inside garage blocks.
        /// </summary>
        private static string UpdateGarageStatus(string content, GarageMode mode)
        {
            var sb = new System.Text.StringBuilder(content.Length);
            string[] sep = { "\r\n", "\n" };
            string[] lines = content.Split(sep, StringSplitOptions.None);
            string nl = content.Contains("\r\n") ? "\r\n" : "\n";

            bool inGarage = false;

            foreach (var line in lines)
            {
                string t = line.TrimStart();

                if (!inGarage)
                {
                    // garage : garage.cityname {
                    if (t.StartsWith("garage") && t.Contains("garage.") && t.EndsWith("{"))
                        inGarage = true;
                    sb.Append(line).Append(nl);
                    continue;
                }

                // End of block
                if (t == "}")
                {
                    inGarage = false;
                    sb.Append(line).Append(nl);
                    continue;
                }

                // Rewrite status line
                if (t.StartsWith("status:"))
                {
                    int cur = 0;
                    int.TryParse(t.Substring(7).Trim(), out cur);

                    int next = cur;
                    switch (mode)
                    {
                        case GarageMode.Visit:
                            // Keep status at 0 (or whatever it is), only discover city in economy block
                            next = cur;
                            break;
                        case GarageMode.Upgrade:
                            // 2 = Small, 6 = Tiny, 4/5 = other intermediate states. 3 = Large (max).
                            if (cur == 2 || cur == 6 || cur == 4 || cur == 5)
                                next = 3;
                            break;
                        case GarageMode.BuyAll:
                            // Buy and fully upgrade to Large (3)
                            next = 3;
                            break;
                    }

                    string indent = line.Substring(0, line.Length - line.TrimStart().Length);
                    sb.Append(indent).Append("status: ").Append(next).Append(nl);
                    continue;
                }

                sb.Append(line).Append(nl);
            }

            // Preserve original trailing newline behaviour
            if (sb.Length >= nl.Length
                && sb.ToString(sb.Length - nl.Length, nl.Length) == nl
                && !content.EndsWith(nl))
                sb.Remove(sb.Length - nl.Length, nl.Length);

            string result = sb.ToString();

            // Visit and BuyAll both need to discover all cities to make garages purchasable online
            if (mode == GarageMode.Visit || mode == GarageMode.BuyAll)
            {
                result = UpdateVisitedCities(result);
            }

            return result;
        }

        /// <summary>
        /// Extracts all city names from garages in the save file and adds them to
        /// the visited_cities list inside the economy block.
        /// </summary>
        private static string UpdateVisitedCities(string content)
        {
            // 1. Find all cities with garages
            var cityMatches = System.Text.RegularExpressions.Regex.Matches(content, @"(?m)^\s*garages\[\d+\]:\s*garage\.([a-z0-9_]+)");
            var cities = new System.Collections.Generic.List<string>();
            foreach (System.Text.RegularExpressions.Match m in cityMatches)
            {
                string city = m.Groups[1].Value.Trim();
                if (!cities.Contains(city))
                    cities.Add(city);
            }
            cities.Sort();

            if (cities.Count == 0)
                return content;

            // 2. Find the economy block
            int econStart = content.IndexOf("economy : ");
            if (econStart == -1)
                return content;

            int econOpenBrace = content.IndexOf("{", econStart);
            if (econOpenBrace == -1)
                return content;

            // Find the closing brace of the economy block
            int econEnd = -1;
            int braceCount = 1;
            for (int i = econOpenBrace + 1; i < content.Length; i++)
            {
                if (content[i] == '{') braceCount++;
                else if (content[i] == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        econEnd = i;
                        break;
                    }
                }
            }

            if (econEnd == -1)
                return content;

            string econBlock = content.Substring(econOpenBrace, econEnd - econOpenBrace + 1);

            // 3. Remove existing visited_cities / visited_cities_count lines from the economy block
            string[] sep = { "\r\n", "\n" };
            string[] econLines = econBlock.Split(sep, StringSplitOptions.None);
            string nl = econBlock.Contains("\r\n") ? "\r\n" : "\n";
            var cleanEconLines = new System.Collections.Generic.List<string>();

            foreach (var line in econLines)
            {
                string t = line.Trim();
                if (t.StartsWith("visited_cities:") || 
                    t.StartsWith("visited_cities[") || 
                    t.StartsWith("visited_cities_count:") || 
                    t.StartsWith("visited_cities_count["))
                {
                    continue;
                }
                cleanEconLines.Add(line);
            }

            // Remove the closing brace line so we can append our new lists before it
            if (cleanEconLines.Count > 0 && cleanEconLines[cleanEconLines.Count - 1].Trim() == "}")
            {
                cleanEconLines.RemoveAt(cleanEconLines.Count - 1);
            }

            // 4. Generate the new lines
            var newEconBlockBuilder = new System.Text.StringBuilder();
            foreach (var line in cleanEconLines)
            {
                newEconBlockBuilder.Append(line).Append(nl);
            }

            newEconBlockBuilder.Append(" visited_cities: ").Append(cities.Count).Append(nl);
            for (int i = 0; i < cities.Count; i++)
            {
                newEconBlockBuilder.Append(" visited_cities[").Append(i).Append("]: ").Append(cities[i]).Append(nl);
            }

            newEconBlockBuilder.Append(" visited_cities_count: ").Append(cities.Count).Append(nl);
            for (int i = 0; i < cities.Count; i++)
            {
                newEconBlockBuilder.Append(" visited_cities_count[").Append(i).Append("]: 1").Append(nl);
            }

            newEconBlockBuilder.Append("}");

            // 5. Replace in content
            string prefix = content.Substring(0, econOpenBrace);
            string suffix = content.Substring(econEnd + 1);

            return prefix + newEconBlockBuilder.ToString() + suffix;
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

            // Ask user for custom driving range
            int? kms = ShowInputDialog(this, "Enter desired fuel range in kilometers (max 50,000):", 50000);
            if (kms == null) return; // Cancelled

            // 50,000 km corresponds to roughly fuel_relative 12.5 on standard 1400L tanks.
            double fuelValue = kms.Value / 4000.0;

            _currentSaveContent = System.Text.RegularExpressions.Regex.Replace(
                _currentSaveContent,
                @"(?m)^(\s*fuel_relative:\s*)[^\r\n]*",
                $"$1 {fuelValue.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}");
            
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            MessageBox.Show($"Extended fuel (~{kms.Value:N0} km of driving range) applied to all your trucks!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static int? ShowInputDialog(Window owner, string prompt, int maxVal)
        {
            Window dialog = new Window
            {
                Title = "Extended Fuel Range",
                Width = 380,
                Height = 190,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                ResizeMode = ResizeMode.NoResize,
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0f172a")),
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1e293b")),
                BorderThickness = new Thickness(1.5)
            };

            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = prompt,
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#cbd5e1")),
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            };
            System.Windows.Controls.Grid.SetRow(textBlock, 0);
            grid.Children.Add(textBlock);

            var textBox = new System.Windows.Controls.TextBox
            {
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0c1220")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#334155")),
                Padding = new Thickness(8),
                FontSize = 15,
                BorderThickness = new Thickness(1),
                Text = "50000",
                VerticalAlignment = VerticalAlignment.Center
            };
            System.Windows.Controls.Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

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
                Content = "Cancel",
                Width = 85,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#334155")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            cancelButton.Click += (s, e) => dialog.Close();

            var applyButton = new System.Windows.Controls.Button
            {
                Content = "Apply",
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
                    MessageBox.Show($"Please enter a valid number between 1 and {maxVal:N0}.", "Invalid Range", MessageBoxButton.OK, MessageBoxImage.Warning);
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
