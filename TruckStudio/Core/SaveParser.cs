using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TruckStudio.Core
{
    public static class SaveParser
    {
        public static string ExtractMoney(string saveContent)
        {
            // The player's bank is the LAST one in the file
            var match = Regex.Match(saveContent, @"money_account:\s*(-?\d+)", RegexOptions.RightToLeft);
            return match.Success ? match.Groups[1].Value : "0";
        }

        public static string ExtractXP(string saveContent)
        {
            // The player is the FIRST experience_points occurrence
            var match = Regex.Match(saveContent, @"experience_points:\s*(-?\d+)");
            return match.Success ? match.Groups[1].Value : "0";
        }

        public static string SetMoney(string saveContent, string newMoney)
        {
            // Replace the LAST money_account in the file
            var regex = new Regex(@"money_account:\s*-?\d+", RegexOptions.RightToLeft);
            return regex.Replace(saveContent, $"money_account: {newMoney}", 1);
        }

        public static string SetXP(string saveContent, string newXp)
        {
            // Replace the FIRST occurrence of experience_points
            var regex = new Regex(@"experience_points:\s*-?\d+");
            return regex.Replace(saveContent, $"experience_points: {newXp}", 1);
        }

        public static string FixAllTrucksAndTrailers(string saveContent)
        {
            // Use a highly precise regex to avoid corrupting the save file (which causes ETS2 to roll back to a backup).
            // This perfectly mimics the original working regex (\w*wear) but safely adds ETS2 1.50 parts_damage.
            var content = Regex.Replace(saveContent, @"(?m)^(\s*(?:\w*wear|parts_damage)(?:_unfixable)?(?:\[\d+\])?:\s*)[^\r\n]*", "$1 0");
            
            // Note: We no longer reset fuel here. If they have infinite fuel (25), it stays at 25.
            
            return content;
        }

        public static string MaxSkills(string saveContent)
        {
            // adr is a bitmask for 6 skills (63 = 111111)
            var content = Regex.Replace(saveContent, @"(?m)^(\s*adr:\s*)[^\r\n]*", "$1 63");
            
            string[] skills = { "long_dist", "heavy", "fragile", "urgent", "mechanical" };
            foreach (var skill in skills)
            {
                content = Regex.Replace(content, $@"(?m)^(\s*{skill}:\s*)[^\r\n]*", "$1 6");
            }
            return content;
        }

        public static string FixCargoDamage(string saveContent)
        {
            // Reset cargo damage to 0
            return Regex.Replace(saveContent, @"(?m)^(\s*cargo_damage:\s*)[^\r\n]*", "$1 0");
        }

        public static string RefillFuel(string saveContent)
        {
            // Refuel trucks (float 1.0)
            return Regex.Replace(saveContent, @"(?m)^(\s*fuel_relative:\s*)[^\r\n]*", "$1 1");
        }

        public static string Teleport(string saveContent, string x, string y, string z, string rotation = null)
        {
            if (string.IsNullOrEmpty(rotation))
            {
                // Replace only the location part, leaving existing rotation untouched
                string content = Regex.Replace(saveContent, @"(?m)^(\s*my_truck_placement:\s*)\([^)]+\)", $"$1({x}, {y}, {z})");
                content = Regex.Replace(content, @"(?m)^(\s*truck_placement:\s*)\([^)]+\)", $"$1({x}, {y}, {z})");
                content = Regex.Replace(content, @"(?m)^(\s*stored_vehicle_placement:\s*)\([^)]+\)", $"$1({x}, {y}, {z})");
                
                // Allow the game engine to auto-align trailers by forcing them to origin relative placement
                content = Regex.Replace(content, @"(?m)^(\s*trailer_placement:\s*)\([^)]+\)", $"$1(0, 0, 0)");
                content = Regex.Replace(content, @"(?m)^(\s*stored_trailer_placements\[\d+\]:\s*)\([^)]+\)", $"$1(0, 0, 0)");
                content = Regex.Replace(content, @"(?m)^(\s*slave_trailer_placements\[\d+\]:\s*)\([^)]+\)", $"$1(0, 0, 0)");
                return content;
            }
            else
            {
                // Replace both location and rotation. Safely match without touching \r or \n.
                string content = Regex.Replace(saveContent, @"(?m)^(\s*my_truck_placement:\s*)[^\r\n]*", $"$1({x}, {y}, {z}) {rotation}");
                content = Regex.Replace(content, @"(?m)^(\s*truck_placement:\s*)[^\r\n]*", $"$1({x}, {y}, {z}) {rotation}");
                content = Regex.Replace(content, @"(?m)^(\s*stored_vehicle_placement:\s*)[^\r\n]*", $"$1({x}, {y}, {z}) {rotation}");
                
                // Allow the game engine to auto-align trailers
                content = Regex.Replace(content, @"(?m)^(\s*trailer_placement:\s*)[^\r\n]*", $"$1(0, 0, 0) {rotation}");
                content = Regex.Replace(content, @"(?m)^(\s*stored_trailer_placements\[\d+\]:\s*)[^\r\n]*", $"$1(0, 0, 0) {rotation}");
                content = Regex.Replace(content, @"(?m)^(\s*slave_trailer_placements\[\d+\]:\s*)[^\r\n]*", $"$1(0, 0, 0) {rotation}");
                return content;
            }
        }

        public static System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> ExtractCitiesAndCompanies(string saveContent)
        {
            var cityCompanies = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();
            string[] separator = new string[] { "\r\n", "\n" };
            string[] lines = saveContent.Split(separator, StringSplitOptions.None);

            string pendingCompany = null;
            string pendingCity = null;
            bool inCompanyBlock = false;
            bool hasJobSlot = false;
            int braceDepth = 0;

            foreach (var line in lines)
            {
                string trimmed = line.Trim();

                if (!inCompanyBlock)
                {
                    // Detect company block header: company : company.volatile.X.Y {
                    if (trimmed.StartsWith("company") && trimmed.Contains("company.volatile."))
                    {
                        int colonIdx = line.IndexOf(':');
                        if (colonIdx != -1)
                        {
                            string key = line.Substring(0, colonIdx).Trim();
                            if (key == "company")
                            {
                                string val = line.Substring(colonIdx + 1).Trim();
                                if (val.EndsWith("{")) val = val.Substring(0, val.Length - 1).Trim();
                                if (val.StartsWith("company.volatile."))
                                {
                                    string sub = val.Substring(17);
                                    int dotIdx = sub.LastIndexOf('.');
                                    if (dotIdx != -1)
                                    {
                                        pendingCompany = sub.Substring(0, dotIdx);
                                        pendingCity    = sub.Substring(dotIdx + 1);
                                        inCompanyBlock = true;
                                        hasJobSlot     = false;
                                        braceDepth     = 1; // the opening { on this line
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Track brace depth to know when the company block ends
                    foreach (char c in trimmed)
                    {
                        if (c == '{') braceDepth++;
                        else if (c == '}') braceDepth--;
                    }

                    // Check for any job_offer[N] line
                    if (!hasJobSlot && trimmed.StartsWith("job_offer["))
                        hasJobSlot = true;

                    // Block closed
                    if (braceDepth <= 0)
                    {
                        if (hasJobSlot && pendingCompany != null && pendingCity != null)
                        {
                            if (!cityCompanies.ContainsKey(pendingCity))
                                cityCompanies[pendingCity] = new System.Collections.Generic.List<string>();
                            if (!cityCompanies[pendingCity].Contains(pendingCompany))
                                cityCompanies[pendingCity].Add(pendingCompany);
                        }
                        inCompanyBlock = false;
                        pendingCompany = null;
                        pendingCity    = null;
                    }
                }
            }
            return cityCompanies;
        }


        public static System.Collections.Generic.List<string> ExtractCargoes(string saveContent)
        {
            var cargoes = new System.Collections.Generic.List<string>();
            string[] separator = new string[] { "\r\n", "\n" };
            string[] lines = saveContent.Split(separator, StringSplitOptions.None);

            foreach (var line in lines)
            {
                if (line.Contains("cargo:"))
                {
                    int colonIdx = line.IndexOf(':');
                    if (colonIdx != -1)
                    {
                        string key = line.Substring(0, colonIdx).Trim();
                        if (key == "cargo")
                        {
                            string val = line.Substring(colonIdx + 1).Trim();
                            val = val.Replace("\"", "");
                            if (val.StartsWith("cargo."))
                            {
                                string cargoName = val.Substring(6);
                                if (!cargoes.Contains(cargoName) && cargoName != "null")
                                {
                                    cargoes.Add(cargoName);
                                }
                            }
                        }
                    }
                }
            }
            cargoes.Sort();
            return cargoes;
        }

        private static (string variant, string definition, string unitsCount) FindTrailerForCargoFast(string[] lines, string cargo)
        {
            string targetCargo = "cargo." + cargo;
            string currentVariant = null;
            string currentDefinition = null;
            string currentUnits = "1";
            bool inJobOfferData = false;
            bool foundTargetCargo = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("job_offer_data :"))
                {
                    inJobOfferData = true;
                    foundTargetCargo = false;
                    currentVariant = null;
                    currentDefinition = null;
                    currentUnits = "1";
                    continue;
                }

                if (inJobOfferData)
                {
                    if (line.Trim() == "}")
                    {
                        if (foundTargetCargo && currentVariant != null && currentDefinition != null)
                        {
                            return (currentVariant, currentDefinition, currentUnits);
                        }
                        inJobOfferData = false;
                        continue;
                    }

                    int colonIdx = line.IndexOf(':');
                    if (colonIdx != -1)
                    {
                        string key = line.Substring(0, colonIdx).Trim();
                        string val = line.Substring(colonIdx + 1).Trim();

                        if (key == "cargo")
                        {
                            if (val.Contains(targetCargo))
                            {
                                foundTargetCargo = true;
                            }
                        }
                        else if (key == "trailer_variant")
                        {
                            currentVariant = val;
                        }
                        else if (key == "trailer_definition")
                        {
                            currentDefinition = val;
                        }
                        else if (key == "units_count")
                        {
                            currentUnits = val;
                        }
                    }
                }
            }
            return (null, null, null);
        }

        private class EconomyEvent
        {
            public string Id { get; set; }
            public string OriginalBlock { get; set; }
            public uint Time { get; set; }
            public string UnitLink { get; set; }
            public int Param { get; set; }
        }

        private static List<EconomyEvent> ParseEconomyEventsFast(string[] lines, string lineSeparator)
        {
            var list = new List<EconomyEvent>();
            bool inEvent = false;
            string currentId = null;
            uint currentTime = 0;
            string currentLink = null;
            int currentParam = 0;
            int blockStartIdx = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("economy_event :"))
                {
                    inEvent = true;
                    int colonIdx = line.IndexOf(':');
                    currentId = line.Substring(colonIdx + 1).Trim();
                    if (currentId.EndsWith("{"))
                    {
                        currentId = currentId.Substring(0, currentId.Length - 1).Trim();
                    }
                    currentTime = 0;
                    currentLink = null;
                    currentParam = 0;
                    blockStartIdx = i;
                    continue;
                }

                if (inEvent)
                {
                    if (line.Trim() == "}")
                    {
                        if (currentId != null && currentLink != null)
                        {
                            var sb = new System.Text.StringBuilder();
                            for (int j = blockStartIdx; j <= i; j++)
                            {
                                sb.Append(lines[j]);
                                if (j < i) sb.Append(lineSeparator);
                            }
                            list.Add(new EconomyEvent
                            {
                                Id = currentId,
                                OriginalBlock = sb.ToString(),
                                Time = currentTime,
                                UnitLink = currentLink,
                                Param = currentParam
                            });
                        }
                        inEvent = false;
                        continue;
                    }

                    int colonIdx = line.IndexOf(':');
                    if (colonIdx != -1)
                    {
                        string key = line.Substring(0, colonIdx).Trim();
                        string val = line.Substring(colonIdx + 1).Trim();

                        if (key == "time")
                        {
                            uint.TryParse(val, out currentTime);
                        }
                        else if (key == "unit_link")
                        {
                            currentLink = val;
                        }
                        else if (key == "param")
                        {
                            int.TryParse(val, out currentParam);
                        }
                    }
                }
            }

            return list;
        }

        private static (string queueBlock, string queueId, List<string> queueItems) FindQueueFast(string[] lines, string lineSeparator)
        {
            bool inQueue = false;
            string queueId = null;
            int blockStartIdx = -1;
            var queueItems = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("economy_event_queue :"))
                {
                    inQueue = true;
                    int colonIdx = line.IndexOf(':');
                    queueId = line.Substring(colonIdx + 1).Trim();
                    if (queueId.EndsWith("{"))
                    {
                        queueId = queueId.Substring(0, queueId.Length - 1).Trim();
                    }
                    blockStartIdx = i;
                    queueItems.Clear();
                    continue;
                }

                if (inQueue)
                {
                    if (line.Trim() == "}")
                    {
                        var sb = new System.Text.StringBuilder();
                        for (int j = blockStartIdx; j <= i; j++)
                        {
                            sb.Append(lines[j]);
                            if (j < i) sb.Append(lineSeparator);
                        }
                        return (sb.ToString(), queueId, queueItems);
                    }

                    int colonIdx = line.IndexOf(':');
                    if (colonIdx != -1)
                    {
                        string key = line.Substring(0, colonIdx).Trim();
                        string val = line.Substring(colonIdx + 1).Trim();

                        if (key.StartsWith("data["))
                        {
                            queueItems.Add(val);
                        }
                    }
                }
            }
            return (null, null, null);
        }

        public static void Log(string message)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (string.IsNullOrEmpty(desktop)) desktop = @"F:\Mouli\Desktop";
                string logPath = System.IO.Path.Combine(desktop, "truckstudio_debug.log");
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\r\n");
            }
            catch {}
        }

        /// <summary>
        /// Reads the shortest_distance_km already stored in the first job offer slot of the given
        /// source company, so the UI can pre-fill the Distance field automatically.
        /// Returns 0 if the value cannot be found.
        /// </summary>
        public static int ExtractJobDistance(string saveContent, string sourceCity, string sourceCompany)
        {
            try
            {
                // Find the company block
                var companyRegex = new Regex($@"company\s*:\s*company\.volatile\.{Regex.Escape(sourceCompany)}\.{Regex.Escape(sourceCity)}\b");
                var companyMatch = companyRegex.Match(saveContent);
                if (!companyMatch.Success) return 0;

                int headerIdx = companyMatch.Index;
                int openBraceIdx = saveContent.IndexOf('{', headerIdx);
                if (openBraceIdx == -1) return 0;

                int braceCount = 1, closeBraceIdx = -1;
                for (int i = openBraceIdx + 1; i < saveContent.Length; i++)
                {
                    if (saveContent[i] == '{') braceCount++;
                    else if (saveContent[i] == '}') { braceCount--; if (braceCount == 0) { closeBraceIdx = i; break; } }
                }
                if (closeBraceIdx == -1) return 0;

                string companyBlock = saveContent.Substring(headerIdx, closeBraceIdx - headerIdx + 1);
                var jobOfferMatch = Regex.Match(companyBlock, @"job_offer\[0\]:\s*([_a-zA-Z0-9.]+)");
                if (!jobOfferMatch.Success) return 0;

                string jobOfferId = jobOfferMatch.Groups[1].Value;

                // Locate the job_offer_data block
                string jobOfferHeader = $"job_offer_data : {jobOfferId}";
                int jobHeaderIdx = saveContent.IndexOf(jobOfferHeader);
                if (jobHeaderIdx == -1) return 0;

                int jobOpenBraceIdx = saveContent.IndexOf('{', jobHeaderIdx);
                if (jobOpenBraceIdx == -1) return 0;

                int jobBrace = 1, jobCloseBraceIdx = -1;
                for (int i = jobOpenBraceIdx + 1; i < saveContent.Length; i++)
                {
                    if (saveContent[i] == '{') jobBrace++;
                    else if (saveContent[i] == '}') { jobBrace--; if (jobBrace == 0) { jobCloseBraceIdx = i; break; } }
                }
                if (jobCloseBraceIdx == -1) return 0;

                string jobBlock = saveContent.Substring(jobHeaderIdx, jobCloseBraceIdx - jobHeaderIdx + 1);
                var distMatch = Regex.Match(jobBlock, @"shortest_distance_km:\s*(\d+)");
                if (distMatch.Success && int.TryParse(distMatch.Groups[1].Value, out int dist))
                    return dist;
            }
            catch { }
            return 0;
        }

        public static string InjectFreightJob(string saveContent, string sourceCity, string sourceCompany, string destCity, string destCompany, string cargo, int urgency, string distance)
        {
            Log("InjectFreightJob: Start");
            string lineSeparator = saveContent.Contains("\r\n") ? "\r\n" : "\n";
            string[] separator = new string[] { "\r\n", "\n" };
            Log("InjectFreightJob: Splitting lines...");
            string[] lines = saveContent.Split(separator, StringSplitOptions.None);
            Log($"InjectFreightJob: Split into {lines.Length} lines");

            // Find current game time from the economy block
            uint gameTime = 0;
            int economyIdx = saveContent.IndexOf("economy :");
            if (economyIdx != -1)
            {
                int openBrace = saveContent.IndexOf('{', economyIdx);
                if (openBrace != -1)
                {
                    int ecoBraceCount = 1;
                    int ecoCloseBrace = -1;
                    for (int ecoI = openBrace + 1; ecoI < saveContent.Length; ecoI++)
                    {
                        if (saveContent[ecoI] == '{') ecoBraceCount++;
                        else if (saveContent[ecoI] == '}')
                        {
                            ecoBraceCount--;
                            if (ecoBraceCount == 0)
                            {
                                ecoCloseBrace = ecoI;
                                break;
                            }
                        }
                    }
                    if (ecoCloseBrace != -1)
                    {
                        string economyContent = saveContent.Substring(openBrace, ecoCloseBrace - openBrace + 1);
                        var match = Regex.Match(economyContent, @"(?m)^\s*game_time:\s*(\d+)");
                        if (match.Success)
                        {
                            uint.TryParse(match.Groups[1].Value, out gameTime);
                        }
                    }
                }
            }
            // Calculate distance-based expiration time to prevent "Late" status on long routes
            Log($"InjectFreightJob: gameTime={gameTime}");
            int distVal = 0;
            int.TryParse(distance, out distVal);
            // Average speed assumption: 60 km/h -> 1 minute per km. 
            // We use (distVal * 2) to give a generous driving time margin, plus a 3-day (4320 minutes) base buffer.
            uint drivingTimeMargin = (uint)(distVal * 2);
            uint expirationTime = gameTime + drivingTimeMargin + 4320;
            Log($"InjectFreightJob: distVal={distVal}, drivingMargin={drivingTimeMargin}, expirationTime={expirationTime}");

            // Find the company block using regex to support spacing variations around the colon
            Log($"InjectFreightJob: Matching company regex for company.volatile.{sourceCompany}.{sourceCity}...");
            var companyRegex = new Regex($@"company\s*:\s*company\.volatile\.{Regex.Escape(sourceCompany)}\.{Regex.Escape(sourceCity)}\b");
            var companyMatch = companyRegex.Match(saveContent);
            if (!companyMatch.Success)
            {
                Log("InjectFreightJob: ERROR: Company not found!");
                throw new Exception($"Company '{sourceCompany}' in '{sourceCity}' not found in save file.");
            }
            int headerIdx = companyMatch.Index;
            Log($"InjectFreightJob: Company found at index {headerIdx}");

            // Find the end of this company block (closing brace)
            int openBraceIdx = saveContent.IndexOf('{', headerIdx);
            if (openBraceIdx == -1)
            {
                Log("InjectFreightJob: ERROR: Missing open brace for company");
                throw new Exception("Malformed save file: missing open brace for company.");
            }

            int braceCount = 1;
            int closeBraceIdx = -1;
            for (int i = openBraceIdx + 1; i < saveContent.Length; i++)
            {
                if (saveContent[i] == '{') braceCount++;
                else if (saveContent[i] == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        closeBraceIdx = i;
                        break;
                    }
                }
            }

            if (closeBraceIdx == -1)
            {
                Log("InjectFreightJob: ERROR: Missing close brace for company");
                throw new Exception("Malformed save file: missing close brace for company.");
            }

            string companyBlock = saveContent.Substring(headerIdx, closeBraceIdx - headerIdx + 1);
            Log("InjectFreightJob: Company block extracted");

            // Find the first available job offer slot (try [0] through [9])
            Match jobOfferMatch = Match.Empty;
            for (int slot = 0; slot <= 9; slot++)
            {
                jobOfferMatch = Regex.Match(companyBlock, $@"job_offer\[{slot}\]:\s*([_a-zA-Z0-9.]+)");
                if (jobOfferMatch.Success)
                {
                    Log($"InjectFreightJob: Using job_offer[{slot}]");
                    break;
                }
            }
            if (!jobOfferMatch.Success)
            {
                Log("InjectFreightJob: ERROR: This company has no job offer slots (visit it in-game first).");
                throw new Exception(
                    $"The company '{sourceCompany}' in '{sourceCity}' has no job offer slots available.\n\n" +
                    "This usually means the company hasn't generated any jobs yet.\n" +
                    "Fix: Load your save in ETS2, drive near or visit this company, save the game, then try again.");
            }

            string jobOfferId = jobOfferMatch.Groups[1].Value;
            Log($"InjectFreightJob: jobOfferId={jobOfferId}");

            // Locate the job_offer_data block for this jobOfferId: job_offer_data : jobOfferId { ... }
            string jobOfferHeader = $"job_offer_data : {jobOfferId}";
            int jobHeaderIdx = saveContent.IndexOf(jobOfferHeader);
            if (jobHeaderIdx == -1)
            {
                Log($"InjectFreightJob: ERROR: Job offer data block for ID {jobOfferId} not found");
                throw new Exception($"Job offer data block for ID '{jobOfferId}' not found in save file.");
            }

            int jobOpenBraceIdx = saveContent.IndexOf('{', jobHeaderIdx);
            if (jobOpenBraceIdx == -1)
            {
                Log("InjectFreightJob: ERROR: Missing open brace for job offer");
                throw new Exception("Malformed save file: missing open brace for job offer.");
            }

            int jobBraceCount = 1;
            int jobCloseBraceIdx = -1;
            for (int i = jobOpenBraceIdx + 1; i < saveContent.Length; i++)
            {
                if (saveContent[i] == '{') jobBraceCount++;
                else if (saveContent[i] == '}')
                {
                    jobBraceCount--;
                    if (jobBraceCount == 0)
                    {
                        jobCloseBraceIdx = i;
                        break;
                    }
                }
            }

            if (jobCloseBraceIdx == -1)
            {
                Log("InjectFreightJob: ERROR: Missing close brace for job offer");
                throw new Exception("Malformed save file: missing close brace for job offer.");
            }

            string jobOfferBlock = saveContent.Substring(jobHeaderIdx, jobCloseBraceIdx - jobHeaderIdx + 1);
            Log("InjectFreightJob: Job offer block extracted");

            // Find a compatible trailer configuration for this cargo from another job offer
            Log($"InjectFreightJob: Finding trailer for cargo {cargo}...");
            var (trailerVariant, trailerDef, foundUnitsCount) = FindTrailerForCargoFast(lines, cargo);
            Log($"InjectFreightJob: Trailer lookup result: variant={trailerVariant}, def={trailerDef}, units={foundUnitsCount}");
            if (string.IsNullOrEmpty(trailerVariant)) trailerVariant = "default";
            if (string.IsNullOrEmpty(trailerDef)) trailerDef = "trailer.curtain";
            string unitsCount = !string.IsNullOrEmpty(foundUnitsCount) ? foundUnitsCount : "1";

            // Modify the job offer block contents
            Log("InjectFreightJob: Modifying job offer block contents...");
            string modifiedJobBlock = jobOfferBlock;
            modifiedJobBlock = Regex.Replace(modifiedJobBlock, @"(?m)^(\s*target:\s*)[^\r\n]*", $"${{1}}\"{destCompany}.{destCity}\"");
            modifiedJobBlock = Regex.Replace(modifiedJobBlock, @"(?m)^(\s*cargo:\s*)[^\r\n]*", $"${{1}}cargo.{cargo}");
            modifiedJobBlock = Regex.Replace(modifiedJobBlock, @"(?m)^(\s*urgency:\s*)[^\r\n]*", $"${{1}}{urgency}");
            modifiedJobBlock = Regex.Replace(modifiedJobBlock, @"(?m)^(\s*shortest_distance_km:\s*)[^\r\n]*", $"${{1}}{distance}");
            modifiedJobBlock = Regex.Replace(modifiedJobBlock, @"(?m)^(\s*expiration_time:\s*)[^\r\n]*", $"${{1}}{expirationTime}");
            modifiedJobBlock = Regex.Replace(modifiedJobBlock, @"(?m)^(\s*ferry_time:\s*)[^\r\n]*", "${1}0");
            modifiedJobBlock = Regex.Replace(modifiedJobBlock, @"(?m)^(\s*ferry_price:\s*)[^\r\n]*", "${1}0");
            modifiedJobBlock = Regex.Replace(modifiedJobBlock, @"(?m)^(\s*units_count:\s*)[^\r\n]*", $"${{1}}{unitsCount}");
            modifiedJobBlock = Regex.Replace(modifiedJobBlock, @"(?m)^(\s*trailer_variant:\s*)[^\r\n]*", $"${{1}}{trailerVariant}");
            modifiedJobBlock = Regex.Replace(modifiedJobBlock, @"(?m)^(\s*trailer_definition:\s*)[^\r\n]*", $"${{1}}{trailerDef}");

            // Replace the old job offer block in the full save content
            Log("InjectFreightJob: Replacing job offer block in save content...");
            string updatedContent = saveContent.Replace(jobOfferBlock, modifiedJobBlock);

            // Parse economy events fast
            Log("InjectFreightJob: Parsing economy events...");
            var eventsList = ParseEconomyEventsFast(lines, lineSeparator);
            Log($"InjectFreightJob: Parsed {eventsList.Count} economy events");

            // Find the event for the source company and update its trigger time to match the job expiration time
            string targetLink = $"company.volatile.{sourceCompany}.{sourceCity}";
            Log($"InjectFreightJob: Searching economy event for link {targetLink} and Param 0...");
            var targetEvent = eventsList.Find(e => e.UnitLink == targetLink && e.Param == 0);
            if (targetEvent != null)
            {
                Log($"InjectFreightJob: Event found! ID={targetEvent.Id}. Updating event time to {expirationTime}...");
                targetEvent.Time = expirationTime;
                string modifiedBlock = Regex.Replace(targetEvent.OriginalBlock, @"(?m)^(\s*time:\s*)[^\r\n]*", $"${{1}}{targetEvent.Time}");
                updatedContent = updatedContent.Replace(targetEvent.OriginalBlock, modifiedBlock);
                Log("InjectFreightJob: Event updated in content");
            }
            else
            {
                Log("InjectFreightJob: WARNING: Economy event not found for company volatile link!");
            }

            // Sort and update the economy_event_queue to ensure it is sorted in ascending order of trigger times
            Log("InjectFreightJob: Parsing economy event queue...");
            var (queueBlock, queueId, queueItems) = FindQueueFast(lines, lineSeparator);
            if (queueBlock != null && queueId != null && queueItems != null)
            {
                Log($"InjectFreightJob: Queue found! ID={queueId}, count={queueItems.Count}. Sorting queue...");
                var eventDict = eventsList.ToDictionary(e => e.Id, e => e.Time);
                var sortedQueueItems = queueItems.OrderBy(item => eventDict.ContainsKey(item) ? eventDict[item] : 0).ToList();

                // Reconstruct the queue block
                Log("InjectFreightJob: Reconstructing queue block...");
                var sb = new System.Text.StringBuilder();
                sb.Append("economy_event_queue : ").Append(queueId).Append(" {").Append(lineSeparator);
                sb.Append(" data: ").Append(sortedQueueItems.Count).Append(lineSeparator);
                for (int i = 0; i < sortedQueueItems.Count; i++)
                {
                    sb.Append(" data[").Append(i).Append("]: ").Append(sortedQueueItems[i]).Append(lineSeparator);
                }
                sb.Append("}");

                Log("InjectFreightJob: Replacing queue block in content...");
                updatedContent = updatedContent.Replace(queueBlock, sb.ToString());
                Log("InjectFreightJob: Queue block updated");
            }
            else
            {
                Log("InjectFreightJob: WARNING: Queue block not found!");
            }

            Log("InjectFreightJob: Complete success");
            return updatedContent;
        }

    }
}
