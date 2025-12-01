namespace BlitzPatch
{
    partial class Gui
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.landingPanel = new System.Windows.Forms.Panel();
            this.exportLiteDbButton = new System.Windows.Forms.Button();
            this.landingPatchButton = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.landingStartButton = new System.Windows.Forms.Button();
            this.landingTitleLabel = new System.Windows.Forms.Label();
            this.unitPanel = new System.Windows.Forms.Panel();
            this.saveAsButton = new System.Windows.Forms.Button();
            this.factionFilterLabel = new System.Windows.Forms.Label();
            this.factionFilterCombo = new System.Windows.Forms.ComboBox();
            this.exportJsonButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.reloadButton = new System.Windows.Forms.Button();
            this.backButton = new System.Windows.Forms.Button();
            this.editorTabControl = new System.Windows.Forms.TabControl();
            this.jsonTabPage = new System.Windows.Forms.TabPage();
            this.jsonEditorTextBox = new System.Windows.Forms.TextBox();
            this.unitsTabPage = new System.Windows.Forms.TabPage();
            this.syncUnitsButton = new System.Windows.Forms.Button();
            this.deleteUnitButton = new System.Windows.Forms.Button();
            this.editUnitButton = new System.Windows.Forms.Button();
            this.addUnitButton = new System.Windows.Forms.Button();
            this.nextIdLabel = new System.Windows.Forms.Label();
            this.unitsDataGridView = new System.Windows.Forms.DataGridView();
            this.listBoxRecords = new System.Windows.Forms.ListBox();
            this.summaryTextBox = new System.Windows.Forms.TextBox();
            this.profileSourceLabel = new System.Windows.Forms.Label();
            this.landingPanel.SuspendLayout();
            this.unitPanel.SuspendLayout();
            this.editorTabControl.SuspendLayout();
            this.jsonTabPage.SuspendLayout();
            this.unitsTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.unitsDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // landingPanel
            // 
            this.landingPanel.Controls.Add(this.unitPanel);
            this.landingPanel.Controls.Add(this.exportLiteDbButton);
            this.landingPanel.Controls.Add(this.landingPatchButton);
            this.landingPanel.Controls.Add(this.button1);
            this.landingPanel.Controls.Add(this.textBox1);
            this.landingPanel.Controls.Add(this.label1);
            this.landingPanel.Controls.Add(this.landingStartButton);
            this.landingPanel.Controls.Add(this.landingTitleLabel);
            this.landingPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.landingPanel.Location = new System.Drawing.Point(0, 0);
            this.landingPanel.Name = "landingPanel";
            this.landingPanel.Size = new System.Drawing.Size(640, 480);
            this.landingPanel.TabIndex = 0;
            // 
            // exportLiteDbButton
            // 
            this.exportLiteDbButton.Location = new System.Drawing.Point(3, 280);
            this.exportLiteDbButton.Name = "exportLiteDbButton";
            this.exportLiteDbButton.Size = new System.Drawing.Size(234, 24);
            this.exportLiteDbButton.TabIndex = 5;
            this.exportLiteDbButton.Text = "Export LiteDB (_a) to JSON...";
            this.exportLiteDbButton.UseVisualStyleBackColor = true;
            this.exportLiteDbButton.Click += new System.EventHandler(this.exportLiteDbButton_Click);
            // 
            // landingPatchButton
            // 
            this.landingPatchButton.Location = new System.Drawing.Point(393, 242);
            this.landingPatchButton.Name = "landingPatchButton";
            this.landingPatchButton.Size = new System.Drawing.Size(96, 24);
            this.landingPatchButton.TabIndex = 6;
            this.landingPatchButton.Text = "Load && Patch";
            this.landingPatchButton.UseVisualStyleBackColor = true;
            this.landingPatchButton.Click += new System.EventHandler(this.landingPatchButton_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(268, 242);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(54, 24);
            this.button1.TabIndex = 2;
            this.button1.Text = "Browse";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(3, 245);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(259, 20);
            this.textBox1.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 268);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(216, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Select your Blitzkrieg 3 directory to continue.";
            // 
            // landingStartButton
            // 
            this.landingStartButton.Location = new System.Drawing.Point(328, 242);
            this.landingStartButton.Name = "landingStartButton";
            this.landingStartButton.Size = new System.Drawing.Size(59, 24);
            this.landingStartButton.TabIndex = 3;
            this.landingStartButton.Text = "Continue";
            this.landingStartButton.UseVisualStyleBackColor = true;
            this.landingStartButton.Click += new System.EventHandler(this.landingStartButton_Click);
            // 
            // landingTitleLabel
            // 
            this.landingTitleLabel.AutoSize = true;
            this.landingTitleLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.landingTitleLabel.Location = new System.Drawing.Point(12, 9);
            this.landingTitleLabel.Name = "landingTitleLabel";
            this.landingTitleLabel.Size = new System.Drawing.Size(203, 30);
            this.landingTitleLabel.TabIndex = 0;
            this.landingTitleLabel.Text = "BlitzPatch (0.00.1)";
            // 
            // unitPanel
            // 
            this.unitPanel.Controls.Add(this.saveAsButton);
            this.unitPanel.Controls.Add(this.factionFilterLabel);
            this.unitPanel.Controls.Add(this.factionFilterCombo);
            this.unitPanel.Controls.Add(this.exportJsonButton);
            this.unitPanel.Controls.Add(this.saveButton);
            this.unitPanel.Controls.Add(this.reloadButton);
            this.unitPanel.Controls.Add(this.backButton);
            this.unitPanel.Controls.Add(this.editorTabControl);
            this.unitPanel.Controls.Add(this.listBoxRecords);
            this.unitPanel.Controls.Add(this.summaryTextBox);
            this.unitPanel.Controls.Add(this.profileSourceLabel);
            this.unitPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.unitPanel.Location = new System.Drawing.Point(0, 0);
            this.unitPanel.Name = "unitPanel";
            this.unitPanel.Size = new System.Drawing.Size(640, 480);
            this.unitPanel.TabIndex = 2;
            this.unitPanel.Visible = false;
            // 
            // saveAsButton
            // 
            this.saveAsButton.Location = new System.Drawing.Point(432, 420);
            this.saveAsButton.Name = "saveAsButton";
            this.saveAsButton.Size = new System.Drawing.Size(95, 23);
            this.saveAsButton.TabIndex = 8;
            this.saveAsButton.Text = "Save As...";
            this.saveAsButton.UseVisualStyleBackColor = true;
            this.saveAsButton.Click += new System.EventHandler(this.saveAsButton_Click);
            // 
            // factionFilterLabel
            // 
            this.factionFilterLabel.AutoSize = true;
            this.factionFilterLabel.Location = new System.Drawing.Point(249, 10);
            this.factionFilterLabel.Name = "factionFilterLabel";
            this.factionFilterLabel.Size = new System.Drawing.Size(88, 13);
            this.factionFilterLabel.TabIndex = 11;
            this.factionFilterLabel.Text = "UserFactionType";
            // 
            // factionFilterCombo
            // 
            this.factionFilterCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.factionFilterCombo.FormattingEnabled = true;
            this.factionFilterCombo.Location = new System.Drawing.Point(345, 7);
            this.factionFilterCombo.Name = "factionFilterCombo";
            this.factionFilterCombo.Size = new System.Drawing.Size(167, 21);
            this.factionFilterCombo.TabIndex = 2;
            this.factionFilterCombo.SelectedIndexChanged += new System.EventHandler(this.factionFilterCombo_SelectedIndexChanged);
            // 
            // exportJsonButton
            // 
            this.exportJsonButton.Location = new System.Drawing.Point(250, 420);
            this.exportJsonButton.Name = "exportJsonButton";
            this.exportJsonButton.Size = new System.Drawing.Size(90, 23);
            this.exportJsonButton.TabIndex = 6;
            this.exportJsonButton.Text = "Export JSON";
            this.exportJsonButton.UseVisualStyleBackColor = true;
            this.exportJsonButton.Click += new System.EventHandler(this.exportJsonButton_Click);
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(533, 420);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(94, 23);
            this.saveButton.TabIndex = 9;
            this.saveButton.Text = "Save to LiteDB";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // reloadButton
            // 
            this.reloadButton.Location = new System.Drawing.Point(346, 420);
            this.reloadButton.Name = "reloadButton";
            this.reloadButton.Size = new System.Drawing.Size(80, 23);
            this.reloadButton.TabIndex = 7;
            this.reloadButton.Text = "Reload";
            this.reloadButton.UseVisualStyleBackColor = true;
            this.reloadButton.Click += new System.EventHandler(this.reloadButton_Click);
            // 
            // backButton
            // 
            this.backButton.Location = new System.Drawing.Point(13, 420);
            this.backButton.Name = "backButton";
            this.backButton.Size = new System.Drawing.Size(75, 23);
            this.backButton.TabIndex = 4;
            this.backButton.Text = "Back";
            this.backButton.UseVisualStyleBackColor = true;
            this.backButton.Click += new System.EventHandler(this.backButton_Click);
            // 
            // editorTabControl
            // 
            this.editorTabControl.Controls.Add(this.jsonTabPage);
            this.editorTabControl.Controls.Add(this.unitsTabPage);
            this.editorTabControl.Location = new System.Drawing.Point(13, 155);
            this.editorTabControl.Name = "editorTabControl";
            this.editorTabControl.SelectedIndex = 0;
            this.editorTabControl.Size = new System.Drawing.Size(614, 250);
            this.editorTabControl.TabIndex = 5;
            // 
            // jsonTabPage
            // 
            this.jsonTabPage.Controls.Add(this.jsonEditorTextBox);
            this.jsonTabPage.Location = new System.Drawing.Point(4, 22);
            this.jsonTabPage.Name = "jsonTabPage";
            this.jsonTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.jsonTabPage.Size = new System.Drawing.Size(606, 224);
            this.jsonTabPage.TabIndex = 0;
            this.jsonTabPage.Text = "Raw JSON";
            this.jsonTabPage.UseVisualStyleBackColor = true;
            // 
            // jsonEditorTextBox
            // 
            this.jsonEditorTextBox.AcceptsReturn = true;
            this.jsonEditorTextBox.AcceptsTab = true;
            this.jsonEditorTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jsonEditorTextBox.Location = new System.Drawing.Point(3, 3);
            this.jsonEditorTextBox.Multiline = true;
            this.jsonEditorTextBox.Name = "jsonEditorTextBox";
            this.jsonEditorTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.jsonEditorTextBox.Size = new System.Drawing.Size(600, 218);
            this.jsonEditorTextBox.TabIndex = 0;
            this.jsonEditorTextBox.WordWrap = false;
            // 
            // unitsTabPage
            // 
            this.unitsTabPage.Controls.Add(this.syncUnitsButton);
            this.unitsTabPage.Controls.Add(this.deleteUnitButton);
            this.unitsTabPage.Controls.Add(this.editUnitButton);
            this.unitsTabPage.Controls.Add(this.addUnitButton);
            this.unitsTabPage.Controls.Add(this.nextIdLabel);
            this.unitsTabPage.Controls.Add(this.unitsDataGridView);
            this.unitsTabPage.Location = new System.Drawing.Point(4, 22);
            this.unitsTabPage.Name = "unitsTabPage";
            this.unitsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.unitsTabPage.Size = new System.Drawing.Size(606, 224);
            this.unitsTabPage.TabIndex = 1;
            this.unitsTabPage.Text = "Units";
            this.unitsTabPage.UseVisualStyleBackColor = true;
            // 
            // syncUnitsButton
            // 
            this.syncUnitsButton.Location = new System.Drawing.Point(454, 193);
            this.syncUnitsButton.Name = "syncUnitsButton";
            this.syncUnitsButton.Size = new System.Drawing.Size(146, 23);
            this.syncUnitsButton.TabIndex = 5;
            this.syncUnitsButton.Text = "Reload from JSON";
            this.syncUnitsButton.UseVisualStyleBackColor = true;
            this.syncUnitsButton.Click += new System.EventHandler(this.syncUnitsButton_Click);
            // 
            // deleteUnitButton
            // 
            this.deleteUnitButton.Location = new System.Drawing.Point(185, 193);
            this.deleteUnitButton.Name = "deleteUnitButton";
            this.deleteUnitButton.Size = new System.Drawing.Size(84, 23);
            this.deleteUnitButton.TabIndex = 4;
            this.deleteUnitButton.Text = "Delete";
            this.deleteUnitButton.UseVisualStyleBackColor = true;
            this.deleteUnitButton.Click += new System.EventHandler(this.deleteUnitButton_Click);
            // 
            // editUnitButton
            // 
            this.editUnitButton.Location = new System.Drawing.Point(95, 193);
            this.editUnitButton.Name = "editUnitButton";
            this.editUnitButton.Size = new System.Drawing.Size(84, 23);
            this.editUnitButton.TabIndex = 3;
            this.editUnitButton.Text = "Edit";
            this.editUnitButton.UseVisualStyleBackColor = true;
            this.editUnitButton.Click += new System.EventHandler(this.editUnitButton_Click);
            // 
            // addUnitButton
            // 
            this.addUnitButton.Location = new System.Drawing.Point(5, 193);
            this.addUnitButton.Name = "addUnitButton";
            this.addUnitButton.Size = new System.Drawing.Size(84, 23);
            this.addUnitButton.TabIndex = 2;
            this.addUnitButton.Text = "Add";
            this.addUnitButton.UseVisualStyleBackColor = true;
            this.addUnitButton.Click += new System.EventHandler(this.addUnitButton_Click);
            // 
            // nextIdLabel
            // 
            this.nextIdLabel.AutoSize = true;
            this.nextIdLabel.Location = new System.Drawing.Point(2, 177);
            this.nextIdLabel.Name = "nextIdLabel";
            this.nextIdLabel.Size = new System.Drawing.Size(86, 13);
            this.nextIdLabel.TabIndex = 1;
            this.nextIdLabel.Text = "Next unit id: N/A";
            // 
            // unitsDataGridView
            // 
            this.unitsDataGridView.AllowUserToAddRows = false;
            this.unitsDataGridView.AllowUserToDeleteRows = false;
            this.unitsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.unitsDataGridView.Location = new System.Drawing.Point(5, 6);
            this.unitsDataGridView.MultiSelect = false;
            this.unitsDataGridView.Name = "unitsDataGridView";
            this.unitsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.unitsDataGridView.Size = new System.Drawing.Size(595, 166);
            this.unitsDataGridView.TabIndex = 0;
            // 
            // listBoxRecords
            // 
            this.listBoxRecords.FormattingEnabled = true;
            this.listBoxRecords.HorizontalScrollbar = true;
            this.listBoxRecords.Location = new System.Drawing.Point(13, 39);
            this.listBoxRecords.Name = "listBoxRecords";
            this.listBoxRecords.Size = new System.Drawing.Size(230, 108);
            this.listBoxRecords.TabIndex = 4;
            this.listBoxRecords.SelectedIndexChanged += new System.EventHandler(this.listBoxRecords_SelectedIndexChanged);
            // 
            // summaryTextBox
            // 
            this.summaryTextBox.Location = new System.Drawing.Point(249, 39);
            this.summaryTextBox.Multiline = true;
            this.summaryTextBox.Name = "summaryTextBox";
            this.summaryTextBox.ReadOnly = true;
            this.summaryTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.summaryTextBox.Size = new System.Drawing.Size(378, 108);
            this.summaryTextBox.TabIndex = 3;
            // 
            // profileSourceLabel
            // 
            this.profileSourceLabel.AutoSize = true;
            this.profileSourceLabel.Location = new System.Drawing.Point(10, 10);
            this.profileSourceLabel.Name = "profileSourceLabel";
            this.profileSourceLabel.Size = new System.Drawing.Size(107, 13);
            this.profileSourceLabel.TabIndex = 0;
            this.profileSourceLabel.Text = "No profile loaded yet.";
            // 
            // Gui
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 480);
            this.Controls.Add(this.landingPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Gui";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BlitzPatch";
            this.Load += new System.EventHandler(this.Gui_Load);
            this.landingPanel.ResumeLayout(false);
            this.landingPanel.PerformLayout();
            this.unitPanel.ResumeLayout(false);
            this.unitPanel.PerformLayout();
            this.editorTabControl.ResumeLayout(false);
            this.jsonTabPage.ResumeLayout(false);
            this.jsonTabPage.PerformLayout();
            this.unitsTabPage.ResumeLayout(false);
            this.unitsTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.unitsDataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel landingPanel;
        private System.Windows.Forms.Button landingStartButton;
        private System.Windows.Forms.Label landingTitleLabel;
        private System.Windows.Forms.Panel unitPanel;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label profileSourceLabel;
        private System.Windows.Forms.TextBox summaryTextBox;
        private System.Windows.Forms.Button backButton;
        private System.Windows.Forms.Button reloadButton;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.ListBox listBoxRecords;
        private System.Windows.Forms.Button exportJsonButton;
        private System.Windows.Forms.ComboBox factionFilterCombo;
        private System.Windows.Forms.Label factionFilterLabel;
        private System.Windows.Forms.Button saveAsButton;
        private System.Windows.Forms.TabControl editorTabControl;
        private System.Windows.Forms.TabPage jsonTabPage;
        private System.Windows.Forms.TextBox jsonEditorTextBox;
        private System.Windows.Forms.TabPage unitsTabPage;
        private System.Windows.Forms.Button syncUnitsButton;
        private System.Windows.Forms.Button deleteUnitButton;
        private System.Windows.Forms.Button editUnitButton;
        private System.Windows.Forms.Button addUnitButton;
        private System.Windows.Forms.Label nextIdLabel;
        private System.Windows.Forms.DataGridView unitsDataGridView;
        private System.Windows.Forms.Button exportLiteDbButton;
        private System.Windows.Forms.Button landingPatchButton;
    }
}
