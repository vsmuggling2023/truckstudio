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

        private const string CurrentAppVersion = "0.3.1";

        private UpdateInfo _latestUpdateInfo;

        private class UpdateInfo
        {
            public string LatestVersion;
            public string DownloadUrl;
            public string ChangelogEn;
            public string ChangelogEs;
        }

        private static async System.Threading.Tasks.Task<UpdateInfo> FetchUpdateInfoAsync()
        {
            using (var webClient = new System.Net.WebClient())
            {
                webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                webClient.Encoding = System.Text.Encoding.UTF8;
                string json = await webClient.DownloadStringTaskAsync(new Uri("https://truckstudio.online/version.json"));

                string changelog = ExtractJsonValue(json, "changelog");
                var info = new UpdateInfo
                {
                    LatestVersion = ExtractJsonValue(json, "version"),
                    DownloadUrl = ExtractJsonValue(json, "url"),
                    ChangelogEn = FirstNonEmpty(ExtractJsonValue(json, "changelog_en"), changelog),
                    ChangelogEs = FirstNonEmpty(ExtractJsonValue(json, "changelog_es"), changelog)
                };
                if (info.ChangelogEn != null) info.ChangelogEn = info.ChangelogEn.Replace("\\n", "\n");
                if (info.ChangelogEs != null) info.ChangelogEs = info.ChangelogEs.Replace("\\n", "\n");
                return info;
            }
        }

        private static string FirstNonEmpty(string a, string b)
        {
            return string.IsNullOrEmpty(a) ? b : a;
        }

        /// <summary>
        /// Silent startup check: shows the navbar alert pill when a newer version exists.
        /// Never bothers the user (offline, server down, parse issues are all ignored).
        /// </summary>
        private async void CheckForUpdatesSilently()
        {
            try
            {
                UpdateInfo info = await FetchUpdateInfoAsync();
                if (info == null || string.IsNullOrEmpty(info.LatestVersion) || string.IsNullOrEmpty(info.DownloadUrl)) return;

                if (Version.TryParse(info.LatestVersion, out Version latest) && latest > new Version(CurrentAppVersion))
                {
                    _latestUpdateInfo = info;
                    BtnUpdateBadge.Visibility = Visibility.Visible;
                    TxtUpdateBadge.Text = _currentLanguage == "es" ? "Actualización" : "Update";
                }
            }
            catch { }
        }

        private void UpdateBadge_Click(object sender, RoutedEventArgs e)
        {
            NavSettings_Click(sender, e);
            CheckUpdates_Click(sender, e);
        }

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            BtnCheckUpdates.IsEnabled = false;

            try
            {
                UpdateInfo info = await FetchUpdateInfoAsync();

                string latestVersionStr = info.LatestVersion;
                string downloadUrl = info.DownloadUrl;
                string changelog = _currentLanguage == "es" ? info.ChangelogEs : info.ChangelogEn;
                if (string.IsNullOrEmpty(changelog))
                {
                    changelog = _currentLanguage == "es" ? "Mejoras de estabilidad y rendimiento." : "Stability and performance improvements.";
                }

                if (string.IsNullOrEmpty(latestVersionStr) || string.IsNullOrEmpty(downloadUrl))
                {
                    ShowLocalizedMessageBox("Failed to parse update info from server.", "No se pudo interpretar la información de actualización del servidor.", "Error", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    BtnCheckUpdates.IsEnabled = true;
                    return;
                }

                if (Version.TryParse(latestVersionStr, out Version latestVersion) && latestVersion > new Version(CurrentAppVersion))
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
