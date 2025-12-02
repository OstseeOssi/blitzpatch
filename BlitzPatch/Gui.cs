using System;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text.Json;
using System.Text;

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
        private static readonly Random Randomizer = new Random();
        private static readonly IReadOnlyList<GameData.UserProgress.PvEMission> CanonicalMissions = BuildCanonicalMissions();

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

        private void landingPatchButton_Click(object sender, EventArgs e)
        {
            if (!IsValidGameDirectory(textBox1.Text, out var reason))
            {
                SetStatus(reason);
                return;
            }

            SaveGameDirectory(textBox1.Text);
            var backupStatus = CreateBackup(textBox1.Text);

            if (!TryPatchMissions(textBox1.Text, out var patchStatus))
            {
                SetStatus($"{patchStatus} ({backupStatus})");
                return;
            }

            if (!TryEnsureFactionsInMaps(textBox1.Text, out var mapStatus))
            {
                SetStatus($"{mapStatus} ({backupStatus})");
                return;
            }

            SetStatus($"{patchStatus}; {mapStatus} ({backupStatus})");
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

        private int? GuessFactionForUnit(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId))
            {
                return null;
            }

            if (GameData.units_ald.Contains(unitId))
            {
                return 0;
            }

            if (GameData.units_sov.Contains(unitId))
            {
                return 1;
            }

            if (GameData.units_ger.Contains(unitId))
            {
                return 2;
            }

            return null;
        }

        private int NextAvailableIdFrom(int start, HashSet<int> usedIds)
        {
            var candidate = Math.Max(start, 1);
            while (usedIds.Contains(candidate))
            {
                candidate++;
            }

            return candidate;
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
            return TryEditUnit(seed, out edited, out _, out _, false);
        }

        private bool TryEditUnit(GameData.Unit seed, out GameData.Unit edited, out int quantity, out bool randomizePositionsSelected, bool allowMultipleInstances)
        {
            edited = null;
            quantity = 1;
            randomizePositionsSelected = false;
            EnsureUnitDefaults(seed);

            using (var dialog = new Form())
            {
                dialog.Text = "Unit Editor";
                dialog.Width = 420;
                dialog.Height = allowMultipleInstances ? 440 : 400;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;

                var techTreeLabel = new Label { Text = "Tech tree", Left = 10, Top = 15, AutoSize = true };
                var techTreeCombo = new ComboBox
                {
                    Left = 100,
                    Top = 10,
                    Width = 280,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                techTreeCombo.Items.Add(new FactionFilterOption { Label = "All", Value = null });
                techTreeCombo.Items.Add(new FactionFilterOption { Label = "Allied", Value = 0 });
                techTreeCombo.Items.Add(new FactionFilterOption { Label = "Soviet", Value = 1 });
                techTreeCombo.Items.Add(new FactionFilterOption { Label = "Axis", Value = 2 });

                var unitLabel = new Label { Text = "Unit id", Left = 10, Top = 50, AutoSize = true };
                var unitCombo = new ComboBox
                {
                    Left = 100,
                    Top = 45,
                    Width = 280,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                void RefreshUnitCombo()
                {
                    var selectedFaction = (techTreeCombo.SelectedItem as FactionFilterOption)?.Value;
                    var pool = GetUnitPoolForFaction(selectedFaction);
                    var previousSelection = unitCombo.SelectedItem?.ToString() ?? seed.id;
                    unitCombo.BeginUpdate();
                    unitCombo.Items.Clear();
                    unitCombo.Items.AddRange(pool);
                    var existingIndex = Array.IndexOf(pool, previousSelection);
                    unitCombo.SelectedIndex = existingIndex >= 0 ? existingIndex : 0;
                    unitCombo.EndUpdate();
                }

                var inferredFaction = GuessFactionForUnit(seed.id) ?? currentUserMap?.UserFactionType;
                var preferredFactionOption = techTreeCombo.Items.Cast<object>()
                    .OfType<FactionFilterOption>()
                    .FirstOrDefault(o => o.Value == inferredFaction);
                techTreeCombo.SelectedItem = preferredFactionOption ?? techTreeCombo.Items[0];
                techTreeCombo.SelectedIndexChanged += (s, e) => RefreshUnitCombo();
                RefreshUnitCombo();

                var idLabel = new Label { Text = "idOnServer", Left = 10, Top = 85, AutoSize = true };
                var idNumeric = new NumericUpDown
                {
                    Left = 100,
                    Top = 80,
                    Width = 120,
                    Minimum = 1,
                    Maximum = 999999,
                    Value = seed.idOnServer > 0 ? seed.idOnServer : GetNextAvailableId()
                };

                var quantityLabelTop = 115;
                var expTop = allowMultipleInstances ? 150 : 115;
                var expLabel = new Label { Text = "Exp", Left = 10, Top = expTop, AutoSize = true };
                var expNumeric = new NumericUpDown
                {
                    Left = 100,
                    Top = expTop - 5,
                    Width = 120,
                    Minimum = 0,
                    Maximum = decimal.MaxValue,
                    DecimalPlaces = 3,
                    Increment = 0.1m,
                    Value = (decimal)seed.exp
                };

                NumericUpDown quantityNumeric = null;
                if (allowMultipleInstances)
                {
                    var quantityLabel = new Label { Text = "Quantity", Left = 10, Top = quantityLabelTop, AutoSize = true };
                    quantityNumeric = new NumericUpDown
                    {
                        Left = 100,
                        Top = quantityLabelTop - 5,
                        Width = 120,
                        Minimum = 1,
                        Maximum = 999,
                        Value = 1
                    };

                    dialog.Controls.Add(quantityLabel);
                    dialog.Controls.Add(quantityNumeric);
                }

                var expLvlTop = expTop + 30;
                var expLvlLabel = new Label { Text = "Exp Level", Left = 10, Top = expLvlTop, AutoSize = true };
                var expLvlNumeric = new NumericUpDown
                {
                    Left = 100,
                    Top = expLvlTop - 5,
                    Width = 120,
                    Minimum = 0,
                    Maximum = 10,
                    Value = seed.expLvl
                };

                var mapLabelTop = expLvlTop + 30;
                var mapLabel = new Label { Text = "unitOnMaps (JSON, optional)", Left = 10, Top = mapLabelTop, AutoSize = true };
                var randomizePositions = new CheckBox
                {
                    Left = 10,
                    Top = mapLabelTop + 20,
                    Text = "Randomize Early/Middle/Late positions",
                    AutoSize = true
                };

                var mapsTextTop = randomizePositions.Top + 25;
                var mapsText = new TextBox
                {
                    Left = 10,
                    Top = mapsTextTop,
                    Width = 370,
                    Height = 130,
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

                var buttonsTop = mapsText.Top + mapsText.Height + 15;
                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Left = 220,
                    Width = 75,
                    Top = buttonsTop
                };
                var cancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Left = 305,
                    Width = 75,
                    Top = buttonsTop
                };

                dialog.Controls.AddRange(new Control[]
                {
                    techTreeLabel, techTreeCombo,
                    unitLabel, unitCombo,
                    idLabel, idNumeric,
                    expLabel, expNumeric,
                    expLvlLabel, expLvlNumeric,
                    mapLabel, randomizePositions, mapsText,
                    okButton, cancelButton
                });

                dialog.AcceptButton = okButton;
                dialog.CancelButton = cancelButton;
                dialog.Height = buttonsTop + 110;

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

                if (randomizePositions.Checked)
                {
                    unitOnMaps = RandomizeUnitOnMaps(unitOnMaps);
                    randomizePositionsSelected = true;
                }

                edited = new GameData.Unit
                {
                    id = chosenId,
                    idOnServer = (int)idNumeric.Value,
                    exp = (double)expNumeric.Value,
                    expLvl = (int)expLvlNumeric.Value,
                    unitOnMaps = unitOnMaps
                };

                quantity = allowMultipleInstances && quantityNumeric != null ? (int)quantityNumeric.Value : 1;
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

        private GameData.UnitOnMaps RandomizeUnitOnMaps(GameData.UnitOnMaps unitOnMaps)
        {
            if (unitOnMaps == null)
            {
                unitOnMaps = new GameData.UnitOnMaps();
            }

            unitOnMaps.Early = RandomizeMapData(new GameData.MapData());
            unitOnMaps.Middle = RandomizeMapData(new GameData.MapData());
            unitOnMaps.Late = RandomizeMapData(new GameData.MapData());
            return unitOnMaps;
        }

        private GameData.MapData RandomizeMapData(GameData.MapData mapData)
        {
            if (mapData == null)
            {
                mapData = new GameData.MapData();
            }

            mapData.Pos = mapData.Pos ?? new GameData.Position();

            mapData.Pos.X = NextPositionCoordinate();
            mapData.Pos.Y = NextHeightCoordinate();
            mapData.Pos.Z = NextPositionCoordinate();
            mapData.Angle = NextAngle();

            mapData.Parent = -1;
            mapData.Modes = 0;
            return mapData;
        }

        private double NextPositionCoordinate() => Math.Round(Randomizer.NextDouble() * 500, 2);
        private double NextHeightCoordinate() => Math.Round(40 + Randomizer.NextDouble() * 40, 2);
        private double NextAngle() => Math.Round(Randomizer.NextDouble() * 360, 2);

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

        private static IReadOnlyList<GameData.UserProgress.PvEMission> BuildCanonicalMissions()
        {
            var missions = new List<GameData.UserProgress.PvEMission>();

            void AddRange(string campaignId, IEnumerable<string> missionIds)
            {
                foreach (var id in missionIds)
                {
                    missions.Add(new GameData.UserProgress.PvEMission
                    {
                        MissionId = id,
                        CampaignId = campaignId,
                        IsMainObjectivesCompleted = true,
                        IsOptionalObjectivesCompleted = true,
                        IsChallengesCompleted = true,
                        MaxAchievedStars = 3
                    });
                }
            }

            AddRange(GameData.campaign_ald, GameData.mission_ald);
            AddRange(GameData.campaign_sov, GameData.mission_sov);
            AddRange(GameData.campaign_ger, GameData.mission_ger);

            return missions;
        }

        private bool TryPatchMissions(string gameDirectory, out string status)
        {
            status = null;

            if (!Db.TryLoadAllUserJson(gameDirectory, out var records, out var loadMessage))
            {
                status = loadMessage ?? "Could not load profile data.";
                return false;
            }

            if (records == null || records.Count == 0)
            {
                status = "No documents found to patch maps.";
                return false;
            }

            var progressRecords = (records ?? new List<Db.UserDataRecord>())
                .Where(r => r.CollectionName == "1")
                .ToList();

            if (progressRecords.Count == 0)
            {
                status = "No collection \"1\" (UserProgress) documents found to patch.";
                return false;
            }

            var patchedCount = 0;

            foreach (var record in progressRecords)
            {
                if (!TryPatchMissionsInJson(record.JsonPayload, out var patchedJson, out var error))
                {
                    status = $"Failed to patch missions for record {record.DisplayName}: {error}";
                    return false;
                }

                if (!Db.TrySaveUserJson(record, patchedJson, out var saveMessage))
                {
                    status = saveMessage ?? $"Failed to save patched record {record.DisplayName}.";
                    return false;
                }

                record.JsonPayload = patchedJson;
                patchedCount++;
            }

            status = $"Patched PvE missions for {patchedCount} profile(s).";
            return patchedCount > 0;
        }

        private bool TryEnsureFactionsInMaps(string gameDirectory, out string status)
        {
            status = null;

            if (!Db.TryLoadAllUserJson(gameDirectory, out var records, out var loadMessage))
            {
                status = loadMessage ?? "Could not load profile data.";
                return false;
            }

            var mapRecords = (records ?? new List<Db.UserDataRecord>())
                .Where(r => r.CollectionName == "2")
                .ToList();

            var existingFactions = new HashSet<int>(mapRecords.Where(r => r.UserFactionType.HasValue).Select(r => r.UserFactionType.Value));
            var missing = new List<int>();
            foreach (var faction in new[] { 0, 1, 2 })
            {
                if (!existingFactions.Contains(faction))
                {
                    missing.Add(faction);
                }
            }

            if (missing.Count == 0)
            {
                status = "All faction maps already present.";
                return true;
            }

            var inserted = 0;
            foreach (var faction in missing)
            {
                var placeholderJson = BuildPlaceholderMapJson(faction);
                if (!Db.TryInsertUserJson(mapRecords.FirstOrDefault()?.DatabasePath ?? records.First().DatabasePath, "2", placeholderJson, faction, out _, out var insertMessage))
                {
                    status = insertMessage ?? $"Failed to insert map for faction {faction}.";
                    return false;
                }

                inserted++;
            }

            status = $"Added {inserted} missing faction map(s).";
            return true;
        }

        private bool TryPatchMissionsInJson(string json, out string patchedJson, out string error)
        {
            patchedJson = null;
            error = null;

            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    var existingMissions = ExtractExistingMissions(doc.RootElement);
                    var mergedMissions = MergeMissionLists(existingMissions);

                    using (var stream = new MemoryStream())
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                    {
                        WritePatchedProfile(doc.RootElement, mergedMissions, writer);
                        writer.Flush();
                        patchedJson = Encoding.UTF8.GetString(stream.ToArray());
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private List<GameData.UserProgress.PvEMission> ExtractExistingMissions(JsonElement root)
        {
            var missions = new List<GameData.UserProgress.PvEMission>();

            if (root.TryGetProperty("PvEMissionProgress", out var missionArray) && missionArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in missionArray.EnumerateArray())
                {
                    try
                    {
                        var mission = JsonSerializer.Deserialize<GameData.UserProgress.PvEMission>(element.GetRawText(), new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (mission != null)
                        {
                            missions.Add(mission);
                        }
                    }
                    catch
                    {
                        // Ignore malformed mission rows; they will be replaced by canonical ones.
                    }
                }
            }

            return missions;
        }

        private List<GameData.UserProgress.PvEMission> MergeMissionLists(List<GameData.UserProgress.PvEMission> existing)
        {
            var merged = new List<GameData.UserProgress.PvEMission>();
            var existingById = new Dictionary<string, GameData.UserProgress.PvEMission>(StringComparer.OrdinalIgnoreCase);

            foreach (var mission in existing ?? Enumerable.Empty<GameData.UserProgress.PvEMission>())
            {
                if (string.IsNullOrWhiteSpace(mission?.MissionId))
                {
                    continue;
                }

                var normalized = NormalizeMission(mission);
                if (existingById.ContainsKey(normalized.MissionId))
                {
                    continue;
                }

                existingById[normalized.MissionId] = normalized;
                merged.Add(normalized);
            }

            foreach (var canonical in CanonicalMissions)
            {
                if (existingById.TryGetValue(canonical.MissionId, out var found))
                {
                    found.IsMainObjectivesCompleted = true;
                    found.IsOptionalObjectivesCompleted = true;
                    found.IsChallengesCompleted = true;
                    found.MaxAchievedStars = Math.Max(found.MaxAchievedStars, canonical.MaxAchievedStars);

                    if (string.IsNullOrWhiteSpace(found.CampaignId))
                    {
                        found.CampaignId = canonical.CampaignId;
                    }
                }
                else
                {
                    merged.Add(CloneMission(canonical));
                }
            }

            return merged;
        }

        private GameData.UserProgress.PvEMission NormalizeMission(GameData.UserProgress.PvEMission mission)
        {
            if (mission == null)
            {
                mission = new GameData.UserProgress.PvEMission();
            }

            mission.IsMainObjectivesCompleted = true;
            mission.IsOptionalObjectivesCompleted = true;
            mission.IsChallengesCompleted = true;
            mission.MaxAchievedStars = Math.Max(mission.MaxAchievedStars, 3);

            return mission;
        }

        private GameData.UserProgress.PvEMission CloneMission(GameData.UserProgress.PvEMission mission)
        {
            if (mission == null)
            {
                return new GameData.UserProgress.PvEMission();
            }

            return new GameData.UserProgress.PvEMission
            {
                MissionId = mission.MissionId,
                CampaignId = mission.CampaignId,
                IsMainObjectivesCompleted = mission.IsMainObjectivesCompleted,
                IsOptionalObjectivesCompleted = mission.IsOptionalObjectivesCompleted,
                IsChallengesCompleted = mission.IsChallengesCompleted,
                MaxAchievedStars = mission.MaxAchievedStars
            };
        }

        private void WritePatchedProfile(JsonElement root, List<GameData.UserProgress.PvEMission> missions, Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            var wroteMissions = false;

            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals("PvEMissionProgress"))
                {
                    writer.WritePropertyName(property.Name);
                    WriteMissionArray(missions, writer);
                    wroteMissions = true;
                }
                else
                {
                    property.WriteTo(writer);
                }
            }

            if (!wroteMissions)
            {
                writer.WritePropertyName("PvEMissionProgress");
                WriteMissionArray(missions, writer);
            }

            writer.WriteEndObject();
        }

        private void WriteMissionArray(List<GameData.UserProgress.PvEMission> missions, Utf8JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var mission in missions)
            {
                JsonSerializer.Serialize(writer, mission);
            }
            writer.WriteEndArray();
        }

        private string BuildPlaceholderMapJson(int faction)
        {
            var map = new GameData.UserMapDocument
            {
                UserFactionType = faction,
                NextId = 1,
                Units = new List<GameData.Unit>(),
                SupportsReserve = new List<GameData.Unit>()
            };

            var pool = GetUnitPoolForFaction(faction);
            var usedIds = new HashSet<int>();
            var count = Math.Min(6, pool.Length);

            for (int i = 0; i < count; i++)
            {
                var unit = new GameData.Unit
                {
                    idOnServer = NextAvailableIdFrom(i + 1, usedIds),
                    id = pool[Randomizer.Next(0, pool.Length)],
                    exp = 0,
                    expLvl = 0,
                    unitOnMaps = new GameData.UnitOnMaps()
                };

                EnsureUnitDefaults(unit);
                map.Units.Add(unit);
                usedIds.Add(unit.idOnServer);
            }

            map.NextId = NextAvailableIdFrom(map.NextId ?? 1, usedIds);
            return GameData.SerializeUserMap(map, pretty: true);
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

        private void exportLiteDbButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select LiteDB (_a) file to export";
                dialog.Filter = "LiteDB files|a_*;*.db;*.litedb;*.ldb|All files|*.*";
                dialog.CheckFileExists = true;

                if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    SetStatus("Export canceled.");
                    return;
                }

                if (Db.TryExportLiteDbFileToJson(dialog.FileName, out var exportPath, out var message))
                {
                    MessageBox.Show(message, "Export JSON", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SetStatus(message);
                }
                else
                {
                    MessageBox.Show(message ?? "Export failed.", "Export JSON", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetStatus(message ?? "Export failed.");
                }
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

            if (!TryEditUnit(newUnit, out var edited, out var quantity, out var randomizePositionsSelected, allowMultipleInstances: true))
            {
                return;
            }

            var usedIds = new HashSet<int>(unitBinding.Select(u => u.idOnServer));
            var startId = NextAvailableIdFrom(edited.idOnServer > 0 ? edited.idOnServer : GetNextAvailableId(), usedIds);
            var unitsToAdd = new List<GameData.Unit>();

            for (int i = 0; i < quantity; i++)
            {
                var idForUnit = NextAvailableIdFrom(startId, usedIds);
                var unitToAdd = CloneUnit(edited);
                unitToAdd.idOnServer = idForUnit;
                if (randomizePositionsSelected)
                {
                    unitToAdd.unitOnMaps = RandomizeUnitOnMaps(unitToAdd.unitOnMaps);
                }

                unitsToAdd.Add(unitToAdd);
                usedIds.Add(idForUnit);
                startId = idForUnit + 1;
            }

            foreach (var unit in unitsToAdd)
            {
                unitBinding.Add(unit);
            }

            currentUserMap.NextId = NextAvailableIdFrom(startId, usedIds);
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

            if (!TryEditUnit(CloneUnit(selected), out var edited, out _, out _, allowMultipleInstances: false))
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
