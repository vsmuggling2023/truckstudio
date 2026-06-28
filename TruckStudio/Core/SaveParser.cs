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
    }
}
