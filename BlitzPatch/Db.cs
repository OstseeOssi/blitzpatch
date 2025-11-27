using System;
using System.IO;
using System.Linq;
using LiteDB;

namespace BlitzPatch
{
    internal static class Db
    {
        private const string SettingsCollection = "settings";
        private const string GameDirectoryKey = "gameDirectory";
        private static readonly string SettingsDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.db");

        public static void SaveGameDirectory(string path)
        {
            try
            {
                using (var db = new LiteDatabase(SettingsDbPath))
                {
                    var col = db.GetCollection<Setting>(SettingsCollection);
                    col.Upsert(new Setting
                    {
                        Key = GameDirectoryKey,
                        Value = path,
                        UpdatedUtc = DateTime.UtcNow
                    });
                }
            }
            catch
            {
                // Persisting the setting is best-effort; ignore failures for now.
            }
        }

        public static string LoadGameDirectory()
        {
            try
            {
                if (!File.Exists(SettingsDbPath))
                {
                    return null;
                }

                using (var db = new LiteDatabase(SettingsDbPath))
                {
                    var col = db.GetCollection<Setting>(SettingsCollection);
                    var setting = col.FindById(GameDirectoryKey);
                    return setting?.Value;
                }
            }
            catch
            {
                return null;
            }
        }

        public static bool TryBackupUserDatabase(string gameDirectory, out string backupPath, out string message)
        {
            backupPath = null;
            message = null;

            try
            {
                var sourcePath = FindUserDatabasePath(gameDirectory);
                if (sourcePath == null)
                {
                    message = "No Data/User_*/a_ file found to back up.";
                    return false;
                }

                var directory = Path.GetDirectoryName(sourcePath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    message = "Unable to determine directory for a_ backup.";
                    return false;
                }

                var backupDirectory = Path.Combine(directory, "blitzpatchdata");
                Directory.CreateDirectory(backupDirectory);

                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                backupPath = Path.Combine(backupDirectory, $"a_old_{timestamp}");
                File.Copy(sourcePath, backupPath, overwrite: false);

                message = $"Backed up a_ to {backupPath}";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Backup failed: {ex.Message}";
                backupPath = null;
                return false;
            }
        }

        private static string FindUserDatabasePath(string gameDirectory)
        {
            if (string.IsNullOrWhiteSpace(gameDirectory))
            {
                return null;
            }

            var dataPath = Path.Combine(gameDirectory, "Data");
            if (!Directory.Exists(dataPath))
            {
                return null;
            }

            var userDirectories = Directory.EnumerateDirectories(dataPath, "User_*", SearchOption.TopDirectoryOnly)
                                           .OrderBy(d => d)
                                           .ToList();

            // Also check a literal Data/User_ directory in case no suffix is used.
            var explicitUserDir = Path.Combine(dataPath, "User_");
            if (Directory.Exists(explicitUserDir))
            {
                userDirectories.Insert(0, explicitUserDir);
            }

            foreach (var dir in userDirectories)
            {
                var candidate = Path.Combine(dir, "a_");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private class Setting
        {
            [BsonId]
            public string Key { get; set; }

            public string Value { get; set; }

            public DateTime UpdatedUtc { get; set; }
        }
    }
}
