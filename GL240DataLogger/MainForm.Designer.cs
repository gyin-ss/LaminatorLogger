namespace GL240DataLogger
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonConnect = new Button();
            buttonStart = new Button();
            richTextBox = new RichTextBox();
            buttonStop = new Button();
            tabControlFunctions = new TabControl();
            tabPage1 = new TabPage();
            checkBoxDBSelection = new CheckBox();
            labelTimeSpan = new Label();
            tabControlDevices = new TabControl();
            tabPage3 = new TabPage();
            label7 = new Label();
            label6 = new Label();
            buttonUpdateSetup = new Button();
            textBoxSetupToSave = new TextBox();
            textBoxSetupToLoad = new TextBox();
            dgvChannelProperties = new DataGridView();
            button1Measure = new Button();
            buttonLoadSetup = new Button();
            buttonSaveSetup = new Button();
            tabPage4 = new TabPage();
            buttonPLC1Call = new Button();
            dgvPLC = new DataGridView();
            groupBox2 = new GroupBox();
            buttonTest = new Button();
            buttonReadList = new Button();
            label5 = new Label();
            textBoxPath = new TextBox();
            buttonReadSymbol = new Button();
            label4 = new Label();
            textBoxOperatorName = new TextBox();
            label2 = new Label();
            textBoxSampleID = new TextBox();
            label1 = new Label();
            tabPage2 = new TabPage();
            dgvRecipe = new DataGridView();
            buttonRecipe = new Button();
            buttonQueryMetaData = new Button();
            dgvMetaData = new DataGridView();
            buttonSaveToCSV = new Button();
            buttonQueryData = new Button();
            textBoxSampleID4Query = new TextBox();
            label3 = new Label();
            dgvData = new DataGridView();
            tabControlFunctions.SuspendLayout();
            tabPage1.SuspendLayout();
            tabControlDevices.SuspendLayout();
            tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvChannelProperties).BeginInit();
            tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPLC).BeginInit();
            groupBox2.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRecipe).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvMetaData).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvData).BeginInit();
            SuspendLayout();
            // 
            // buttonConnect
            // 
            buttonConnect.BackColor = Color.LightGreen;
            buttonConnect.Location = new Point(965, 186);
            buttonConnect.Name = "buttonConnect";
            buttonConnect.Size = new Size(125, 36);
            buttonConnect.TabIndex = 0;
            buttonConnect.Text = "Connect";
            buttonConnect.UseVisualStyleBackColor = false;
            buttonConnect.Click += buttonConnect_Click;
            // 
            // buttonStart
            // 
            buttonStart.BackColor = Color.PeachPuff;
            buttonStart.Location = new Point(965, 247);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(125, 36);
            buttonStart.TabIndex = 1;
            buttonStart.Text = "Start";
            buttonStart.UseVisualStyleBackColor = false;
            buttonStart.Click += buttonStart_Click;
            // 
            // richTextBox
            // 
            richTextBox.Location = new Point(12, 491);
            richTextBox.Name = "richTextBox";
            richTextBox.Size = new Size(1221, 205);
            richTextBox.TabIndex = 2;
            richTextBox.Text = "";
            // 
            // buttonStop
            // 
            buttonStop.BackColor = Color.DarkSalmon;
            buttonStop.Location = new Point(965, 351);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(125, 36);
            buttonStop.TabIndex = 3;
            buttonStop.Text = "Stop";
            buttonStop.UseVisualStyleBackColor = false;
            buttonStop.Click += buttonStop_Click;
            // 
            // tabControlFunctions
            // 
            tabControlFunctions.Controls.Add(tabPage1);
            tabControlFunctions.Controls.Add(tabPage2);
            tabControlFunctions.Location = new Point(12, 12);
            tabControlFunctions.Name = "tabControlFunctions";
            tabControlFunctions.SelectedIndex = 0;
            tabControlFunctions.Size = new Size(1221, 461);
            tabControlFunctions.TabIndex = 4;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(checkBoxDBSelection);
            tabPage1.Controls.Add(labelTimeSpan);
            tabPage1.Controls.Add(tabControlDevices);
            tabPage1.Controls.Add(groupBox2);
            tabPage1.Controls.Add(textBoxOperatorName);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(textBoxSampleID);
            tabPage1.Controls.Add(label1);
            tabPage1.Controls.Add(buttonStart);
            tabPage1.Controls.Add(buttonStop);
            tabPage1.Controls.Add(buttonConnect);
            tabPage1.Location = new Point(4, 29);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1213, 428);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "   Data Acquisition   ";
            tabPage1.UseVisualStyleBackColor = true;
            tabPage1.Click += tabPage1_Click;
            // 
            // checkBoxDBSelection
            // 
            checkBoxDBSelection.AutoSize = true;
            checkBoxDBSelection.Location = new Point(956, 40);
            checkBoxDBSelection.Name = "checkBoxDBSelection";
            checkBoxDBSelection.Size = new Size(161, 24);
            checkBoxDBSelection.TabIndex = 17;
            checkBoxDBSelection.Text = "Use Local Database";
            checkBoxDBSelection.UseVisualStyleBackColor = true;
            checkBoxDBSelection.CheckedChanged += checkBoxDBSelection_CheckedChanged;
            // 
            // labelTimeSpan
            // 
            labelTimeSpan.AutoSize = true;
            labelTimeSpan.Font = new Font("Segoe UI", 13.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            labelTimeSpan.Location = new Point(974, 296);
            labelTimeSpan.Name = "labelTimeSpan";
            labelTimeSpan.Size = new Size(104, 31);
            labelTimeSpan.TabIndex = 16;
            labelTimeSpan.Text = "00:00:00";
            // 
            // tabControlDevices
            // 
            tabControlDevices.Alignment = TabAlignment.Bottom;
            tabControlDevices.Controls.Add(tabPage3);
            tabControlDevices.Controls.Add(tabPage4);
            tabControlDevices.Location = new Point(6, 6);
            tabControlDevices.Multiline = true;
            tabControlDevices.Name = "tabControlDevices";
            tabControlDevices.SelectedIndex = 0;
            tabControlDevices.Size = new Size(781, 416);
            tabControlDevices.TabIndex = 15;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(label7);
            tabPage3.Controls.Add(label6);
            tabPage3.Controls.Add(buttonUpdateSetup);
            tabPage3.Controls.Add(textBoxSetupToSave);
            tabPage3.Controls.Add(textBoxSetupToLoad);
            tabPage3.Controls.Add(dgvChannelProperties);
            tabPage3.Controls.Add(button1Measure);
            tabPage3.Controls.Add(buttonLoadSetup);
            tabPage3.Controls.Add(buttonSaveSetup);
            tabPage3.Location = new Point(4, 4);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(773, 383);
            tabPage3.TabIndex = 0;
            tabPage3.Text = "   GL240   ";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(625, 216);
            label7.Name = "label7";
            label7.Size = new Size(125, 20);
            label7.TabIndex = 19;
            label7.Text = "New Setup Name";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(625, 24);
            label6.Name = "label6";
            label6.Size = new Size(136, 20);
            label6.TabIndex = 18;
            label6.Text = "GL240 Setup Name";
            // 
            // buttonUpdateSetup
            // 
            buttonUpdateSetup.BackColor = Color.PeachPuff;
            buttonUpdateSetup.Location = new Point(626, 136);
            buttonUpdateSetup.Name = "buttonUpdateSetup";
            buttonUpdateSetup.Size = new Size(125, 36);
            buttonUpdateSetup.TabIndex = 17;
            buttonUpdateSetup.Text = "Update Setup";
            buttonUpdateSetup.UseVisualStyleBackColor = false;
            buttonUpdateSetup.Click += buttonUpdateSetup_Click;
            // 
            // textBoxSetupToSave
            // 
            textBoxSetupToSave.Location = new Point(626, 242);
            textBoxSetupToSave.Name = "textBoxSetupToSave";
            textBoxSetupToSave.Size = new Size(125, 27);
            textBoxSetupToSave.TabIndex = 16;
            // 
            // textBoxSetupToLoad
            // 
            textBoxSetupToLoad.Location = new Point(626, 47);
            textBoxSetupToLoad.Name = "textBoxSetupToLoad";
            textBoxSetupToLoad.Size = new Size(125, 27);
            textBoxSetupToLoad.TabIndex = 15;
            // 
            // dgvChannelProperties
            // 
            dgvChannelProperties.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvChannelProperties.Location = new Point(6, 3);
            dgvChannelProperties.Name = "dgvChannelProperties";
            dgvChannelProperties.RowHeadersWidth = 51;
            dgvChannelProperties.Size = new Size(588, 374);
            dgvChannelProperties.TabIndex = 1;
            // 
            // button1Measure
            // 
            button1Measure.Location = new Point(626, 326);
            button1Measure.Name = "button1Measure";
            button1Measure.Size = new Size(125, 36);
            button1Measure.TabIndex = 14;
            button1Measure.Text = "1 Measurement";
            button1Measure.UseVisualStyleBackColor = true;
            button1Measure.Visible = false;
            button1Measure.Click += button1Measure_Click;
            // 
            // buttonLoadSetup
            // 
            buttonLoadSetup.BackColor = Color.LightGreen;
            buttonLoadSetup.Location = new Point(626, 91);
            buttonLoadSetup.Name = "buttonLoadSetup";
            buttonLoadSetup.Size = new Size(125, 36);
            buttonLoadSetup.TabIndex = 13;
            buttonLoadSetup.Text = "Load Setup";
            buttonLoadSetup.UseVisualStyleBackColor = false;
            buttonLoadSetup.Click += buttonLoadSetup_Click;
            // 
            // buttonSaveSetup
            // 
            buttonSaveSetup.BackColor = Color.DarkSalmon;
            buttonSaveSetup.Location = new Point(626, 275);
            buttonSaveSetup.Name = "buttonSaveSetup";
            buttonSaveSetup.Size = new Size(125, 36);
            buttonSaveSetup.TabIndex = 12;
            buttonSaveSetup.Text = "Save Setup";
            buttonSaveSetup.UseVisualStyleBackColor = false;
            buttonSaveSetup.Click += buttonSaveSetup_Click;
            // 
            // tabPage4
            // 
            tabPage4.Controls.Add(buttonPLC1Call);
            tabPage4.Controls.Add(dgvPLC);
            tabPage4.Location = new Point(4, 4);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new Padding(3);
            tabPage4.Size = new Size(773, 383);
            tabPage4.TabIndex = 1;
            tabPage4.Text = "   Beckhoff PLC   ";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // buttonPLC1Call
            // 
            buttonPLC1Call.Location = new Point(620, 65);
            buttonPLC1Call.Name = "buttonPLC1Call";
            buttonPLC1Call.Size = new Size(125, 36);
            buttonPLC1Call.TabIndex = 17;
            buttonPLC1Call.Text = "Read Symbols";
            buttonPLC1Call.UseVisualStyleBackColor = true;
            buttonPLC1Call.Click += buttonPLC1Call_Click;
            // 
            // dgvPLC
            // 
            dgvPLC.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPLC.Location = new Point(6, 6);
            dgvPLC.Name = "dgvPLC";
            dgvPLC.RowHeadersWidth = 51;
            dgvPLC.Size = new Size(588, 371);
            dgvPLC.TabIndex = 0;
            dgvPLC.CellContentClick += dataGridView1_CellContentClick;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(buttonTest);
            groupBox2.Controls.Add(buttonReadList);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(textBoxPath);
            groupBox2.Controls.Add(buttonReadSymbol);
            groupBox2.Controls.Add(label4);
            groupBox2.Location = new Point(797, 207);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(137, 213);
            groupBox2.TabIndex = 11;
            groupBox2.TabStop = false;
            groupBox2.Text = "Testing items";
            groupBox2.Visible = false;
            // 
            // buttonTest
            // 
            buttonTest.Location = new Point(12, 28);
            buttonTest.Name = "buttonTest";
            buttonTest.Size = new Size(104, 29);
            buttonTest.TabIndex = 18;
            buttonTest.Text = "Test";
            buttonTest.UseVisualStyleBackColor = true;
            buttonTest.Visible = false;
            buttonTest.Click += buttonTest_Click;
            // 
            // buttonReadList
            // 
            buttonReadList.Location = new Point(9, 159);
            buttonReadList.Name = "buttonReadList";
            buttonReadList.Size = new Size(125, 29);
            buttonReadList.TabIndex = 16;
            buttonReadList.Text = "Read multiCall";
            buttonReadList.UseVisualStyleBackColor = true;
            buttonReadList.Click += buttonReadList_Click;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(25, 60);
            label5.Name = "label5";
            label5.Size = new Size(91, 20);
            label5.TabIndex = 15;
            label5.Text = "Symbol Path";
            // 
            // textBoxPath
            // 
            textBoxPath.Location = new Point(6, 83);
            textBoxPath.Name = "textBoxPath";
            textBoxPath.Size = new Size(125, 27);
            textBoxPath.TabIndex = 14;
            textBoxPath.Text = "TCI.rTemp_Avg_degC";
            textBoxPath.TextChanged += textBoxPath_TextChanged;
            // 
            // buttonReadSymbol
            // 
            buttonReadSymbol.Location = new Point(6, 124);
            buttonReadSymbol.Name = "buttonReadSymbol";
            buttonReadSymbol.Size = new Size(125, 29);
            buttonReadSymbol.TabIndex = 13;
            buttonReadSymbol.Text = "Read Symbol";
            buttonReadSymbol.UseVisualStyleBackColor = true;
            buttonReadSymbol.Click += buttonReadSymbol_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(-11, 26);
            label4.Name = "label4";
            label4.Size = new Size(0, 20);
            label4.TabIndex = 8;
            // 
            // textBoxOperatorName
            // 
            textBoxOperatorName.Location = new Point(958, 136);
            textBoxOperatorName.Name = "textBoxOperatorName";
            textBoxOperatorName.Size = new Size(144, 27);
            textBoxOperatorName.TabIndex = 7;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(838, 138);
            label2.Name = "label2";
            label2.Size = new Size(113, 20);
            label2.TabIndex = 6;
            label2.Text = "Operator Name";
            // 
            // textBoxSampleID
            // 
            textBoxSampleID.Location = new Point(958, 90);
            textBoxSampleID.Name = "textBoxSampleID";
            textBoxSampleID.Size = new Size(144, 27);
            textBoxSampleID.TabIndex = 5;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(858, 92);
            label1.Name = "label1";
            label1.Size = new Size(78, 20);
            label1.TabIndex = 4;
            label1.Text = "Sample ID";
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(dgvRecipe);
            tabPage2.Controls.Add(buttonRecipe);
            tabPage2.Controls.Add(buttonQueryMetaData);
            tabPage2.Controls.Add(dgvMetaData);
            tabPage2.Controls.Add(buttonSaveToCSV);
            tabPage2.Controls.Add(buttonQueryData);
            tabPage2.Controls.Add(textBoxSampleID4Query);
            tabPage2.Controls.Add(label3);
            tabPage2.Controls.Add(dgvData);
            tabPage2.Location = new Point(4, 29);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1213, 428);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "   Data Query   ";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // dgvRecipe
            // 
            dgvRecipe.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvRecipe.Location = new Point(3, 68);
            dgvRecipe.Name = "dgvRecipe";
            dgvRecipe.RowHeadersWidth = 51;
            dgvRecipe.Size = new Size(1201, 354);
            dgvRecipe.TabIndex = 18;
            dgvRecipe.Visible = false;
            // 
            // buttonRecipe
            // 
            buttonRecipe.BackColor = Color.Khaki;
            buttonRecipe.Location = new Point(834, 15);
            buttonRecipe.Name = "buttonRecipe";
            buttonRecipe.Size = new Size(149, 40);
            buttonRecipe.TabIndex = 17;
            buttonRecipe.Text = "Query Recipe";
            buttonRecipe.UseVisualStyleBackColor = false;
            buttonRecipe.Click += buttonRecipe_Click;
            // 
            // buttonQueryMetaData
            // 
            buttonQueryMetaData.BackColor = Color.MediumTurquoise;
            buttonQueryMetaData.Location = new Point(637, 14);
            buttonQueryMetaData.Name = "buttonQueryMetaData";
            buttonQueryMetaData.Size = new Size(149, 40);
            buttonQueryMetaData.TabIndex = 16;
            buttonQueryMetaData.Text = "Query MetaData";
            buttonQueryMetaData.UseVisualStyleBackColor = false;
            buttonQueryMetaData.Click += buttonQueryMetaData_Click;
            // 
            // dgvMetaData
            // 
            dgvMetaData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvMetaData.Location = new Point(6, 68);
            dgvMetaData.Name = "dgvMetaData";
            dgvMetaData.RowHeadersWidth = 51;
            dgvMetaData.Size = new Size(1201, 354);
            dgvMetaData.TabIndex = 15;
            dgvMetaData.Visible = false;
            // 
            // buttonSaveToCSV
            // 
            buttonSaveToCSV.BackColor = Color.NavajoWhite;
            buttonSaveToCSV.Location = new Point(1030, 14);
            buttonSaveToCSV.Name = "buttonSaveToCSV";
            buttonSaveToCSV.Size = new Size(149, 40);
            buttonSaveToCSV.TabIndex = 14;
            buttonSaveToCSV.Text = "Save to CSV";
            buttonSaveToCSV.UseVisualStyleBackColor = false;
            buttonSaveToCSV.Click += buttonSaveToCSV_Click;
            // 
            // buttonQueryData
            // 
            buttonQueryData.BackColor = Color.YellowGreen;
            buttonQueryData.Location = new Point(440, 15);
            buttonQueryData.Name = "buttonQueryData";
            buttonQueryData.Size = new Size(149, 40);
            buttonQueryData.TabIndex = 13;
            buttonQueryData.Text = "Query Data";
            buttonQueryData.UseVisualStyleBackColor = false;
            buttonQueryData.Click += buttonQueryDada_Click;
            // 
            // textBoxSampleID4Query
            // 
            textBoxSampleID4Query.Location = new Point(124, 22);
            textBoxSampleID4Query.Name = "textBoxSampleID4Query";
            textBoxSampleID4Query.Size = new Size(144, 27);
            textBoxSampleID4Query.TabIndex = 6;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(23, 25);
            label3.Name = "label3";
            label3.Size = new Size(78, 20);
            label3.TabIndex = 1;
            label3.Text = "Sample ID";
            // 
            // dgvData
            // 
            dgvData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvData.Location = new Point(6, 68);
            dgvData.Name = "dgvData";
            dgvData.RowHeadersWidth = 51;
            dgvData.Size = new Size(1201, 354);
            dgvData.TabIndex = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1241, 708);
            Controls.Add(tabControlFunctions);
            Controls.Add(richTextBox);
            Name = "MainForm";
            Text = "Laminator Data Logger V1.02";
            Load += MainForm_Load;
            tabControlFunctions.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            tabControlDevices.ResumeLayout(false);
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvChannelProperties).EndInit();
            tabPage4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvPLC).EndInit();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRecipe).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvMetaData).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvData).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button buttonConnect;
        private Button buttonStart;
        private RichTextBox richTextBox;
        private Button buttonStop;
        private TabControl tabControlFunctions;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private TextBox textBoxOperatorName;
        private Label label2;
        private TextBox textBoxSampleID;
        private Label label1;
        private GroupBox groupBox2;
        private Label label4;
        private DataGridView dgvChannelProperties;
        private Button buttonLoadSetup;
        private Button buttonSaveSetup;
        private Button button1Measure;
        private DataGridView dgvData;
        private Button buttonQueryData;
        private TextBox textBoxSampleID4Query;
        private Label label3;
        private Button buttonSaveToCSV;
        private Label label5;
        private TextBox textBoxPath;
        private Button buttonReadSymbol;
        private Button buttonReadList;
        private Button buttonPLC1Call;
        private TabControl tabControlDevices;
        private TabPage tabPage3;
        private TabPage tabPage4;
        private DataGridView dgvPLC;
        private Label labelTimeSpan;
        private DataGridView dgvMetaData;
        private Button buttonRecipe;
        private Button buttonQueryMetaData;
        private DataGridView dgvRecipe;
        private TextBox textBoxSetupToSave;
        private TextBox textBoxSetupToLoad;
        private CheckBox checkBoxDBSelection;
        private Button buttonTest;
        private Button buttonUpdateSetup;
        private Label label7;
        private Label label6;
    }
}
