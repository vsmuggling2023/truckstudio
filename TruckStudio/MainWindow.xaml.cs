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
        private GameType _currentGame = GameType.ETS2;
        private string _currentLanguage = "en";
        private bool _isDarkTheme = true;
        private bool _isLoadingSettings = false;

        public MainWindow()
        {
            InitializeComponent();
            ExtractUpdater();
            LoadSettings();
            ApplyTheme(_isDarkTheme);
            Loaded += MainWindow_Loaded;
        }

        private void ExtractUpdater()
        {
            try
            {
                string appDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                string updaterPath = System.IO.Path.Combine(appDir, "TruckStudioUpdater.exe");

                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = "TruckStudio.Resources.TruckStudioUpdater.exe";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        bool shouldWrite = true;
                        if (System.IO.File.Exists(updaterPath))
                        {
                            try
                            {
                                var fileInfo = new System.IO.FileInfo(updaterPath);
                                if (fileInfo.Length == stream.Length)
                                {
                                    shouldWrite = false;
                                }
                            }
                            catch { }
                        }

                        if (shouldWrite)
                        {
                            if (System.IO.File.Exists(updaterPath))
                            {
                                try
                                {
                                    System.IO.File.Delete(updaterPath);
                                }
                                catch
                                {
                                    string backup = updaterPath + ".bak";
                                    if (System.IO.File.Exists(backup)) System.IO.File.Delete(backup);
                                    System.IO.File.Move(updaterPath, backup);
                                }
                            }

                            using (var fileStream = new System.IO.FileStream(updaterPath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                            {
                                stream.CopyTo(fileStream);
                            }
                        }
                    }
                }
            }
            catch {}
        }

        private void LoadSettings()
        {
            try
            {
                string folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TruckStudio");
                string settingsFile = System.IO.Path.Combine(folder, "settings.cfg");
                if (System.IO.File.Exists(settingsFile))
                {
                    var lines = System.IO.File.ReadAllLines(settingsFile);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();
                            if (key == "Language")
                            {
                                _currentLanguage = value;
                            }
                            else if (key == "Theme")
                            {
                                _isDarkTheme = (value == "dark");
                            }
                        }
                    }
                }
            }
            catch {}
        }

        private void SaveSettings()
        {
            try
            {
                string folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TruckStudio");
                if (!System.IO.Directory.Exists(folder))
                {
                    System.IO.Directory.CreateDirectory(folder);
                }
                string settingsFile = System.IO.Path.Combine(folder, "settings.cfg");
                var lines = new string[]
                {
                    $"Language={_currentLanguage}",
                    $"Theme={(_isDarkTheme ? "dark" : "light")}"
                };
                System.IO.File.WriteAllLines(settingsFile, lines);
            }
            catch {}
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoadingSettings = true;
            if (_currentLanguage == "es")
            {
                CbSettingsLanguage.SelectedItem = ComboLangEs;
            }
            else
            {
                CbSettingsLanguage.SelectedItem = ComboLangEn;
            }

            if (_isDarkTheme)
            {
                CbSettingsTheme.SelectedItem = ComboThemeDark;
            }
            else
            {
                CbSettingsTheme.SelectedItem = ComboThemeLight;
            }
            _isLoadingSettings = false;

            ApplyTheme(_isDarkTheme);
            LoadProfilesForSelectedGame();
        }

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
                    // Debug: export decrypted game.sii to desktop (disabled in v0.2.1-alpha)
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
                    ShowLocalizedMessageBox("Could not find coordinates in cams.txt or bugs.txt. Make sure you use Camera 0 to save your coordinates.", "No se encontraron coordenadas en cams.txt o bugs.txt. Asegúrate de usar la Cámara 0 para guardar tus coordenadas.", "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            _currentSaveContent = SaveParser.Teleport(_currentSaveContent, x, y, z, rotation);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox($"Truck teleported to coordinates: X:{x}, Y:{y}, Z:{z}!", $"¡Camión teletransportado a las coordenadas X:{x}, Y:{y}, Z:{z}!", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FixCargo_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            _currentSaveContent = SaveParser.FixCargoDamage(_currentSaveContent);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox("Current cargo damage has been reset to 0% in the save file!", "¡El daño de la carga activa ha sido restablecido al 0%!", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
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

            ShowLocalizedMessageBox("All garages have been unlocked as Small (1-slot) garages!\nEvery city is now accessible. Use 'Upgrade All Owned' to max them out.", "¡Todos los garajes han sido descubiertos como Pequeños (1 espacio)!\nTodas las ciudades son accesibles. Usa 'Mejorar Garajes Propios' para expandirlos.", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpgradeGarages_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            // Only upgrades garages you already own (status 2-5) to max (6).
            // Garages at 0 (undiscovered) are left untouched.
            _currentSaveContent = UpdateGarageStatus(_currentSaveContent, GarageMode.Upgrade);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox("All owned garages have been upgraded to maximum size (6 slots)!", "¡Todos tus garajes comprados han sido mejorados al tamaño máximo (6 espacios)!", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BuyAllGarages_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            // Set every garage to status 6 = fully purchased + max upgraded.
            _currentSaveContent = UpdateGarageStatus(_currentSaveContent, GarageMode.BuyAll);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox("All garages have been purchased and upgraded to maximum size!\nYou now own every garage across the entire map.", "¡Todos los garajes del mapa han sido comprados y mejorados al tamaño máximo!\nAhora eres dueño de todos los garajes del mapa.", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
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

            ShowLocalizedMessageBox("All player skills (ADR, Long Distance, etc) have been maxed out!", "¡Todas las habilidades del conductor (ADR, Larga Distancia, etc.) han sido maximizadas!", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InfiniteFuel_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;

            // Ask user for custom driving range
            string prompt = _currentLanguage == "es" ? "Ingresa el rango de combustible deseado en kilómetros (máx 50,000):" : "Enter desired fuel range in kilometers (max 50,000):";
            int? kms = ShowInputDialog(this, prompt, 50000);
            if (kms == null) return; // Cancelled

            // 50,000 km corresponds to roughly fuel_relative 12.5 on standard 1400L tanks.
            double fuelValue = kms.Value / 4000.0;

            _currentSaveContent = System.Text.RegularExpressions.Regex.Replace(
                _currentSaveContent,
                @"(?m)^(\s*fuel_relative:\s*)[^\r\n]*",
                $"$1 {fuelValue.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}");
            
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox($"Extended fuel (~{kms.Value:N0} km of driving range) applied to all your trucks!", $"¡Combustible extendido (~{kms.Value:N0} km de autonomía) aplicado a todos tus camiones!", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
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
                Height = 190,
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
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            };
            System.Windows.Controls.Grid.SetRow(textBlock, 0);
            grid.Children.Add(textBlock);

            var textBox = new System.Windows.Controls.TextBox
            {
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(inputColor)),
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(inputText)),
                BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(borderColor)),
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

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void NavProfile_Click(object sender, RoutedEventArgs e)
        {
            PageProfile.Visibility = Visibility.Visible;
            PageTrucks.Visibility = Visibility.Collapsed;
            PageWorld.Visibility = Visibility.Collapsed;
            PageTuning.Visibility = Visibility.Collapsed;
            PageFreight.Visibility = Visibility.Collapsed;
            PageSettings.Visibility = Visibility.Collapsed;
            TranslateUI();
        }

        private void NavTrucks_Click(object sender, RoutedEventArgs e)
        {
            PageProfile.Visibility = Visibility.Collapsed;
            PageTrucks.Visibility = Visibility.Visible;
            PageWorld.Visibility = Visibility.Collapsed;
            PageTuning.Visibility = Visibility.Collapsed;
            PageFreight.Visibility = Visibility.Collapsed;
            PageSettings.Visibility = Visibility.Collapsed;
            TranslateUI();
        }

        private void NavWorld_Click(object sender, RoutedEventArgs e)
        {
            PageProfile.Visibility = Visibility.Collapsed;
            PageTrucks.Visibility = Visibility.Collapsed;
            PageWorld.Visibility = Visibility.Visible;
            PageTuning.Visibility = Visibility.Collapsed;
            PageFreight.Visibility = Visibility.Collapsed;
            PageSettings.Visibility = Visibility.Collapsed;
            TranslateUI();
        }

        private void NavTuning_Click(object sender, RoutedEventArgs e)
        {
            PageProfile.Visibility = Visibility.Collapsed;
            PageTrucks.Visibility = Visibility.Collapsed;
            PageWorld.Visibility = Visibility.Collapsed;
            PageTuning.Visibility = Visibility.Visible;
            PageFreight.Visibility = Visibility.Collapsed;
            PageSettings.Visibility = Visibility.Collapsed;
            TranslateUI();
        }

        private void NavFreight_Click(object sender, RoutedEventArgs e)
        {
            PageProfile.Visibility = Visibility.Collapsed;
            PageTrucks.Visibility = Visibility.Collapsed;
            PageWorld.Visibility = Visibility.Collapsed;
            PageTuning.Visibility = Visibility.Collapsed;
            PageFreight.Visibility = Visibility.Visible;
            PageSettings.Visibility = Visibility.Collapsed;
            TranslateUI();
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            PageProfile.Visibility = Visibility.Collapsed;
            PageTrucks.Visibility = Visibility.Collapsed;
            PageWorld.Visibility = Visibility.Collapsed;
            PageTuning.Visibility = Visibility.Collapsed;
            PageFreight.Visibility = Visibility.Collapsed;
            PageSettings.Visibility = Visibility.Visible;
            TranslateUI();
        }

        private void CbSettingsLanguage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            if (CbSettingsLanguage.SelectedItem == ComboLangEn)
            {
                _currentLanguage = "en";
            }
            else if (CbSettingsLanguage.SelectedItem == ComboLangEs)
            {
                _currentLanguage = "es";
            }

            TranslateUI();

            if (!_isLoadingSettings)
            {
                SaveSettings();
            }
        }

        private void CbSettingsTheme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            if (CbSettingsTheme.SelectedItem == ComboThemeDark)
            {
                ApplyTheme(true);
            }
            else if (CbSettingsTheme.SelectedItem == ComboThemeLight)
            {
                ApplyTheme(false);
            }

            if (!_isLoadingSettings)
            {
                SaveSettings();
            }
        }

        private void ApplyTheme(bool isDark)
        {
            _isDarkTheme = isDark;
            
            var bg = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(isDark ? "#0f172a" : "#f8fafc");
            var cardBg = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(isDark ? "#1e293b" : "#ffffff");
            var text = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(isDark ? "#f8fafc" : "#0f172a");
            var subText = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(isDark ? "#94a3b8" : "#475569");
            var border = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(isDark ? "#334155" : "#cbd5e1");
            var inputBg = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(isDark ? "#0f172a" : "#f1f5f9");
            var inputText = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(isDark ? "#ffffff" : "#0f172a");
            var sidebarHover = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(isDark ? "#1e293b" : "#f1f5f9");

            this.Resources["BgBrush"] = new System.Windows.Media.SolidColorBrush(bg);
            this.Resources["CardBgBrush"] = new System.Windows.Media.SolidColorBrush(cardBg);
            this.Resources["TextBrush"] = new System.Windows.Media.SolidColorBrush(text);
            this.Resources["SubTextBrush"] = new System.Windows.Media.SolidColorBrush(subText);
            this.Resources["BorderBrush"] = new System.Windows.Media.SolidColorBrush(border);
            this.Resources["InputBgBrush"] = new System.Windows.Media.SolidColorBrush(inputBg);
            this.Resources["InputTextBrush"] = new System.Windows.Media.SolidColorBrush(inputText);
            this.Resources["SidebarHoverBrush"] = new System.Windows.Media.SolidColorBrush(sidebarHover);
        }

        private void TranslateUI()
        {
            bool isEs = (_currentLanguage == "es");

            // Sidebar
            TxtLogoTitle.Text = "TruckStudio";
            TxtNavProfile.Text = isEs ? "Perfil de Usuario" : "Player Profile";
            TxtNavTrucks.Text = isEs ? "Camiones y Remolques" : "Trucks & Trailers";
            TxtNavWorld.Text = isEs ? "Mundo y Mapa" : "World & Map";
            TxtNavTuning.Text = isEs ? "Tuning Pro" : "Pro Tuning";
            TxtNavFreight.Text = isEs ? "Mercado de Fletes" : "Freight Market";
            TxtNavSettings.Text = isEs ? "Ajustes" : "Settings";
            TxtNavExit.Text = isEs ? "Salir" : "Exit";

            // Page Titles
            if (PageProfile.Visibility == Visibility.Visible) PageTitle.Text = isEs ? "Perfil de Usuario" : "Player Profile";
            else if (PageTrucks.Visibility == Visibility.Visible) PageTitle.Text = isEs ? "Camiones y Remolques" : "Trucks & Trailers";
            else if (PageWorld.Visibility == Visibility.Visible) PageTitle.Text = isEs ? "Mundo y Mapa" : "World & Map";
            else if (PageTuning.Visibility == Visibility.Visible) PageTitle.Text = isEs ? "Tuning Pro" : "Pro Tuning";
            else if (PageFreight.Visibility == Visibility.Visible) PageTitle.Text = isEs ? "Mercado de Fletes" : "Freight Market";
            else if (PageSettings.Visibility == Visibility.Visible) PageTitle.Text = isEs ? "Ajustes" : "Settings";

            // Page 1: Profile
            TxtSelectSaveGameHeader.Text = isEs ? "Seleccionar Partida" : "Select Save Game";
            TxtGameLabel.Text = isEs ? "Juego" : "Game";
            RadioEts2.Content = "Euro Truck Simulator 2";
            RadioAts.Content = "American Truck Simulator";
            ProfileLabel.Text = isEs 
                ? (_currentGame == GameType.ETS2 ? "Perfil de Euro Truck Simulator 2" : "Perfil de American Truck Simulator")
                : (_currentGame == GameType.ETS2 ? "Euro Truck Simulator 2 Profile" : "American Truck Simulator Profile");
            TxtSaveGameLabel.Text = isEs ? "Partida Guardada" : "Save Game";
            BtnLoadSelectedSave.Content = isEs ? "Cargar Partida Seleccionada" : "Load Selected Save";
            
            TxtEconomyHeader.Text = isEs ? "Economía y Progreso" : "Player Economy & Progress";
            TxtMoneyLabel.Text = isEs ? "Dinero" : "Money (€)";
            TxtXpLabel.Text = isEs ? "Experiencia (XP)" : "Experience (XP)";
            BtnSaveProfile.Content = isEs ? "Guardar Cambios de Perfil" : "Save Profile Changes";

            // Page 2: Trucks
            TxtFleetHeader.Text = isEs ? "Mantenimiento de Flota" : "Fleet Maintenance & Actions";
            TxtFixFleetDesc.Text = isEs ? "Repara al instante todos tus camiones y remolques al 100%." : "Instantly repair all your trucks and trailers to 100% condition.";
            BtnFixFleet.Content = isEs ? "Reparar Camiones y Remolques" : "Fix All Trucks & Trailers";
            TxtRefuelDesc.Text = isEs ? "Rellena el combustible de todos tus camiones al 100%." : "Refuel all your trucks to 100% (without repairing).";
            BtnRefuel.Content = isEs ? "Rellena Combustible (100%)" : "Refill Fuel (100%)";
            TxtFixCargoDesc.Text = isEs ? "Elimina el daño de tu carga activa al 0%." : "Fix the cargo damage of your active delivery back to 0%.";
            BtnFixCargo.Content = isEs ? "Reparar Carga (0%)" : "Fix Cargo Damage (0%)";
            TxtTeleportHeader.Text = isEs ? "Teletransporte (Cámara 0)" : "Teleport (Camera 0)";
            TxtTeleportReqHeader.Text = isEs ? "Requisito: ¡Cámara 0 no activada!" : "Requirement: Camera 0 not enabled!";
            TxtTeleportReqDesc.Text = isEs 
                ? "Debes activar g_console y g_developer (ponerlos en 1) en tu archivo config.cfg en la carpeta de Documentos."
                : "You must activate g_console and g_developer (set them to 1) in the config.cfg file located in your Documents folder.";
            TxtTeleportInstructions.Text = isEs 
                ? "Instrucciones: Guarda el juego y presiona Alt + F12 (como hace Truck Tools)."
                : "Instructions: Save the game and press Alt + F12, as Truck Tools does.";
            BtnTeleport.Content = isEs ? "Teletransportar" : "Teleport";

            // Page 3: World
            TxtWorldHeader.Text = isEs ? "Trucos de Mundo y Mapa" : "World & Map Exploits";
            TxtVisitGaragesDesc.Text = isEs 
                ? "Descubre todas las ciudades sin comprar garajes. Esto te permite comprar garajes online luego."
                : "Unlock every city without buying garages. Sets all undiscovered garages to the minimum (Small) so you can upgrade manually.";
            BtnVisitGarages.Content = isEs ? "Descubrir Todos los Garajes" : "Visit All Garages";
            TxtUpgradeGaragesDesc.Text = isEs 
                ? "Mejora todos los garajes que ya posees al tamaño máximo (6 espacios)."
                : "Upgrade every garage you already own to maximum size (6 slots). Does not buy garages you don't own yet.";
            BtnUpgradeGarages.Content = isEs ? "Mejorar Garajes Propios" : "Upgrade All Owned Garages";
            TxtBuyGaragesDesc.Text = isEs 
                ? "Compra y mejora al máximo absolutamente todos los garajes a lo largo del mapa entero."
                : "Purchase and fully upgrade every garage to maximum size (6 slots) across the entire map.";
            BtnBuyGarages.Content = isEs ? "Comprar y Mejorar Todo" : "Buy All Garages";

            // Page 4: Tuning
            TxtTuningHeader.Text = isEs ? "Tuning Pro y Trampas" : "Pro Tuning & Cheats";
            TxtTuningDesc.Text = isEs 
                ? "Habilita combustible extendido o maximiza todos tus niveles de habilidad del conductor."
                : "Enable infinite fuel or completely max out all driver skills (ADR, Long Distance, etc).";
            BtnMaxSkills.Content = isEs ? "Maximizar Habilidades" : "Max Out All Skills";
            BtnInfiniteFuel.Content = isEs ? "Combustible Extendido (Establecer km)" : "Extended Fuel (Set km)";
            BtnRestoreFuel.Content = isEs ? "Restaurar Combustible" : "Restore Fuel";

            // Page 5: Freight Market
            TxtFreightHeader.Text = isEs ? "Generador de Cargas" : "Custom Job Generator";
            TxtSourceCityLabel.Text = isEs ? "Ciudad de Origen" : "Source City";
            TxtSourceCompanyLabel.Text = isEs ? "Empresa de Origen" : "Source Company";
            TxtDestCityLabel.Text = isEs ? "Ciudad de Destino" : "Destination City";
            TxtDestCompanyLabel.Text = isEs ? "Empresa de Destino" : "Destination Company";
            TxtCargoLabel.Text = isEs ? "Carga / Mercancía" : "Cargo";
            TxtUrgencyLabel.Text = isEs ? "Urgencia" : "Urgency";
            BtnInjectJob.Content = isEs ? "Inyectar Trabajo Personalizado" : "Inject Custom Job";

            // Page 6: Settings
            TxtSettingsTitle.Text = isEs ? "Configuración de la Aplicación" : "Application Settings";
            TxtSettingsLanguage.Text = isEs ? "Idioma" : "Language";
            TxtSettingsTheme.Text = isEs ? "Tema Visual" : "Theme";
            ComboThemeDark.Content = isEs ? "Tema Oscuro" : "Dark Theme";
            ComboThemeLight.Content = isEs ? "Tema Claro" : "Light Theme";
            BtnCheckUpdates.Content = isEs ? "Buscar Actualizaciones" : "Check for Updates";
        }

        private MessageBoxResult ShowLocalizedMessageBox(string enMessage, string esMessage, string enTitle, string esTitle, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.Information)
        {
            string message = _currentLanguage == "es" ? esMessage : enMessage;
            string title = _currentLanguage == "es" ? esTitle : enTitle;
            return MessageBox.Show(message, title, buttons, image);
        }

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            BtnCheckUpdates.IsEnabled = false;
            
            try
            {
                using (var webClient = new System.Net.WebClient())
                {
                    webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                    
                    // Fetches JSON meta file from the hosted server
                    string json = await webClient.DownloadStringTaskAsync(new Uri("https://truckstudio.online/version.json"));
                    
                    string latestVersionStr = ExtractJsonValue(json, "version");
                    string downloadUrl = ExtractJsonValue(json, "url");
                    
                    // Support bilingual changelogs
                    string changelogKey = _currentLanguage == "es" ? "changelog_es" : "changelog_en";
                    string changelog = ExtractJsonValue(json, changelogKey);
                    if (string.IsNullOrEmpty(changelog))
                    {
                        changelog = ExtractJsonValue(json, "changelog");
                        if (string.IsNullOrEmpty(changelog))
                        {
                            changelog = _currentLanguage == "es" ? "Mejoras de estabilidad y rendimiento." : "Stability and performance improvements.";
                        }
                    }

                    if (string.IsNullOrEmpty(latestVersionStr) || string.IsNullOrEmpty(downloadUrl))
                    {
                        ShowLocalizedMessageBox("Failed to parse update info from server.", "No se pudo interpretar la información de actualización del servidor.", "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        BtnCheckUpdates.IsEnabled = true;
                        return;
                    }

                    Version currentVersion = new Version("0.2.1");
                    if (Version.TryParse(latestVersionStr, out Version latestVersion) && latestVersion > currentVersion)
                    {
                        var answer = ShowLocalizedMessageBox(
                            $"A new version (v{latestVersionStr}) is available!\nChangelog: {changelog}\n\nDo you want to download and install it now?",
                            $"¡Hay una nueva versión (v{latestVersionStr}) disponible!\nCambios: {changelog}\n\n¿Quieres descargarla e instalarla ahora?",
                            "Update Available", "Actualización Disponible", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (answer == MessageBoxResult.Yes)
                        {
                            string currentExePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                            string appDir = System.IO.Path.GetDirectoryName(currentExePath);
                            string updaterExe = System.IO.Path.Combine(appDir, "TruckStudioUpdater.exe");

                            if (!System.IO.File.Exists(updaterExe))
                            {
                                ShowLocalizedMessageBox("TruckStudioUpdater.exe not found! Please make sure it exists in the app folder.", "¡No se encontró TruckStudioUpdater.exe! Asegúrate de que esté en la carpeta de la aplicación.", "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                BtnCheckUpdates.IsEnabled = true;
                                return;
                            }

                            int curPid = System.Diagnostics.Process.GetCurrentProcess().Id;
                            string args = $"/url \"{downloadUrl}\" /target \"{currentExePath}\" /pid {curPid}";

                            System.Diagnostics.Process.Start(updaterExe, args);
                            Application.Current.Shutdown();
                            return;
                        }
                    }
                    else
                    {
                        ShowLocalizedMessageBox("You already have the latest version!", "¡Ya tienes la última versión instalada!", "Up to date", "Al día", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowLocalizedMessageBox(
                    $"Error checking for updates: {ex.Message}\n\nMake sure the version URL in MainWindow.xaml.cs is configured with your active host domain.", 
                    $"Error al buscar actualizaciones: {ex.Message}\n\nAsegúrate de que la URL en MainWindow.xaml.cs esté configurada con tu dominio de hosting activo.", 
                    "Update Error", "Error de Actualización", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            BtnCheckUpdates.IsEnabled = true;
        }

        private static string ExtractJsonValue(string json, string key)
        {
            try
            {
                string searchKey = $"\"{key}\"";
                int keyIdx = json.IndexOf(searchKey);
                if (keyIdx == -1) return null;

                int colonIdx = json.IndexOf(":", keyIdx);
                if (colonIdx == -1) return null;

                int startQuote = json.IndexOf("\"", colonIdx);
                if (startQuote == -1) return null;

                int endQuote = json.IndexOf("\"", startQuote + 1);
                if (endQuote == -1) return null;

                return json.Substring(startQuote + 1, endQuote - startQuote - 1);
            }
            catch
            {
                return null;
            }
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
                string gameName = _currentGame == GameType.ETS2 ? "Euro Truck Simulator 2" : "American Truck Simulator";
                SaveParser.Log("UI: Showing success MessageBox...");
                MessageBox.Show($"Custom Job successfully injected!\nRoute: {sourceCity} ({sourceCompany}) -> {destCity} ({destCompany})\nCargo: {cargo}\nUrgency: {urgencyText}\nDistance: {distance} km\n\nLoad this save in {gameName} and check {sourceCompany} in {sourceCity}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
