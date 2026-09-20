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
            PopulatePlateColorCombos();
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
            SetActiveNav(BtnNavHome);
            LoadProfilesForSelectedGame();
            CheckForUpdatesSilently();
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
            SetActiveNav(BtnNavHome);
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
            SetActiveNav(BtnNavFleet);
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
            SetActiveNav(BtnNavMap);
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
            SetActiveNav(BtnNavPower);
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
            SetActiveNav(BtnNavJobs);
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
            SetActiveNav(BtnNavSettings);
            TranslateUI();
        }

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void SetActiveNav(System.Windows.Controls.Button activeButton)
        {
            var normalFg = (System.Windows.Media.Brush)FindResource("SubTextBrush");
            var activeBg = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3b82f6"));

            var navButtons = new[] { BtnNavHome, BtnNavFleet, BtnNavMap, BtnNavPower, BtnNavJobs, BtnNavSettings };
            foreach (var b in navButtons)
            {
                b.Background = System.Windows.Media.Brushes.Transparent;
                b.Foreground = normalFg;
            }
            activeButton.Background = activeBg;
            activeButton.Foreground = System.Windows.Media.Brushes.White;
        }

        private void SetLockedHints(bool isLoaded)
        {
            Visibility v = isLoaded ? Visibility.Collapsed : Visibility.Visible;
            TxtLockedHintFleet.Visibility = v;
            TxtLockedHintWorld.Visibility = v;
            TxtLockedHintTuning.Visibility = v;
            TxtLockedHintFreight.Visibility = v;
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
            TxtNavHome.Text = isEs ? "Inicio" : "Home";
            TxtNavFleet.Text = isEs ? "Arreglos Rápidos" : "Quick Fixes";
            TxtNavMap.Text = isEs ? "Mapa y Garajes" : "Map & Garages";
            TxtNavPower.Text = isEs ? "Mejoras" : "Power-Ups";
            TxtNavJobs.Text = isEs ? "Trabajos a Medida" : "Custom Jobs";
            TxtNavSettings.Text = isEs ? "Ajustes" : "Settings";
            BtnNavExit.ToolTip = isEs ? "Salir" : "Exit";

            // Page Titles
            if (PageProfile.Visibility == Visibility.Visible) PageTitle.Text = isEs ? "Inicio" : "Home";
            else if (PageTrucks.Visibility == Visibility.Visible) PageTitle.Text = isEs ? "Arreglos Rápidos" : "Quick Fixes";
            else if (PageWorld.Visibility == Visibility.Visible) PageTitle.Text = isEs ? "Mapa y Garajes" : "Map & Garages";
            else if (PageTuning.Visibility == Visibility.Visible) PageTitle.Text = isEs ? "Mejoras" : "Power-Ups";
            else if (PageFreight.Visibility == Visibility.Visible) PageTitle.Text = isEs ? "Trabajos a Medida" : "Custom Jobs";
            else if (PageSettings.Visibility == Visibility.Visible) PageTitle.Text = isEs ? "Ajustes" : "Settings";

            // Page 1: Home
            TxtHowItWorksHeader.Text = isEs ? "Cómo funciona" : "How it works";
            TxtStep1.Text = isEs ? "Elige tu juego" : "Pick your game";
            TxtStep2.Text = isEs ? "Elige tu perfil y partida" : "Choose your profile & save";
            TxtStep3.Text = isEs ? "Clic en Cargar y edita lo que quieras" : "Click Load and edit anything";
            TxtHowItWorksSub.Text = isEs
                ? "No necesitas saber nada técnico: los botones de abajo editan esa partida al instante."
                : "No tech knowledge needed - every button below edits that save instantly.";
            TxtSelectSaveGameHeader.Text = isEs ? "Seleccionar Partida" : "Select Save Game";
            TxtSaveHint.Text = isEs
                ? "Tus partidas se detectan automáticamente. Elige la que quieras modificar."
                : "Your save games are found automatically. Pick the one you want to change.";
            TxtGameLabel.Text = isEs ? "Juego" : "Game";
            RadioEts2.Content = "Euro Truck Simulator 2";
            RadioAts.Content = "American Truck Simulator";
            ProfileLabel.Text = isEs
                ? (_currentGame == GameType.ETS2 ? "Perfil de Euro Truck Simulator 2" : "Perfil de American Truck Simulator")
                : (_currentGame == GameType.ETS2 ? "Euro Truck Simulator 2 Profile" : "American Truck Simulator Profile");
            TxtSaveGameLabel.Text = isEs ? "Partida Guardada" : "Save Game";
            BtnLoadSelectedSave.Content = isEs ? "Cargar Partida" : "Load Save";
            BtnRefreshProfiles.Content = isEs ? "Actualizar" : "Refresh";
            BtnRefreshProfiles.ToolTip = isEs
                ? "Vuelve a buscar perfiles y partidas nuevas. Útil si el juego creó un perfil o una partida mientras está abierto."
                : "Scan again for new profiles and save games. Useful if the game created one while it is running.";

            TxtEconomyHeader.Text = isEs ? "Tu dinero y nivel" : "Your money & level";
            TxtEconomySub.Text = isEs
                ? "Pon tu saldo y experiencia en lo que quieras (solo números enteros)."
                : "Set your balance and experience to whatever you want (whole numbers only).";
            TxtMoneyLabel.Text = isEs ? "Dinero (€)" : "Money (€)";
            TxtXpLabel.Text = isEs ? "Experiencia (XP)" : "Experience (XP)";
            BtnSaveProfile.Content = isEs ? "Guardar Cambios" : "Save Changes";

            // Page 2: Quick Fixes
            string lockedHint = isEs
                ? "Consejo: primero carga una partida (Inicio) para desbloquear estas herramientas."
                : "Tip: load a save first (Home) to unlock these tools.";
            TxtLockedHintFleet.Text = lockedHint;
            TxtFixFleetTitle.Text = isEs ? "Repara todo de una vez" : "Fix everything at once";
            TxtFixFleetDesc.Text = isEs
                ? "¿Choque? Repara todos tus camiones y remolques al 100% al instante."
                : "Accident? Repair all your trucks and trailers to 100% condition instantly.";
            BtnFixFleet.Content = isEs ? "Reparar Todo" : "Repair Everything";
            TxtRefuelTitle.Text = isEs ? "Llena los tanques" : "Fill your tanks";
            TxtRefuelDesc.Text = isEs
                ? "Rellena todos los camiones al 100%: se acabó quedarse sin combustible."
                : "Refuel every truck to 100% - no more running on empty.";
            BtnRefuel.Content = isEs ? "Rellenar Combustible (100%)" : "Refill Fuel (100%)";
            TxtFixCargoTitle.Text = isEs ? "Salva tu entrega" : "Save your delivery";
            TxtFixCargoDesc.Text = isEs
                ? "¿Daño en la carga que llevas? Vuélvela al 0% y cobra el pago completo."
                : "Damage on the cargo you're hauling? Reset it to 0% and keep the full payment.";
            BtnFixCargo.Content = isEs ? "Reparar Carga (0%)" : "Fix Cargo Damage (0%)";
            TxtCargoWeightTitle.Text = isEs ? "Ajusta tu carga" : "Adjust your load";
            TxtCargoWeightLabel.Text = isEs ? "Peso de la Carga (Toneladas)" : "Cargo Weight (Tons)";
            TxtCargoWeightDesc.Text = isEs
                ? "Cambia el peso que llevas. 0 = liviana como una pluma, más = todo un desafío."
                : "Set the weight you're hauling. 0 = light as a feather, more = a real challenge.";
            BtnSaveCargoWeight.Content = isEs ? "Actualizar Peso" : "Update Weight";
            TxtDeliveryTimeTitle.Text = isEs ? "Compra más tiempo" : "Buy more time";
            TxtDeliveryTimeLabel.Text = isEs ? "Tiempo Restante para la Entrega (Horas)" : "Remaining Delivery Time (Hours)";
            TxtDeliveryTimeDesc.Text = isEs
                ? "¿A punto de llegar tarde? Añade más horas a la entrega activa (solo números positivos)."
                : "About to be late? Add more hours to the active delivery (positive numbers only).";
            BtnSaveDeliveryTime.Content = isEs ? "Actualizar Tiempo" : "Update Time";
            TxtTeleportTitle.Text = isEs ? "Teletranspórtate a un punto guardado" : "Teleport to a saved spot";
            TxtTeleportReqHeader.Text = isEs ? "Requisito: ¡Cámara 0 no activada!" : "Requirement: Camera 0 not enabled!";
            TxtTeleportReqDesc.Text = isEs
                ? "Necesitas activar dos opciones de desarrollador (g_console y g_developer = 1) en el archivo config.cfg de tu carpeta de Documentos."
                : "You need to enable two developer options (g_console and g_developer = 1) in the config.cfg file in your Documents folder.";
            TxtTeleportInstructions.Text = isEs
                ? "En el juego, guarda y presiona Alt + F12 para marcar el punto, vuelve y presiona Teletransportar."
                : "In game, save and press Alt + F12 to mark the spot, then come back and press Teleport.";
            BtnTeleport.Content = isEs ? "Teletransportar" : "Teleport";
            TxtLicensePlateTitle.Text = isEs ? "Diseña tu matrícula" : "Design your license plate";
            TxtLicensePlateDesc.Text = isEs
                ? "Escribe lo que quieras en la matrícula de tu camión y elige los colores. También puedes estamparla en el remolque acoplado."
                : "Put whatever you want on your truck's plate and pick the colors. You can also stamp the same plate on the attached trailer.";
            TxtPlateTextLabel.Text = isEs ? "Texto" : "Plate Text";
            TxtPlateBgColorLabel.Text = isEs ? "Fondo" : "Background";
            TxtPlateTextColorLabel.Text = isEs ? "Color del Texto" : "Text Color";
            TxtPlatePreviewLabel.Text = isEs ? "Vista Previa" : "Preview";
            ChkPlateColoredMargin.Content = isEs ? "Borde del color del texto" : "Colored border (uses text color)";
            ChkPlateApplyTrailer.Content = isEs ? "Aplicar también al remolque acoplado" : "Apply to attached trailer too";
            BtnSaveLicensePlate.Content = isEs ? "Aplicar Matrícula" : "Apply Plate";

            // Page 3: Map & Garages
            TxtLockedHintWorld.Text = lockedHint;
            TxtVisitGaragesTitle.Text = isEs ? "Abre todo el mapa" : "Open the whole map";
            TxtVisitGaragesDesc.Text = isEs
                ? "Desbloquea todas las ciudades sin pagar nada. Luego podrás comprar un garaje en cualquier lugar."
                : "Unlock every city without paying anything. You can buy a garage anywhere later.";
            BtnVisitGarages.Content = isEs ? "Desbloquear Todas las Ciudades" : "Unlock All Cities";
            TxtUpgradeGaragesTitle.Text = isEs ? "Haz más grandes tus garajes" : "Make your garages bigger";
            TxtUpgradeGaragesDesc.Text = isEs
                ? "Convierte todos los garajes que ya tienes en el tamaño más grande (6 espacios)."
                : "Turn every garage you already own into the biggest size (6 parking slots).";
            BtnUpgradeGarages.Content = isEs ? "Mejorar Todos los Garajes" : "Upgrade All Owned Garages";
            TxtBuyGaragesTitle.Text = isEs ? "Dueño de todos los garajes" : "Own every garage";
            TxtBuyGaragesDesc.Text = isEs
                ? "Compra y mejora todos los garajes del mapa. Cuesta dinero del juego, ¡prepárate!"
                : "Buy and fully upgrade every garage on the whole map. It costs real in-game money - be ready!";
            BtnBuyGarages.Content = isEs ? "Comprar Todos los Garajes" : "Buy All Garages";

            // Page 4: Power-Ups
            TxtLockedHintTuning.Text = lockedHint;
            TxtMaxSkillsTitle.Text = isEs ? "Sé el mejor conductor" : "Be the best driver";
            TxtMaxSkillsDesc.Text = isEs
                ? "Desbloquea todas las habilidades del conductor (ADR, Larga Distancia, etc.) al instante."
                : "Unlock every driver skill (ADR, Long Distance, etc.) instantly.";
            BtnMaxSkills.Content = isEs ? "Maximizar Habilidades" : "Max Out All Skills";
            TxtInfiniteFuelTitle.Text = isEs ? "Casi nunca recargues" : "Almost never refuel";
            TxtInfiniteFuelDesc.Text = isEs
                ? "Aumenta muchísimo tu tanque. Tú eliges la cantidad con un cuadro sencillo."
                : "Massively extend your fuel tank. You pick the amount with a simple box.";
            BtnInfiniteFuel.Content = isEs ? "Combustible Extendido" : "Extended Fuel";
            TxtRestoreFuelTitle.Text = isEs ? "Vuelve al tanque normal" : "Back to normal fuel";
            TxtRestoreFuelDesc.Text = isEs
                ? "Devuelve tu tanque a su tamaño original."
                : "Put your fuel tank back to its original size.";
            BtnRestoreFuel.Content = isEs ? "Restaurar Combustible" : "Restore Fuel";

            // Page 5: Custom Jobs
            TxtLockedHintFreight.Text = lockedHint;
            TxtFreightHeader.Text = isEs ? "Crea tu propio trabajo" : "Create your own job";
            TxtFreightIntro.Text = isEs
                ? "Elige dónde empieza el trabajo, dónde termina y qué carga llevas. La app hace el resto por ti."
                : "Pick where the job starts, where it ends, and what cargo you carry. The app builds the rest for you.";
            TxtFreightNote.Text = isEs
                ? "Nota: crea un trabajo 'de empresa' (el remolque te lo dan), como los principales de la lista de trabajos del juego."
                : "Note: It creates a company job (the trailer is provided), like the main ones in the game's job list.";
            TxtSourceCityLabel.Text = isEs ? "Ciudad de Origen" : "Start City";
            TxtSourceCompanyLabel.Text = isEs ? "Empresa de Origen" : "Start Company";
            TxtDestCityLabel.Text = isEs ? "Ciudad de Destino" : "Destination City";
            TxtDestCompanyLabel.Text = isEs ? "Empresa de Destino" : "Destination Company";
            TxtCargoLabel.Text = isEs ? "Carga / Mercancía" : "Cargo";
            TxtUrgencyLabel.Text = isEs ? "Urgencia" : "Urgency";
            BtnInjectJob.Content = isEs ? "Crear Trabajo a Medida" : "Create Custom Job";

            // Page 6: Settings
            TxtSettingsTitle.Text = isEs ? "Configuración de la Aplicación" : "Application Settings";
            TxtSettingsLanguage.Text = isEs ? "Idioma" : "Language";
            TxtSettingsTheme.Text = isEs ? "Tema Visual" : "Theme";
            ComboThemeDark.Content = isEs ? "Tema Oscuro" : "Dark Theme";
            ComboThemeLight.Content = isEs ? "Tema Claro" : "Light Theme";
            BtnCheckUpdates.Content = isEs ? "Buscar Actualizaciones" : "Check for Updates";
            TxtUpdateBadge.Text = isEs ? "Actualización" : "Update";
            BtnUpdateBadge.ToolTip = isEs
                ? "¡Hay una nueva versión disponible! Haz clic para actualizar."
                : "A new version is available! Click to update.";
        }

        private MessageBoxResult ShowLocalizedMessageBox(string enMessage, string esMessage, string enTitle, string esTitle, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.Information)
        {
            string message = _currentLanguage == "es" ? esMessage : enMessage;
            string title = _currentLanguage == "es" ? esTitle : enTitle;
            return MessageBox.Show(message, title, buttons, image);
        }

        private static bool IsGameRunning()
        {
            string[] processNames = { "eurotrucks2", "ets2", "amtrucks", "truckersmp", "bin_x64" };
            foreach (string name in processNames)
            {
                try
                {
                    if (System.Diagnostics.Process.GetProcessesByName(name).Length > 0)
                        return true;
                }
                catch { }
            }
            return false;
        }

        private bool WarnIfGameRunning()
        {
            if (!IsGameRunning()) return false;
            MessageBoxResult result = ShowLocalizedMessageBox(
                "WARNING: Euro Truck Simulator 2 (or TruckersMP) is currently running.\n\n" +
                "The game auto-saves constantly and will OVERWRITE your edits with its old state.\n" +
                "Your changes will be lost unless you close the game completely first.\n\n" +
                "Do you want to continue anyway?",
                "¡ADVERTENCIA! Euro Truck Simulator 2 (o TruckersMP) está en ejecución.\n\n" +
                "El juego autoguarda constantemente y SOBRESCRIBIRÁ tus cambios con su estado anterior.\n" +
                "Perderás las modificaciones a menos que cierres el juego por completo primero.\n\n" +
                "¿Deseas continuar de todas formas?",
                "Game is running", "El juego está en ejecución",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            return result != MessageBoxResult.Yes;
        }
    }
}
