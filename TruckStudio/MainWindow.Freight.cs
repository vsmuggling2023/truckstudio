using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TruckStudio.Core;

namespace TruckStudio
{
    public partial class MainWindow : Window
    {
        private void CbSourceCity_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void CbDestCity_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

            string distance = "5000";
            SaveParser.Log("UI: Distance fixed=5000 km");

            try
            {
                SaveParser.Log("UI: Calling InjectFreightJob...");
                _currentSaveContent = SaveParser.InjectFreightJob(_currentSaveContent, sourceCity, sourceCompany, destCity, destCompany, cargo, urgency, distance);
                
                SaveParser.Log($"UI: Writing updated content to file: {_currentSavePath}...");
                System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);
                SaveParser.Log("UI: File successfully written!");

                string urgencyText = (CbUrgency.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Normal";
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
