using System;
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

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
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
    }
}
