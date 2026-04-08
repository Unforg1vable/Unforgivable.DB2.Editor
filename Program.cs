using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DBCD;
using DBCD.Providers;
using WDBXLib;

namespace WowDb2Viewer
{
    public partial class Form1 : Form
    {
        private TextBox txtSearchId;
        private Button btnFindId;
        private ComboBox cmbTable;
        private Dictionary<int, DBCDRow> _typedStorage;
        private Button btnPatchFurbolg;
        private DataTable table;
        private DataGridView grid;
        private FilesystemDBCProvider _dbcProvider;
        private FilesystemDBDProvider _dbdProvider;
        private DBCD.DBCD _dbcd;
        private string _currentFilePath;
        private readonly string db2Folder = @"C:\Build\bin\RelWithDebInfo\dbc\ruRU";
        private readonly string defsFolder = @"C:\WoWDBDefs-master\definitions";
        private dynamic _storage;
        private string _currentTableName;
        private Button btnLoad;
        private Button btnExport;
        private Button btnSaveDb2;
        private readonly string _keyColumn = "ID";

        public Form1()
        {
            InitializeComponent();

            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            grid.CellValueChanged += Grid_CellValueChanged;
            grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grid.IsCurrentCellDirty)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
        }

        private void InitializeComponent()
        {
            btnLoad = new Button();
            btnExport = new Button();
            btnSaveDb2 = new Button();
            grid = new DataGridView();
            txtSearchId = new TextBox();
            btnFindId = new Button();
            cmbTable = new ComboBox();
            btnPatchFurbolg = new Button();

            ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
            SuspendLayout();

            // grid (создаём сначала, чтобы можно было красить)
            grid.Location = new Point(10, 50);
            grid.Name = "grid";
            grid.Size = new Size(1580, 820);
            grid.TabIndex = 3;
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Dark theme for grid
            grid.BackgroundColor = Color.Black;
            grid.DefaultCellStyle.BackColor = Color.Black;
            grid.DefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 60, 60);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.EnableHeadersVisualStyles = false;

            // Form
            ClientSize = new Size(1604, 881);
            Name = "Form1";
            Text = ".db2 Viewer (DBCD)";
            BackColor = Color.FromArgb(20, 20, 20);
            Load += Form1_Load;

            // btnLoad
            btnLoad.Location = new Point(10, 10);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(120, 30);
            btnLoad.TabIndex = 0;
            btnLoad.Text = "Load DB2";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += LoadBtn_Click;

            // btnExport
            btnExport.Location = new Point(140, 10);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(120, 30);
            btnExport.TabIndex = 1;
            btnExport.Text = "Export CSV";
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += ExportBtn_Click;

            // btnSaveDb2
            btnSaveDb2.Location = new Point(270, 10);
            btnSaveDb2.Name = "btnSaveDb2";
            btnSaveDb2.Size = new Size(120, 30);
            btnSaveDb2.TabIndex = 2;
            btnSaveDb2.Text = "Save DB2";
            btnSaveDb2.UseVisualStyleBackColor = true;
            btnSaveDb2.Click += SaveDb2Btn_Click;

            // btnPatchFurbolg
            btnPatchFurbolg.Location = new Point(400, 10);
            btnPatchFurbolg.Name = "btnPatchFurbolg";
            btnPatchFurbolg.Size = new Size(180, 30);
            btnPatchFurbolg.TabIndex = 3;
            btnPatchFurbolg.Text = "Furbolg как Human";
            btnPatchFurbolg.UseVisualStyleBackColor = true;
            btnPatchFurbolg.Click += BtnPatchFurbolg_Click;

            // cmbTable
            cmbTable.Location = new Point(600, 10);
            cmbTable.Name = "cmbTable";
            cmbTable.Size = new Size(180, 30);
            cmbTable.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTable.Items.Add("ChrRaces");
            cmbTable.Items.Add("CreatureDisplayInfo");
            cmbTable.SelectedIndex = 0;

            // txtSearchId
            txtSearchId.Location = new Point(800, 10);
            txtSearchId.Size = new Size(80, 30);
            txtSearchId.Name = "txtSearchId";
            txtSearchId.BackColor = Color.FromArgb(40, 40, 40);
            txtSearchId.ForeColor = Color.White;

            // btnFindId
            btnFindId.Location = new Point(890, 10);
            btnFindId.Size = new Size(80, 30);
            btnFindId.Text = "Find ID";
            btnFindId.Click += BtnFindId_Click;

            // Добавляем контролы
            Controls.Add(btnLoad);
            Controls.Add(btnExport);
            Controls.Add(btnSaveDb2);
            Controls.Add(btnPatchFurbolg);
            Controls.Add(cmbTable);
            Controls.Add(txtSearchId);
            Controls.Add(btnFindId);
            Controls.Add(grid);

            // Тёмные кнопки/комбо/текстбоксы
            foreach (Control c in Controls)
            {
                if (c is Button || c is ComboBox || c is TextBox)
                {
                    c.BackColor = Color.FromArgb(40, 40, 40);
                    c.ForeColor = Color.White;
                }
            }

            ((System.ComponentModel.ISupportInitialize)grid).EndInit();
            ResumeLayout(false);
        }

        // Хоткеи
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                e.SuppressKeyPress = true;
                txtSearchId.Focus();
                txtSearchId.SelectAll();
            }

            if (e.Control && e.KeyCode == Keys.S)
            {
                e.SuppressKeyPress = true;
                btnSaveDb2.PerformClick();
            }

            if (!e.Control && e.KeyCode == Keys.F1)
            {
                e.SuppressKeyPress = true;
                btnLoad.PerformClick();
            }
        }

        private void BtnFindId_Click(object sender, EventArgs e)
        {
            if (table == null || grid.Rows.Count == 0)
            {
                MessageBox.Show("No data loaded.");
                return;
            }

            if (!int.TryParse(txtSearchId.Text, out int searchId))
            {
                MessageBox.Show("Введите числовой ID.");
                return;
            }

            int idColIndex = table.Columns.IndexOf(_keyColumn);
            if (idColIndex < 0)
            {
                MessageBox.Show("Колонка ID не найдена.");
                return;
            }

            for (int i = 0; i < grid.Rows.Count; i++)
            {
                var row = grid.Rows[i];
                if (row.IsNewRow)
                    continue;

                var cellVal = row.Cells[idColIndex].Value?.ToString();
                if (int.TryParse(cellVal, out int id) && id == searchId)
                {
                    grid.ClearSelection();
                    row.Selected = true;
                    grid.CurrentCell = row.Cells[Math.Max(0, idColIndex)];
                    grid.FirstDisplayedScrollingRowIndex = i;
                    return;
                }
            }

            MessageBox.Show($"ID {searchId} не найден.");
        }

        private void SaveDb2Btn_Click(object sender, EventArgs e)
        {
            if (_storage == null || string.IsNullOrEmpty(_currentFilePath))
            {
                MessageBox.Show("No DB2 file loaded.");
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "WoW DB2 files (*.db2)|*.db2|All files (*.*)|*.*",
                DefaultExt = "db2",
                FileName = Path.GetFileNameWithoutExtension(_currentFilePath) + "_patched.db2"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                SaveDb2Real(sfd.FileName);
                MessageBox.Show("DB2 saved.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save error: {ex.Message}");
            }
        }

        private void SaveDb2Real(string newPath)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
                throw new InvalidOperationException("No DB2 file loaded.");

            File.Copy(_currentFilePath, newPath, overwrite: true);
            _currentFilePath = newPath;
        }

        private void LoadBtn_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(db2Folder))
            {
                MessageBox.Show($"DB2 folder not found:\n{db2Folder}");
                return;
            }
            if (!Directory.Exists(defsFolder))
            {
                MessageBox.Show($"Definitions folder not found:\n{defsFolder}");
                return;
            }

            var dlg = new OpenFileDialog
            {
                InitialDirectory = db2Folder,
                Filter = "WoW DB2 files (*.db2)|*.db2|All files (*.*)|*.*",
                Title = "Select WoW .db2 file"
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                table = LoadDb2ToDataTable(dlg.FileName);
                _currentFilePath = dlg.FileName;

                grid.DataSource = null;
                grid.DataSource = table;
                MessageBox.Show($"Loaded {table.Rows.Count} rows, {table.Columns.Count} columns.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Load error: {ex.Message}");
            }
        }

        private DataTable LoadDb2ToDataTable(string fullPath)
        {
            var dt = new DataTable();

            try
            {
                _dbcProvider = new FilesystemDBCProvider(Path.GetDirectoryName(fullPath));
                _dbdProvider = new FilesystemDBDProvider(defsFolder);
                _dbcd = new DBCD.DBCD(_dbcProvider, _dbdProvider);

                var physicalName = Path.GetFileNameWithoutExtension(fullPath);
                string logicalName = physicalName;
                const string suffix = "_patched";
                if (physicalName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    logicalName = physicalName.Substring(0, physicalName.Length - suffix.Length);

                if (cmbTable != null && cmbTable.SelectedItem != null)
                {
                    var selected = cmbTable.SelectedItem.ToString();
                    if (!string.IsNullOrEmpty(selected))
                        logicalName = selected;
                }

                _currentTableName = logicalName;

                _storage = _dbcd.Load(logicalName);
                _typedStorage = (Dictionary<int, DBCDRow>)_storage;
                var storage = _typedStorage;

                string[] columns;

                if (_currentTableName.Equals("ChrRaces", StringComparison.OrdinalIgnoreCase))
                {
                    columns = new[]
                    {
                        "ID","ClientPrefix","ClientFileString",
                        "Name_lang","Name_female_lang","Name_lowercase_lang","Name_female_lowercase_lang",
                        "Lore_name_lang","Lore_name_female_lang","Lore_name_lower_lang","Lore_name_lower_female_lang",
                        "LoreDescription_lang","Short_name_lang","Short_name_female_lang","Short_name_lower_lang","Short_name_lower_female_lang",
                        "Flags","FactionID","CinematicSequenceID","ResSicknessSpellID","SplashSoundID",
                        "CreateScreenFileDataID","SelectScreenFileDataID","LowResScreenFileDataID",
                        "AlteredFormStartVisualKitID","AlteredFormFinishVisualKitID","HeritageArmorAchievementID",
                        "StartingLevel","UiDisplayOrder","PlayableRaceBit","TransmogrifyDisabledSlotMask",
                        "AlteredFormCustomizeOffsetFallback","AlteredFormCustomizeRotationFallback",
                        "Field_9_1_0_38312_030","Field_9_1_0_38312_031",
                        "BaseLanguage","CreatureType","Alliance","Race_related",
                        "UnalteredVisualRaceID","DefaultClassID","NeutralRaceID",
                        "MaleModelFallbackRaceID","MaleModelFallbackSex",
                        "FemaleModelFallbackRaceID","FemaleModelFallbackSex",
                        "MaleTextureFallbackRaceID","MaleTextureFallbackSex",
                        "FemaleTextureFallbackRaceID","FemaleTextureFallbackSex",
                        "HelmetAnimScalingRaceID","UnalteredVisualCustomizationRaceID"
                    };
                }
                else if (_currentTableName.Equals("CreatureDisplayInfo", StringComparison.OrdinalIgnoreCase))
                {
                    columns = new[]
                    {
                        "ID",
                        "ModelID",
                        "CreatureModelScale",
                        "SizeClass",
                        "Gender",
                        "Flags",
                        "ExtendedDisplayInfoID",
                        "NPCSoundID",
                        "CreatureModelAlpha",
                        "BloodID",
                        "PortraitCreatureDisplayInfoID",
                        "PortraitTextureFileDataID",
                        "ObjectEffectPackageID",
                        "AnimReplacementSetID",
                        "PlayerOverrideScale",
                        "PetInstanceScale",
                        "UnarmedWeaponType",
                        "MountPoofSpellVisualKitID",
                        "DissolveEffectID",
                        "DissolveOutEffectID",
                        "CreatureModelMinLod",
                        "ConditionalCreatureModelID",
                        "MountMaxBankingAngle"
                    };
                }
                else
                {
                    columns = new[] { "ID" };
                }

                foreach (var c in columns)
                    dt.Columns.Add(c, typeof(string));

                foreach (var pair in storage)
                {
                    var row = pair.Value;
                    var dr = dt.NewRow();

                    foreach (var c in columns)
                    {
                        object val = null;
                        try { val = row[c]; }
                        catch { }
                        dr[c] = val?.ToString() ?? "";
                    }

                    dt.Rows.Add(dr);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Inner load error: {ex.Message}");
            }

            table = dt;
            return dt;
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_typedStorage == null || table == null)
                return;

            var gridLocal = (DataGridView)sender;
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var rowView = gridLocal.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (rowView == null)
                return;

            var dataRow = rowView.Row;

            if (!table.Columns.Contains(_keyColumn))
                return;

            var keyObj = dataRow[_keyColumn];
            if (keyObj == null)
                return;

            if (!int.TryParse(keyObj.ToString(), out int key))
                return;

            string columnName = gridLocal.Columns[e.ColumnIndex].Name;
            if (string.IsNullOrEmpty(columnName))
                columnName = gridLocal.Columns[e.ColumnIndex].DataPropertyName;

            if (string.IsNullOrEmpty(columnName))
                return;

            object newValueObj = dataRow[columnName];
            string newValueStr = newValueObj?.ToString() ?? "";

            try
            {
                if (!_typedStorage.TryGetValue(key, out var dbcdRow))
                    return;

                object originalValue = null;
                try
                {
                    originalValue = dbcdRow[columnName];
                }
                catch
                {
                    return;
                }

                var converted = ConvertStringToSameType(newValueStr, originalValue);
                dbcdRow[columnName] = converted;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating underlying DBCDRow: {ex.Message}");
            }
        }

        private void ExportBtn_Click(object sender, EventArgs e)
        {
            if (table == null || table.Rows.Count == 0)
            {
                MessageBox.Show("No data loaded.");
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                DefaultExt = "csv"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var sb = new StringBuilder();

                var headers = new string[table.Columns.Count];
                for (int i = 0; i < table.Columns.Count; i++)
                    headers[i] = table.Columns[i].ColumnName;
                sb.AppendLine(string.Join(";", headers));

                foreach (DataRow row in table.Rows)
                {
                    var fields = new string[table.Columns.Count];
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        var val = (row[i] ?? "").ToString();
                        val = val
                            .Replace("\\", "\\\\")
                            .Replace(";", "\\;")
                            .Replace("\n", "\\n")
                            .Replace("\r", "\\r");
                        fields[i] = val;
                    }
                    sb.AppendLine(string.Join(";", fields));
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("CSV exported.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}");
            }
        }

        private object ConvertStringToSameType(string input, object originalValue)
        {
            if (originalValue == null)
                return input;

            var t = originalValue.GetType();

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                t = Nullable.GetUnderlyingType(t);

            try
            {
                if (t == typeof(int))
                {
                    if (int.TryParse(input, out int i))
                        return i;
                    return originalValue;
                }
                if (t == typeof(uint))
                {
                    if (uint.TryParse(input, out uint u))
                        return u;
                    return originalValue;
                }
                if (t == typeof(short))
                {
                    if (short.TryParse(input, out short s))
                        return s;
                    return originalValue;
                }
                if (t == typeof(ushort))
                {
                    if (ushort.TryParse(input, out ushort us))
                        return us;
                    return originalValue;
                }
                if (t == typeof(long))
                {
                    if (long.TryParse(input, out long l))
                        return l;
                    return originalValue;
                }
                if (t == typeof(ulong))
                {
                    if (ulong.TryParse(input, out ulong ul))
                        return ul;
                    return originalValue;
                }
                if (t == typeof(float))
                {
                    if (float.TryParse(input, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float f))
                        return f;
                    return originalValue;
                }
                if (t == typeof(double))
                {
                    if (double.TryParse(input, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double d))
                        return d;
                    return originalValue;
                }
                if (t == typeof(bool))
                {
                    if (bool.TryParse(input, out bool b))
                        return b;

                    if (input == "0")
                        return false;
                    if (input == "1")
                        return true;

                    return originalValue;
                }
                if (t == typeof(byte))
                {
                    if (byte.TryParse(input, out byte byteVal))
                        return byteVal;
                    return originalValue;
                }
                if (t == typeof(sbyte))
                {
                    if (sbyte.TryParse(input, out sbyte sbyteVal))
                        return sbyteVal;
                    return originalValue;
                }

                return input;
            }
            catch
            {
                return originalValue;
            }
        }

        private void BtnPatchFurbolg_Click(object sender, EventArgs e)
        {
            if (!string.Equals(_currentTableName, "ChrRaces", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Фурболга патчим только в ChrRaces.db2.");
                return;
            }

            int humanId = 1;
            int furbolgId = 95;

            try
            {
                if (!_typedStorage.TryGetValue(humanId, out var humanRow) ||
                    !_typedStorage.TryGetValue(furbolgId, out var furbolgRow))
                {
                    MessageBox.Show("Human или Furbolg не найдены");
                    return;
                }

                foreach (DataColumn col in table.Columns)
                {
                    string colName = col.ColumnName;
                    if (colName == "ID")
                        continue;

                    try
                    {
                        object val = humanRow[colName];
                        furbolgRow[colName] = val;
                    }
                    catch
                    {
                    }
                }

                furbolgRow["ClientPrefix"] = "Fu";
                furbolgRow["ClientFileString"] = "Furbolg";

                furbolgRow["Name_lang"] = "Фурболг";
                furbolgRow["Name_female_lang"] = "Фурболг";
                furbolgRow["Name_lowercase_lang"] = "фурболг";
                furbolgRow["Name_female_lowercase_lang"] = "фурболг";

                furbolgRow["Lore_name_lang"] = "Фурболг";
                furbolgRow["Lore_name_female_lang"] = "Фурболг";
                furbolgRow["Lore_name_lower_lang"] = "фурболг";
                furbolgRow["Lore_name_lower_female_lang"] = "фурболг";

                furbolgRow["LoreDescription_lang"] =
                    "Фурболги – древний народ Азерота, сражающийся за защиту своих лесов.";

                furbolgRow["Short_name_lang"] = "Фурболг";
                furbolgRow["Short_name_female_lang"] = "Фурболг";
                furbolgRow["Short_name_lower_lang"] = "фурболг";
                furbolgRow["Short_name_lower_female_lang"] = "фурболг";

                void SetSameType(DBCDRow row, string field, object value)
                {
                    try
                    {
                        var orig = row[field];
                        var targetType = orig?.GetType() ?? typeof(int);
                        var converted = Convert.ChangeType(value, targetType, System.Globalization.CultureInfo.InvariantCulture);
                        row[field] = converted;
                    }
                    catch
                    {
                    }
                }

                SetSameType(furbolgRow, "UnalteredVisualRaceID", 86);
                SetSameType(furbolgRow, "MaleModelFallbackRaceID", 86);
                SetSameType(furbolgRow, "MaleModelFallbackSex", 0);
                SetSameType(furbolgRow, "FemaleModelFallbackRaceID", 86);
                SetSameType(furbolgRow, "FemaleModelFallbackSex", 1);
                SetSameType(furbolgRow, "UnalteredVisualCustomizationRaceID", 86);

                foreach (DataRow dr in table.Rows)
                {
                    if (int.TryParse(dr["ID"].ToString(), out int id) && id == furbolgId)
                    {
                        foreach (DataColumn col in table.Columns)
                        {
                            string colName = col.ColumnName;
                            object val = null;
                            try { val = furbolgRow[colName]; }
                            catch { }

                            dr[colName] = val?.ToString() ?? "";
                        }
                        break;
                    }
                }

                table.AcceptChanges();
                grid.Refresh();

                MessageBox.Show("ID=95 перекрашен в фурболга (в памяти). Не забудь Save DB2.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Patch error: {ex.Message}");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        static class Program
        {
            [STAThread]
            static void Main()
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new WowDb2Viewer.Form1());
            }
        }
    }
}