using System;
using System.Windows;
using System.Windows.Controls;

namespace TruckStudio
{
    public partial class MainWindow : Window
    {
        private void CbSettingsLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void CbSettingsTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            BtnCheckUpdates.IsEnabled = false;
            
            try
            {
                using (var webClient = new System.Net.WebClient())
                {
                    webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                    
                    string json = await webClient.DownloadStringTaskAsync(new Uri("https://truckstudio.online/version.json"));
                    
                    string latestVersionStr = ExtractJsonValue(json, "version");
                    string downloadUrl = ExtractJsonValue(json, "url");
                    
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

                    Version currentVersion = new Version("0.2.2");
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
    }
}
