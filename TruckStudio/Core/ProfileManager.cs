using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TruckStudio.Core
{
    public class ETS2Profile
    {
        public string ProfileId { get; set; }
        public string ProfileName { get; set; }
        public string ProfilePath { get; set; }
        public List<ETS2Save> Saves { get; set; }
    }

    public class ETS2Save
    {
        public string SaveName { get; set; }
        public string SavePath { get; set; }
        public DateTime LastModified { get; set; }
    }

    public static class ProfileManager
    {
        public static string GetEts2ProfilesPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Euro Truck Simulator 2", "profiles");
        }

        public static List<ETS2Profile> GetProfiles()
        {
            var profilesPath = GetEts2ProfilesPath();
            var profiles = new List<ETS2Profile>();

            if (!Directory.Exists(profilesPath)) return profiles;

            foreach (var dir in Directory.GetDirectories(profilesPath))
            {
                var profile = new ETS2Profile
                {
                    ProfileId = Path.GetFileName(dir),
                    ProfilePath = dir,
                    Saves = new List<ETS2Save>()
                };

                // Decrypt profile.sii to get actual name
                var profileSiiPath = Path.Combine(dir, "profile.sii");
                if (File.Exists(profileSiiPath))
                {
                    try
                    {
                        var decrypted = SiiDecryptor.DecryptFile(profileSiiPath);
                        if (decrypted != null)
                        {
                            // Basic extraction (we'll improve parsing later)
                            var lines = decrypted.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                            var nameLine = lines.FirstOrDefault(l => l.Contains("profile_name:"));
                            if (nameLine != null)
                            {
                                var parts = nameLine.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2)
                                {
                                    profile.ProfileName = parts[1];
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (string.IsNullOrEmpty(profile.ProfileName))
                    profile.ProfileName = profile.ProfileId;

                // Scan Saves
                var savesPath = Path.Combine(dir, "save");
                if (Directory.Exists(savesPath))
                {
                    foreach (var saveDir in Directory.GetDirectories(savesPath))
                    {
                        profile.Saves.Add(new ETS2Save
                        {
                            SaveName = Path.GetFileName(saveDir),
                            SavePath = saveDir,
                            LastModified = Directory.GetLastWriteTime(saveDir)
                        });
                    }
                    profile.Saves = profile.Saves.OrderByDescending(s => s.LastModified).ToList();
                }

                profiles.Add(profile);
            }

            return profiles;
        }
    }
}
