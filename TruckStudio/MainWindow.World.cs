using System;
using System.Windows;
using TruckStudio.Core;

namespace TruckStudio
{
    public partial class MainWindow : Window
    {
        private void VisitGarages_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;
            if (WarnIfGameRunning()) return;

            int garageCount = System.Text.RegularExpressions.Regex.Matches(_currentSaveContent, @"(?m)^\s*garage : garage\.").Count;

            _currentSaveContent = UpdateGarageStatus(_currentSaveContent, GarageMode.Visit);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox($"All garages have been unlocked as Small (1-slot) garages!\nEvery city is now accessible ({garageCount} garages found). Use 'Upgrade All Owned' to max them out.", $"¡Todos los garajes han sido descubiertos como Pequeños (1 espacio)!\nTodas las ciudades son accesibles ({garageCount} garajes encontrados). Usa 'Mejorar Garajes Propios' para expandirlos.", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpgradeGarages_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;
            if (WarnIfGameRunning()) return;

            int garageCount = System.Text.RegularExpressions.Regex.Matches(_currentSaveContent, @"(?m)^\s*garage : garage\.").Count;

            _currentSaveContent = UpdateGarageStatus(_currentSaveContent, GarageMode.Upgrade);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox($"All owned garages have been upgraded to maximum size (6 slots)! ({garageCount} garages found)", $"¡Todos tus garajes comprados han sido mejorados al tamaño máximo (6 espacios)! ({garageCount} garajes encontrados)", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BuyAllGarages_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentSavePath) || string.IsNullOrEmpty(_currentSaveContent)) return;
            if (WarnIfGameRunning()) return;

            int garageCount = System.Text.RegularExpressions.Regex.Matches(_currentSaveContent, @"(?m)^\s*garage : garage\.").Count;

            _currentSaveContent = UpdateGarageStatus(_currentSaveContent, GarageMode.BuyAll);
            System.IO.File.WriteAllText(_currentSavePath, _currentSaveContent);

            ShowLocalizedMessageBox($"All garages have been purchased and upgraded to maximum size!\nYou now own every garage across the entire map ({garageCount} garages).", $"¡Todos los garajes del mapa han sido comprados y mejorados al tamaño máximo!\nAhora eres dueño de todos los garajes del mapa ({garageCount} garajes).", "Success", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private enum GarageMode { Visit, Upgrade, BuyAll }

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
                    if (t.StartsWith("garage") && t.Contains("garage.") && t.EndsWith("{"))
                        inGarage = true;
                    sb.Append(line).Append(nl);
                    continue;
                }

                if (t == "}")
                {
                    inGarage = false;
                    sb.Append(line).Append(nl);
                    continue;
                }

                if (t.StartsWith("status:"))
                {
                    int cur = 0;
                    int.TryParse(t.Substring(7).Trim(), out cur);

                    int next = cur;
                    switch (mode)
                    {
                        case GarageMode.Visit:
                            next = cur;
                            break;
                        case GarageMode.Upgrade:
                            if (cur == 1 || cur == 2 || cur == 4 || cur == 5 || cur == 6)
                                next = 3;
                            break;
                        case GarageMode.BuyAll:
                            next = 3;
                            break;
                    }

                    string indent = line.Substring(0, line.Length - line.TrimStart().Length);
                    sb.Append(indent).Append("status: ").Append(next).Append(nl);
                    continue;
                }

                sb.Append(line).Append(nl);
            }

            if (sb.Length >= nl.Length
                && sb.ToString(sb.Length - nl.Length, nl.Length) == nl
                && !content.EndsWith(nl))
                sb.Remove(sb.Length - nl.Length, nl.Length);

            string result = sb.ToString();

            if (mode == GarageMode.Visit || mode == GarageMode.BuyAll)
            {
                result = UpdateVisitedCities(result);
            }

            return result;
        }

        private static string UpdateVisitedCities(string content)
        {
            var cityMatches = System.Text.RegularExpressions.Regex.Matches(content, @"(?m)^\s*garages\[\d+\]:\s*garage\.([a-z0-9_]+)");
            var garageCities = new System.Collections.Generic.List<string>();
            foreach (System.Text.RegularExpressions.Match m in cityMatches)
            {
                string city = m.Groups[1].Value.Trim();
                if (!garageCities.Contains(city))
                    garageCities.Add(city);
            }

            // Collect every city referenced by company blocks too (company.volatile.<company>.<city>),
            // so ALL cities in the save are marked as visited, regardless of DLC or garages.
            var companyCityMatches = System.Text.RegularExpressions.Regex.Matches(content, @"(?m)^\s*company\s*:\s*company\.volatile\.[a-z0-9_]+\.([a-z0-9_]+)\s*\{");
            var companyCities = new System.Collections.Generic.List<string>();
            foreach (System.Text.RegularExpressions.Match m in companyCityMatches)
            {
                string city = m.Groups[1].Value.Trim();
                if (!companyCities.Contains(city))
                    companyCities.Add(city);
            }

            // Preserve existing visited cities (and their visit counts) instead of overwriting them,
            // so cities visited through gameplay are never lost when unlocking garages.
            var visitCounts = new System.Collections.Generic.Dictionary<string, int>();
            var existingCities = System.Text.RegularExpressions.Regex.Matches(content, @"(?m)^\s*visited_cities\[(\d+)\]:\s*(\S+)");
            var existingCounts = System.Text.RegularExpressions.Regex.Matches(content, @"(?m)^\s*visited_cities_count\[(\d+)\]:\s*(\d+)");
            var existingCountMap = new System.Collections.Generic.Dictionary<int, int>();
            foreach (System.Text.RegularExpressions.Match m in existingCounts)
            {
                existingCountMap[int.Parse(m.Groups[1].Value)] = int.Parse(m.Groups[2].Value);
            }
            foreach (System.Text.RegularExpressions.Match m in existingCities)
            {
                int index = int.Parse(m.Groups[1].Value);
                string city = m.Groups[2].Value.Trim();
                int count;
                if (!existingCountMap.TryGetValue(index, out count)) count = 1;
                if (count < 1) count = 1;
                if (!visitCounts.ContainsKey(city))
                    visitCounts.Add(city, count);
            }

            // Add every city that has a garage, so all garages become visitable.
            foreach (string city in garageCities)
            {
                if (!visitCounts.ContainsKey(city))
                    visitCounts.Add(city, 1);
            }

            // Add every city referenced by a company block, so all cities are marked as visited.
            foreach (string city in companyCities)
            {
                if (!visitCounts.ContainsKey(city))
                    visitCounts.Add(city, 1);
            }

            var cities = new System.Collections.Generic.List<string>(visitCounts.Keys);
            cities.Sort();

            if (cities.Count == 0)
                return content;

            int econStart = content.IndexOf("economy : ");
            if (econStart == -1)
                return content;

            int econOpenBrace = content.IndexOf("{", econStart);
            if (econOpenBrace == -1)
                return content;

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

            if (cleanEconLines.Count > 0 && cleanEconLines[cleanEconLines.Count - 1].Trim() == "}")
            {
                cleanEconLines.RemoveAt(cleanEconLines.Count - 1);
            }

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
                newEconBlockBuilder.Append(" visited_cities_count[").Append(i).Append("]: ").Append(visitCounts[cities[i]]).Append(nl);
            }

            newEconBlockBuilder.Append("}");

            string prefix = content.Substring(0, econOpenBrace);
            string suffix = content.Substring(econEnd + 1);

            return prefix + newEconBlockBuilder.ToString() + suffix;
        }
    }
}
