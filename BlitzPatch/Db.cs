using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LiteDB;

namespace BlitzPatch
{
    internal static class Db
    {
        private const string SettingsCollection = "settings";
        private const string GameDirectoryKey = "gameDirectory";
        private static readonly string SettingsDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.db");

        internal class UserDataRecord
        {
            public string DatabasePath { get; set; }
            public string CollectionName { get; set; }
            public BsonValue DocumentId { get; set; }
            public string JsonPayload { get; set; }
            public int? UserFactionType { get; set; }

            public string DisplayName
            {
                get
                {
                    var factionSuffix = UserFactionType.HasValue ? $" | Faction: {UserFactionType}" : string.Empty;
                    return $"{CollectionName} | Id: {DocumentId}{factionSuffix}";
                }
            }
        }

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

        public static bool TryLoadUserJson(string gameDirectory, out UserDataRecord record, out string message)
        {
            record = null;
            message = null;

            var databasePath = FindUserDatabasePath(gameDirectory);
            if (databasePath == null)
            {
                message = "No Data/User_*/a_ LiteDB file found.";
                return false;
            }

            try
            {
                using (var db = new LiteDatabase(databasePath))
                {
                    foreach (var collectionName in db.GetCollectionNames())
                    {
                        var col = db.GetCollection(collectionName);
                        foreach (var doc in col.FindAll())
                        {
                            if (doc == null || !doc.ContainsKey("j_"))
                            {
                                continue;
                            }

                            var jsonField = doc["j_"];
                            if (!jsonField.IsString)
                            {
                                continue;
                            }

                            var id = doc.ContainsKey("_id") ? doc["_id"] : BsonValue.Null;

                            record = new UserDataRecord
                            {
                                DatabasePath = databasePath,
                                CollectionName = collectionName,
                                DocumentId = id,
                                JsonPayload = jsonField.AsString,
                                UserFactionType = GameData.TryExtractUserFactionType(jsonField.AsString)
                            };

                            message = $"Loaded profile from collection '{collectionName}' (id: {id}).";
                            return true;
                        }
                    }
                }

                message = "No document with a 'j_' string field was found.";
                return false;
            }
            catch (Exception ex)
            {
                message = $"Failed to open LiteDB: {ex.Message}";
                record = null;
                return false;
            }
        }

        public static bool TryLoadAllUserJson(string gameDirectory, out List<UserDataRecord> records, out string message)
        {
            records = new List<UserDataRecord>();
            message = null;

            var databasePath = FindUserDatabasePath(gameDirectory);
            if (databasePath == null)
            {
                message = "No Data/User_*/a_ LiteDB file found.";
                return false;
            }

            try
            {
                using (var db = new LiteDatabase(databasePath))
                {
                    foreach (var collectionName in db.GetCollectionNames())
                    {
                        var col = db.GetCollection(collectionName);
                        foreach (var doc in col.FindAll())
                        {
                            if (doc == null || !doc.ContainsKey("j_"))
                            {
                                continue;
                            }

                            var jsonField = doc["j_"];
                            if (!jsonField.IsString)
                            {
                                continue;
                            }

                            var id = doc.ContainsKey("_id") ? doc["_id"] : BsonValue.Null;

                            records.Add(new UserDataRecord
                            {
                                DatabasePath = databasePath,
                                CollectionName = collectionName,
                                DocumentId = id,
                                JsonPayload = jsonField.AsString,
                                UserFactionType = GameData.TryExtractUserFactionType(jsonField.AsString)
                            });
                        }
                    }
                }

                if (records.Count == 0)
                {
                    message = "No document with a 'j_' string field was found.";
                    return false;
                }

                message = $"Loaded {records.Count} profile doc(s) from {Path.GetFileName(databasePath)}.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Failed to open LiteDB: {ex.Message}";
                records = null;
                return false;
            }
        }

        public static bool TrySaveUserJson(UserDataRecord record, string json, out string message)
        {
            message = null;

            if (record == null)
            {
                message = "No profile is loaded.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                message = "JSON is empty.";
                return false;
            }

            try
            {
                using (var db = new LiteDatabase(record.DatabasePath))
                {
                    var col = db.GetCollection(record.CollectionName);
                    var doc = col.FindById(record.DocumentId);

                    if (doc == null)
                    {
                        message = "Could not find the original document to update.";
                        return false;
                    }

                    doc["j_"] = json;

                    if (!col.Update(doc))
                    {
                        message = "LiteDB update returned false.";
                        return false;
                    }
                }

                message = "Saved changes back to LiteDB.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Failed to save: {ex.Message}";
                return false;
            }
        }

        public static bool TrySaveUserJsonAs(UserDataRecord record, string json, string targetPath, out string message)
        {
            message = null;

            if (record == null)
            {
                message = "No profile is loaded.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                message = "Choose a destination file.";
                return false;
            }

            if (string.Equals(record.DatabasePath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                return TrySaveUserJson(record, json, out message);
            }

            try
            {
                File.Copy(record.DatabasePath, targetPath, overwrite: true);
                var cloned = new UserDataRecord
                {
                    CollectionName = record.CollectionName,
                    DatabasePath = targetPath,
                    DocumentId = record.DocumentId,
                    JsonPayload = record.JsonPayload,
                    UserFactionType = record.UserFactionType
                };

                if (!TrySaveUserJson(cloned, json, out var innerMessage))
                {
                    message = innerMessage;
                    return false;
                }

                message = $"Saved copy to {targetPath}";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Save As failed: {ex.Message}";
                return false;
            }
        }

        public static bool TryExportDatabaseToJson(string gameDirectory, out string exportPath, out string message)
        {
            exportPath = null;
            message = null;

            if (string.IsNullOrWhiteSpace(gameDirectory))
            {
                message = "Game directory is empty.";
                return false;
            }

            var databasePath = FindUserDatabasePath(gameDirectory);
            if (databasePath == null)
            {
                message = "No Data/User_*/a_ LiteDB file found.";
                return false;
            }

            var directory = Path.GetDirectoryName(databasePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                message = "Could not determine database directory.";
                return false;
            }

            var exportDirectory = Path.Combine(directory, "blitzpatchdata");
            Directory.CreateDirectory(exportDirectory);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            exportPath = Path.Combine(exportDirectory, $"a_export_{timestamp}.json");

            try
            {
                using (var db = new LiteDatabase(databasePath))
                using (var writer = new StreamWriter(exportPath, false, new UTF8Encoding(false)))
                {
                    var collections = db.GetCollectionNames()
                                        .OrderBy(n => n)
                                        .ToList();

                    writer.WriteLine("{");

                    for (int i = 0; i < collections.Count; i++)
                    {
                        var name = collections[i];
                        var col = db.GetCollection(name);

                        writer.WriteLine($"  \"{name}\": [");

                        bool firstDoc = true;
                        foreach (var doc in col.FindAll())
                        {
                            if (!firstDoc) writer.WriteLine(",");
                            var json = JsonSerializer.Serialize(doc, pretty: true, writeBinary: false);
                            writer.Write(IndentJson(json, "    "));
                            firstDoc = false;
                        }

                        writer.WriteLine();
                        writer.Write("  ]");
                        if (i < collections.Count - 1) writer.Write(",");
                        writer.WriteLine();
                    }

                    writer.WriteLine("}");
                }

                message = $"Exported to {exportPath}";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Export failed: {ex.Message}";
                exportPath = null;
                return false;
            }
        }

        public static bool TryExportLiteDbFileToJson(string databasePath, out string exportPath, out string message)
        {
            exportPath = null;
            message = null;

            if (string.IsNullOrWhiteSpace(databasePath) || !File.Exists(databasePath))
            {
                message = "LiteDB file path is invalid.";
                return false;
            }

            var directory = Path.GetDirectoryName(databasePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                message = "Could not determine database directory.";
                return false;
            }

            var exportDirectory = Path.Combine(directory, "blitzpatchdata");
            Directory.CreateDirectory(exportDirectory);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            exportPath = Path.Combine(exportDirectory, $"a_export_{timestamp}.json");

            try
            {
                using (var db = new LiteDatabase(databasePath))
                using (var writer = new StreamWriter(exportPath, false, new UTF8Encoding(false)))
                {
                    var collections = db.GetCollectionNames()
                                        .OrderBy(n => n)
                                        .ToList();

                    writer.WriteLine("{");

                    for (int i = 0; i < collections.Count; i++)
                    {
                        var name = collections[i];
                        var col = db.GetCollection(name);

                        writer.WriteLine($"  \"{name}\": [");

                        bool firstDoc = true;
                        foreach (var doc in col.FindAll())
                        {
                            if (!firstDoc) writer.WriteLine(",");
                            var json = JsonSerializer.Serialize(doc, pretty: true, writeBinary: false);
                            writer.Write(IndentJson(json, "    "));
                            firstDoc = false;
                        }

                        writer.WriteLine();
                        writer.Write("  ]");
                        if (i < collections.Count - 1) writer.Write(",");
                        writer.WriteLine();
                    }

                    writer.WriteLine("}");
                }

                message = $"Exported to {exportPath}";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Export failed: {ex.Message}";
                exportPath = null;
                return false;
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

        public static bool TryInsertUserJson(string databasePath, string collectionName, string json, int? userFactionType, out UserDataRecord record, out string message)
        {
            record = null;
            message = null;

            if (string.IsNullOrWhiteSpace(databasePath) || string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(json))
            {
                message = "Insert failed: missing path, collection, or JSON.";
                return false;
            }

            try
            {
                using (var db = new LiteDatabase(databasePath))
                {
                    var col = db.GetCollection(collectionName);

                    var doc = new BsonDocument
                    {
                        ["j_"] = json
                    };

                    if (userFactionType.HasValue)
                    {
                        doc["UserFactionType"] = userFactionType.Value;
                    }

                    col.Insert(doc);

                    var id = doc.ContainsKey("_id") ? doc["_id"] : BsonValue.Null;

                    record = new UserDataRecord
                    {
                        DatabasePath = databasePath,
                        CollectionName = collectionName,
                        DocumentId = id,
                        JsonPayload = json,
                        UserFactionType = userFactionType
                    };

                    message = $"Inserted new document into collection '{collectionName}'.";
                    return true;
                }
            }
            catch (Exception ex)
            {
                message = $"Insert failed: {ex.Message}";
                record = null;
                return false;
            }
        }

        private static string IndentJson(string json, string indent)
        {
            var lines = json.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = indent + lines[i];
            }
            return string.Join(Environment.NewLine, lines);
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
