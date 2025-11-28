using System;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text.Json;

namespace BlitzPatch
{
    public partial class Gui : Form
    {
        private const string DefaultStatus = "Select your Blitzkrieg 3 directory to continue.";
        private Db.UserDataRecord currentRecord;
        private GameData.UserProfile currentProfile;
        private GameData.UserMapDocument currentUserMap;
        private BindingList<GameData.Unit> unitBinding = new BindingList<GameData.Unit>();
        private bool suppressRecordListEvent;
        private List<Db.UserDataRecord> loadedRecords = new List<Db.UserDataRecord>();
        private string[] allUnits = GameData.AllUnitsDistinct;

        private class FactionFilterOption
        {
            public string Label { get; set; }
            public int? Value { get; set; }

            public override string ToString()
            {
                return Label;
            }
        }

        public Gui()
        {
            InitializeComponent();
        }

        private void Gui_Load(object sender, EventArgs e)
        {
            ConfigureFactionFilter();
            ConfigureUnitsGrid();
            unitsDataGridView.DataSource = unitBinding;
            LoadSavedGameDirectory();
            ShowPanel(landingPanel);
        }

        private void landingStartButton_Click(object sender, EventArgs e)
        {
            if (!IsValidGameDirectory(textBox1.Text, out var reason))
            {
                SetStatus(reason);
                return;
            }

            SaveGameDirectory(textBox1.Text);
            var backupStatus = CreateBackup(textBox1.Text);
            SetStatus($"Opening unit editor... {backupStatus}");
            LoadProfileAndShow(textBox1.Text);
        }

        private void ShowPanel(Panel panelToShow)
        {
            landingPanel.Visible = false;
            unitPanel.Visible = false;

            panelToShow.Visible = true;
            panelToShow.BringToFront();
        }

        private void ConfigureFactionFilter()
        {
            factionFilterCombo.Items.Clear();
            factionFilterCombo.Items.Add(new FactionFilterOption { Label = "All UserFactionTypes", Value = null });
            factionFilterCombo.Items.Add(new FactionFilterOption { Label = "Axis (UserFactionType = 2)", Value = 2 });
            factionFilterCombo.Items.Add(new FactionFilterOption { Label = "Soviet (UserFactionType = 1)", Value = 1 });
            factionFilterCombo.Items.Add(new FactionFilterOption { Label = "Allied (UserFactionType = 0)", Value = 0 });
            factionFilterCombo.SelectedIndex = 0;
        }

        private void ConfigureUnitsGrid()
        {
            unitsDataGridView.AutoGenerateColumns = false;
            unitsDataGridView.Columns.Clear();

            unitsDataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "idOnServer",
                HeaderText = "UnitId",
                Width = 70
            });

            unitsDataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "id",
                HeaderText = "Unit Name",
                Width = 260
            });

            unitsDataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "exp",
                HeaderText = "Exp",
                Width = 80
            });

            unitsDataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "expLvl",
                HeaderText = "Exp Lvl",
                Width = 80
            });

            unitsDataGridView.CellEndEdit += (s, e) => SyncJsonFromMap();
        }

        private IEnumerable<Db.UserDataRecord> FilterRecordsByFaction()
        {
            var filtered = loadedRecords ?? new List<Db.UserDataRecord>();
            var option = factionFilterCombo.SelectedItem as FactionFilterOption;

            if (option?.Value != null)
            {
                filtered = filtered.Where(r => r.UserFactionType == option.Value).ToList();
            }

            return filtered;
        }

        private void RebindRecordList(Db.UserDataRecord toSelect = null)
        {
            suppressRecordListEvent = true;

            var filtered = FilterRecordsByFaction().ToList();
            listBoxRecords.DataSource = null;
            listBoxRecords.DataSource = filtered;
            listBoxRecords.DisplayMember = "DisplayName";

            if (toSelect != null && filtered.Contains(toSelect))
            {
                listBoxRecords.SelectedItem = toSelect;
            }
            else if (filtered.Any())
            {
                listBoxRecords.SelectedIndex = 0;
            }

            suppressRecordListEvent = false;
        }

        private void LoadProfileAndShow(string directory)
        {
            if (!Db.TryLoadAllUserJson(directory, out var records, out var message))
            {
                SetStatus($"Failed to load profile: {message}");
                return;
            }

            loadedRecords = (records ?? new List<Db.UserDataRecord>())
                                .Where(r => r.CollectionName == "2")
                                .ToList();

            if (loadedRecords.Count == 0 && records != null)
            {
                loadedRecords = records;
            }

            RebindRecordList();

            var record = listBoxRecords.SelectedItem as Db.UserDataRecord ?? loadedRecords.FirstOrDefault();
            if (record == null)
            {
                SetStatus("No matching documents to edit.");
                return;
            }

            ShowRecord(record);
            listBoxRecords.SelectedItem = record;

            ShowPanel(unitPanel);
            SetStatus(message);
        }

        private void ShowRecord(Db.UserDataRecord record)
        {
            currentRecord = record;

            var factionLabel = record.UserFactionType.HasValue
                ? $" | Faction: {record.UserFactionType}"
                : string.Empty;

            profileSourceLabel.Text = $"Database: {Path.GetFileName(record.DatabasePath)} | Collection: {record.CollectionName} | Id: {record.DocumentId}{factionLabel}";

            var pretty = GameData.PrettyPrintJson(record.JsonPayload);
            jsonEditorTextBox.Text = pretty;
            UpdateSummary(pretty);
            LoadUserMap(pretty);
        }


        private void button1_Click(object sender, EventArgs e)
        {
            BrowseForGameDirectory();
        }

        private void BrowseForGameDirectory()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select your Blitzkrieg 3 install directory";
                dialog.ShowNewFolderButton = false;

                if (!string.IsNullOrWhiteSpace(textBox1.Text) && Directory.Exists(textBox1.Text))
                {
                    dialog.SelectedPath = textBox1.Text;
                }

                var result = dialog.ShowDialog(this);
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    if (IsValidGameDirectory(dialog.SelectedPath, out var reason))
                    {
                        textBox1.Text = dialog.SelectedPath;
                        SaveGameDirectory(dialog.SelectedPath);
                        SetStatus($"Selected directory: {dialog.SelectedPath}");
                    }
                    else
                    {
                        textBox1.Text = dialog.SelectedPath;
                        SetStatus($"Invalid directory: {reason}");
                    }
                }
                else
                {
                    SetStatus("Browse canceled.");
                }
            }
        }

        private bool IsValidGameDirectory(string path, out string reason)
        {
            reason = DefaultStatus;

            if (string.IsNullOrWhiteSpace(path))
            {
                reason = "Please enter or browse to your Blitzkrieg 3 directory.";
                return false;
            }

            if (!Directory.Exists(path))
            {
                reason = "Directory does not exist.";
                return false;
            }

            var launcherPath = Path.Combine(path, "Launcher.exe");
            if (!File.Exists(launcherPath))
            {
                reason = "Launcher.exe not found in the selected directory.";
                return false;
            }

            var dataPath = Path.Combine(path, "Data");
            if (!Directory.Exists(dataPath))
            {
                reason = "Data folder not found in the selected directory.";
                return false;
            }

            var userFolderExists = Directory.EnumerateDirectories(dataPath, "User_*", SearchOption.TopDirectoryOnly).Any();
            if (!userFolderExists)
            {
                reason = "No Data/User_* folder found.";
                return false;
            }

            reason = "Directory looks good.";
            return true;
        }

        private void SetStatus(string message)
        {
            label1.Text = message;
        }

        private void UpdateSummary(string json)
        {
            if (GameData.TryParseUserProfile(json, out var profile, out var error))
            {
                currentProfile = profile;
                summaryTextBox.Text = GameData.BuildUserProfileSummary(profile);
            }
            else if (GameData.TryParseUserMap(json, out var map, out var mapError))
            {
                currentProfile = null;
                summaryTextBox.Text = GameData.BuildUserMapSummary(map);
            }
            else
            {
                currentProfile = null;
                summaryTextBox.Text = $"Could not parse profile JSON: {error ?? mapError}";
            }
        }

        private void LoadUserMap(string json)
        {
            if (GameData.TryParseUserMap(json, out var map, out var error))
            {
                currentUserMap = map;

                if (currentRecord != null && !currentRecord.UserFactionType.HasValue && map.UserFactionType.HasValue)
                {
                    currentRecord.UserFactionType = map.UserFactionType;
                    RebindRecordList(currentRecord);
                }

                BindUnits();
                UpdateNextIdLabel();
            }
            else
            {
                currentUserMap = null;
                unitBinding = new BindingList<GameData.Unit>();
                unitsDataGridView.DataSource = unitBinding;
                nextIdLabel.Text = $"Units unavailable: {error}";
            }
        }

        private void BindUnits()
        {
            var units = currentUserMap?.Units ?? new List<GameData.Unit>();
            if (currentUserMap != null && currentUserMap.Units == null)
            {
                currentUserMap.Units = units;
            }

            unitBinding = new BindingList<GameData.Unit>(units);
            unitsDataGridView.DataSource = unitBinding;
        }

        private void UpdateNextIdLabel()
        {
            var nextId = GetNextAvailableId();
            nextIdLabel.Text = nextId > 0 ? $"Next unit id: {nextId}" : "Next unit id: n/a";
        }

        private int GetNextAvailableId()
        {
            if (currentUserMap == null)
            {
                return -1;
            }

            var usedIds = new List<int>();
            if (currentUserMap.Units != null)
            {
                usedIds.AddRange(currentUserMap.Units.Select(u => u.idOnServer));
            }

            if (currentUserMap.SupportsReserve != null)
            {
                usedIds.AddRange(currentUserMap.SupportsReserve.Select(u => u.idOnServer));
            }

            var next = currentUserMap.NextId ?? (usedIds.Any() ? usedIds.Max() + 1 : 1);
            if (next < 1) next = 1;

            while (usedIds.Contains(next))
            {
                next++;
            }

            return next;
        }

        private string[] GetUnitPoolForFaction(int? factionType)
        {
            switch (factionType)
            {
                case 0:
                    return GameData.units_ald;
                case 1:
                    return GameData.units_sov;
                case 2:
                    return GameData.units_ger;
                default:
                    return allUnits;
            }
        }

        private string SuggestUnitId()
        {
            var pool = allUnits;
            var existing = new HashSet<string>(unitBinding.Select(u => u.id ?? string.Empty));
            var candidate = pool.FirstOrDefault(id => !existing.Contains(id));
            return candidate ?? "new_unit";
        }

        private bool EnsureMapLoaded()
        {
            if (currentUserMap == null)
            {
                MessageBox.Show("Units can only be edited for records that contain map data (collection \"2\").", "Units", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            return true;
        }

        private bool TryEditUnit(GameData.Unit seed, out GameData.Unit edited)
        {
            edited = null;
            EnsureUnitDefaults(seed);

            using (var dialog = new Form())
            {
                dialog.Text = "Unit Editor";
                dialog.Width = 420;
                dialog.Height = 360;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;

                var unitLabel = new Label { Text = "Unit id", Left = 10, Top = 15, AutoSize = true };
                var unitCombo = new ComboBox
                {
                    Left = 100,
                    Top = 10,
                    Width = 280,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                unitCombo.Items.AddRange(allUnits);
                var existingIndex = Array.IndexOf(allUnits, seed.id);
                unitCombo.SelectedIndex = existingIndex >= 0 ? existingIndex : 0;

                var idLabel = new Label { Text = "idOnServer", Left = 10, Top = 50, AutoSize = true };
                var idNumeric = new NumericUpDown
                {
                    Left = 100,
                    Top = 45,
                    Width = 120,
                    Minimum = 1,
                    Maximum = 999999,
                    Value = seed.idOnServer > 0 ? seed.idOnServer : GetNextAvailableId()
                };

                var expLabel = new Label { Text = "Exp", Left = 10, Top = 80, AutoSize = true };
                var expNumeric = new NumericUpDown
                {
                    Left = 100,
                    Top = 75,
                    Width = 120,
                    Minimum = 0,
                    Maximum = decimal.MaxValue,
                    DecimalPlaces = 3,
                    Increment = 0.1m,
                    Value = (decimal)seed.exp
                };

                var expLvlLabel = new Label { Text = "Exp Level", Left = 10, Top = 110, AutoSize = true };
                var expLvlNumeric = new NumericUpDown
                {
                    Left = 100,
                    Top = 105,
                    Width = 120,
                    Minimum = 0,
                    Maximum = 10,
                    Value = seed.expLvl
                };

                var mapLabel = new Label { Text = "unitOnMaps (JSON, optional)", Left = 10, Top = 140, AutoSize = true };
                var mapsText = new TextBox
                {
                    Left = 10,
                    Top = 160,
                    Width = 370,
                    Height = 110,
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    WordWrap = false
                };

                try
                {
                    mapsText.Text = JsonSerializer.Serialize(seed.unitOnMaps ?? new GameData.UnitOnMaps(), new JsonSerializerOptions { WriteIndented = true });
                }
                catch
                {
                    mapsText.Text = string.Empty;
                }

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Left = 220,
                    Width = 75,
                    Top = 280
                };
                var cancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Left = 305,
                    Width = 75,
                    Top = 280
                };

                dialog.Controls.AddRange(new Control[]
                {
                    unitLabel, unitCombo,
                    idLabel, idNumeric,
                    expLabel, expNumeric,
                    expLvlLabel, expLvlNumeric,
                    mapLabel, mapsText,
                    okButton, cancelButton
                });

                dialog.AcceptButton = okButton;
                dialog.CancelButton = cancelButton;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return false;
                }

                var chosenId = unitCombo.SelectedItem?.ToString() ?? seed.id;
                var unitOnMaps = seed.unitOnMaps ?? new GameData.UnitOnMaps();

                if (!string.IsNullOrWhiteSpace(mapsText.Text))
                {
                    try
                    {
                        unitOnMaps = JsonSerializer.Deserialize<GameData.UnitOnMaps>(mapsText.Text, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"unitOnMaps JSON invalid: {ex.Message}", "Unit editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        unitOnMaps = seed.unitOnMaps ?? new GameData.UnitOnMaps();
                    }
                }

                edited = new GameData.Unit
                {
                    id = chosenId,
                    idOnServer = (int)idNumeric.Value,
                    exp = (double)expNumeric.Value,
                    expLvl = (int)expLvlNumeric.Value,
                    unitOnMaps = unitOnMaps
                };

                EnsureUnitDefaults(edited);
                return true;
            }
        }

        private static GameData.Unit CloneUnit(GameData.Unit unit)
        {
            var json = JsonSerializer.Serialize(unit);
            return JsonSerializer.Deserialize<GameData.Unit>(json);
        }

        private void EnsureUnitDefaults(GameData.Unit unit)
        {
            if (unit == null) return;

            unit.unitOnMaps = unit.unitOnMaps ?? new GameData.UnitOnMaps();
            unit.unitOnMaps.Unknown = unit.unitOnMaps.Unknown ?? new GameData.MapData();
            unit.unitOnMaps.Early = unit.unitOnMaps.Early ?? new GameData.MapData();
            unit.unitOnMaps.Middle = unit.unitOnMaps.Middle ?? new GameData.MapData();
            unit.unitOnMaps.Late = unit.unitOnMaps.Late ?? new GameData.MapData();
        }

        private void SyncJsonFromMap()
        {
            if (currentUserMap == null) return;

            currentUserMap.NextId = GetNextAvailableId();
            var pretty = GameData.SerializeUserMap(currentUserMap, pretty: true);
            jsonEditorTextBox.Text = pretty;
            UpdateSummary(pretty);
            UpdateNextIdLabel();
        }

        private bool IsDuplicateUnitId(int idOnServer, GameData.Unit ignoreUnit = null)
        {
            return unitBinding.Any(u => u != ignoreUnit && u.idOnServer == idOnServer);
        }

        private GameData.Unit GetSelectedUnit()
        {
            if (unitsDataGridView.CurrentRow == null)
            {
                return null;
            }

            return unitsDataGridView.CurrentRow.DataBoundItem as GameData.Unit;
        }

        private void LoadSavedGameDirectory()
        {
            var saved = Db.LoadGameDirectory();
            if (!string.IsNullOrWhiteSpace(saved) && Directory.Exists(saved))
            {
                textBox1.Text = saved;
                SetStatus($"Loaded saved directory: {saved}");
            }
            else
            {
                SetStatus(DefaultStatus);
            }
        }

        private void SaveGameDirectory(string path)
        {
            Db.SaveGameDirectory(path);
        }

        private string CreateBackup(string gameDirectory)
        {
            if (Db.TryBackupUserDatabase(gameDirectory, out var backupPath, out var message))
            {
                return $"Backup created ({Path.GetFileName(backupPath)})";
            }

            return message ?? "No backup created.";
        }

        private void reloadButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                SetStatus("No directory selected.");
                return;
            }

            LoadProfileAndShow(textBox1.Text);
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (currentRecord == null)
            {
                SetStatus("No profile loaded.");
                return;
            }

            var editedJson = jsonEditorTextBox.Text;
            if (!GameData.TryNormalizeJson(editedJson, out var normalized, out var error))
            {
                SetStatus($"Invalid JSON: {error}");
                return;
            }

            if (!Db.TrySaveUserJson(currentRecord, normalized, out var message))
            {
                SetStatus(message);
                return;
            }

            currentRecord.JsonPayload = normalized;
            currentRecord.UserFactionType = GameData.TryExtractUserFactionType(normalized);
            jsonEditorTextBox.Text = GameData.PrettyPrintJson(normalized);
            UpdateSummary(normalized);
            LoadUserMap(normalized);
            RebindRecordList(currentRecord);
            SetStatus(message);
        }

        private void exportJsonButton_Click(object sender, EventArgs e)
        {
            if (currentRecord == null)
            {
                MessageBox.Show("No profile loaded.", "Export JSON", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Prefer the text box (user-selected) directory; fall back to inferring from the loaded database path.
            var gameDir = textBox1.Text;
            if (string.IsNullOrWhiteSpace(gameDir) && !string.IsNullOrWhiteSpace(currentRecord?.DatabasePath))
            {
                var directory = Path.GetDirectoryName(currentRecord.DatabasePath);
                gameDir = Directory.GetParent(directory)?.Parent?.FullName;
            }

            if (Db.TryExportDatabaseToJson(gameDir, out var exportPath, out var message))
            {
                MessageBox.Show(message, "Export JSON", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(message ?? "Export failed.", "Export JSON", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void listBoxRecords_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressRecordListEvent)
            {
                return;
            }

            var selected = listBoxRecords.SelectedItem as Db.UserDataRecord;
            if (selected != null)
            {
                ShowRecord(selected);
            }
        }

        private void factionFilterCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            RebindRecordList(currentRecord);
        }

        private void addUnitButton_Click(object sender, EventArgs e)
        {
            if (!EnsureMapLoaded())
            {
                return;
            }

            var newUnit = new GameData.Unit
            {
                idOnServer = GetNextAvailableId(),
                id = SuggestUnitId(),
                exp = 0,
                expLvl = 0,
                unitOnMaps = new GameData.UnitOnMaps()
            };

            if (!TryEditUnit(newUnit, out var edited))
            {
                return;
            }

            if (IsDuplicateUnitId(edited.idOnServer))
            {
                MessageBox.Show("Unit idOnServer must be unique.", "Add unit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            unitBinding.Add(edited);
            currentUserMap.NextId = Math.Max(currentUserMap.NextId ?? 0, edited.idOnServer + 1);
            SyncJsonFromMap();
        }

        private void editUnitButton_Click(object sender, EventArgs e)
        {
            if (!EnsureMapLoaded())
            {
                return;
            }

            var selected = GetSelectedUnit();
            if (selected == null)
            {
                MessageBox.Show("Select a unit to edit.", "Edit unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!TryEditUnit(CloneUnit(selected), out var edited))
            {
                return;
            }

            if (IsDuplicateUnitId(edited.idOnServer, selected))
            {
                MessageBox.Show("Unit idOnServer must be unique.", "Edit unit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selected.idOnServer = edited.idOnServer;
            selected.id = edited.id;
            selected.exp = edited.exp;
            selected.expLvl = edited.expLvl;
            selected.unitOnMaps = edited.unitOnMaps;
            SyncJsonFromMap();
        }

        private void deleteUnitButton_Click(object sender, EventArgs e)
        {
            if (!EnsureMapLoaded())
            {
                return;
            }

            var selected = GetSelectedUnit();
            if (selected == null)
            {
                MessageBox.Show("Select a unit to delete.", "Delete unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"Delete unit {selected.idOnServer} ({selected.id})?", "Delete unit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                unitBinding.Remove(selected);
                SyncJsonFromMap();
            }
        }

        private void syncUnitsButton_Click(object sender, EventArgs e)
        {
            LoadUserMap(jsonEditorTextBox.Text);
            SetStatus("Units refreshed from JSON editor.");
        }

        private void saveAsButton_Click(object sender, EventArgs e)
        {
            if (currentRecord == null)
            {
                SetStatus("No profile loaded.");
                return;
            }

            if (!GameData.TryNormalizeJson(jsonEditorTextBox.Text, out var normalized, out var error))
            {
                SetStatus($"Invalid JSON: {error}");
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "LiteDB files|*.*";
                dialog.FileName = Path.GetFileName(currentRecord.DatabasePath);
                dialog.Title = "Save patched LiteDB as...";

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (Db.TrySaveUserJsonAs(currentRecord, normalized, dialog.FileName, out var message))
                    {
                        currentRecord.DatabasePath = dialog.FileName;
                        currentRecord.JsonPayload = normalized;
                        currentRecord.UserFactionType = GameData.TryExtractUserFactionType(normalized);
                        ShowRecord(currentRecord);
                        SetStatus(message);
                    }
                    else
                    {
                        SetStatus(message ?? "Save As failed.");
                    }
                }
            }
        }

        private void backButton_Click(object sender, EventArgs e)
        {
            SetStatus(DefaultStatus);
            ShowPanel(landingPanel);
        }
    }
}
