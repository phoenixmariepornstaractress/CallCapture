using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using NAudio.Wave;

namespace CallRecordingApp
{
    public partial class MainForm : Form
    {
        private WaveInEvent waveIn;
        private WaveFileWriter writer;
        private string outputFolder = @"C:\CallRecordings";
        private string currentFileName;
        private BindingList<string> callLogs;

        public MainForm()
        {
            InitializeComponent();
            callLogs = new BindingList<string>();
            listBoxCallLogs.DataSource = callLogs;
            LoadInputDevices();
            LoadAudioFormats();
            CheckDiskSpace();
        }

        private void LoadInputDevices()
        {
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var deviceInfo = WaveIn.GetCapabilities(i);
                comboBoxInputDevices.Items.Add(deviceInfo.ProductName);
            }
            if (comboBoxInputDevices.Items.Count > 0)
                comboBoxInputDevices.SelectedIndex = 0;
        }

        private void LoadAudioFormats()
        {
            comboBoxAudioFormats.Items.Add("WAV");
            comboBoxAudioFormats.Items.Add("MP3");
            comboBoxAudioFormats.SelectedIndex = 0;
        }

        private void btnStartRecording_Click(object sender, EventArgs e)
        {
            int deviceNumber = comboBoxInputDevices.SelectedIndex;
            waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(44100, 1)
            };

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;

            currentFileName = $"{outputFolder}\\Recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
            writer = new WaveFileWriter(currentFileName, waveIn.WaveFormat);

            waveIn.StartRecording();
            lblStatus.Text = "Recording...";
        }

        private void btnPauseRecording_Click(object sender, EventArgs e)
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                lblStatus.Text = "Recording paused.";
            }
        }

        private void btnResumeRecording_Click(object sender, EventArgs e)
        {
            if (waveIn != null && writer != null)
            {
                waveIn.StartRecording();
                lblStatus.Text = "Recording resumed.";
            }
        }

        private void btnStopRecording_Click(object sender, EventArgs e)
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                lblStatus.Text = "Recording stopped.";
            }
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (writer != null)
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
                writer.Flush();
            }
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }

            waveIn.Dispose();
            waveIn = null;

            if (e.Exception != null)
            {
                MessageBox.Show($"An error occurred: {e.Exception.Message}");
            }
            else
            {
                callLogs.Add(currentFileName);
            }
        }

        private void btnPlayRecording_Click(object sender, EventArgs e)
        {
            if (listBoxCallLogs.SelectedItem != null)
            {
                string fileName = listBoxCallLogs.SelectedItem.ToString();
                using (var audioFile = new AudioFileReader(fileName))
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    MessageBox.Show("Playing recording...");
                }
            }
        }

        private void btnDeleteRecording_Click(object sender, EventArgs e)
        {
            if (listBoxCallLogs.SelectedItem != null)
            {
                string fileName = listBoxCallLogs.SelectedItem.ToString();
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                    callLogs.Remove(fileName);
                }
            }
        }

        private void btnExportLogs_Click(object sender, EventArgs e)
        {
            string logFilePath = $"{outputFolder}\\CallLogs_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            File.WriteAllLines(logFilePath, callLogs.ToArray());
            MessageBox.Show($"Logs exported to {logFilePath}");
        }

        private void btnImportRecordings_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*";
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string fileName in openFileDialog.FileNames)
                    {
                        string destFileName = Path.Combine(outputFolder, Path.GetFileName(fileName));
                        File.Copy(fileName, destFileName, true);
                        callLogs.Add(destFileName);
                    }
                }
            }
        }

        private void btnExportRecordings_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string exportFolder = folderBrowserDialog.SelectedPath;
                    foreach (string fileName in callLogs)
                    {
                        string destFileName = Path.Combine(exportFolder, Path.GetFileName(fileName));
                        File.Copy(fileName, destFileName, true);
                    }
                    MessageBox.Show($"Recordings exported to {exportFolder}");
                }
            }
        }

        private void btnSearchLogs_Click(object sender, EventArgs e)
        {
            string searchQuery = txtSearch.Text.ToLower();
            var searchResults = callLogs.Where(log => log.ToLower().Contains(searchQuery)).ToList();
            listBoxCallLogs.DataSource = new BindingList<string>(searchResults);
        }

        private void trackBarVolume_Scroll(object sender, EventArgs e)
        {
            // Handle volume change here
        }

        private void CheckDiskSpace()
        {
            DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(outputFolder));
            if (driveInfo.AvailableFreeSpace < 1000000000) // less than 1 GB
            {
                MessageBox.Show("Warning: Low disk space!");
            }
        }

        private void InitializeComponent()
        {
            this.btnStartRecording = new System.Windows.Forms.Button();
            this.btnPauseRecording = new System.Windows.Forms.Button();
            this.btnResumeRecording = new System.Windows.Forms.Button();
            this.btnStopRecording = new System.Windows.Forms.Button();
            this.btnPlayRecording = new System.Windows.Forms.Button();
            this.btnDeleteRecording = new System.Windows.Forms.Button();
            this.btnExportLogs = new System.Windows.Forms.Button();
            this.btnImportRecordings = new System.Windows.Forms.Button();
            this.btnExportRecordings = new System.Windows.Forms.Button();
            this.btnSearchLogs = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.listBoxCallLogs = new System.Windows.Forms.ListBox();
            this.txtSaveLocation = new System.Windows.Forms.TextBox();
            this.lblSaveLocation = new System.Windows.Forms.Label();
            this.comboBoxInputDevices = new System.Windows.Forms.ComboBox();
            this.lblInputDevices = new System.Windows.Forms.Label();
            this.comboBoxAudioFormats = new System.Windows.Forms.ComboBox();
            this.lblAudioFormats = new System.Windows.Forms.Label();
            this.trackBarVolume = new System.Windows.Forms.TrackBar();
            this.lblVolume = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarVolume)).BeginInit();
            this.SuspendLayout();
            // 
            // btnStartRecording
            // 
            this.btnStartRecording.Location = new System.Drawing.Point(30, 30);
            this.btnStartRecording.Name = "btnStartRecording";
            this.btnStartRecording.Size = new System.Drawing.Size(120, 40);
            this.btnStartRecording.TabIndex = 0;
            this.btnStartRecording.Text = "Start Recording";
            this.btnStartRecording.UseVisualStyleBackColor = true;
            this.btnStartRecording.Click += new System.EventHandler(this.btnStartRecording_Click);
            // 
            // btnPauseRecording
            // 
            this.btnPauseRecording.Location = new System.Drawing.Point(180, 30);
            this.btnPauseRecording.Name = "btnPauseRecording";
            this.btnPauseRecording.Size = new System.Drawing.Size(120, 40);
            this.btnPauseRecording.TabIndex = 1;
            this.btnPauseRecording.Text = "Pause Recording";
            this.btnPauseRecording.UseVisualStyleBackColor = true;
            this.btnPauseRecording.Click += new System.EventHandler(this.btnPauseRecording_Click);
            // 
            // btnResumeRecording
            // 
            this.btnResumeRecording.Location = new System.Drawing.Point(330, 30);
            this.btnResumeRecording.Name = "btnResumeRecording";
            this.btnResumeRecording.Size = new System.Drawing.Size(120, 40);
            this.btnResumeRecording.TabIndex = 2;
            this.btnResumeRecording.Text = "Resume Recording";
            this.btnResumeRecording.UseVisualStyleBackColor = true;
            this.btnResumeRecording.Click += new System.EventHandler(this.btnResumeRecording_Click);
            // 
            // btnStopRecording
            // 
            this.btnStopRecording.Location = new System.Drawing.Point(30, 90);
            this.btnStopRecording.Name = "btnStopRecording";
            this.btnStopRecording.Size = new System.Drawing.Size(120, 40);
            this.btnStopRecording.TabIndex = 3;
            this.btnStopRecording.Text = "Stop Recording";
            this.btnStopRecording.UseVisualStyleBackColor = true;
            this.btnStopRecording.Click += new System.EventHandler(this.btnStopRecording_Click);
            // 
            // btnPlayRecording
            // 
            this.btnPlayRecording.Location = new System.Drawing.Point(180, 90);
            this.btnPlayRecording.Name = "btnPlayRecording";
            this.btnPlayRecording.Size = new System.Drawing.Size(120, 40);
            this.btnPlayRecording.TabIndex = 4;
            this.btnPlayRecording.Text = "Play Recording";
            this.btnPlayRecording.UseVisualStyleBackColor = true;
            this.btnPlayRecording.Click += new System.EventHandler(this.btnPlayRecording_Click);
            // 
            // btnDeleteRecording
            // 
            this.btnDeleteRecording.Location = new System.Drawing.Point(330, 90);
            this.btnDeleteRecording.Name = "btnDeleteRecording";
            this.btnDeleteRecording.Size = new System.Drawing.Size(120, 40);
            this.btnDeleteRecording.TabIndex = 5;
            this.btnDeleteRecording.Text = "Delete Recording";
            this.btnDeleteRecording.UseVisualStyleBackColor = true;
            this.btnDeleteRecording.Click += new System.EventHandler(this.btnDeleteRecording_Click);
            // 
            // btnExportLogs
            // 
            this.btnExportLogs.Location = new System.Drawing.Point(30, 150);
            this.btnExportLogs.Name = "btnExportLogs";
            this.btnExportLogs.Size = new System.Drawing.Size(120, 40);
            this.btnExportLogs.TabIndex = 6;
            this.btnExportLogs.Text = "Export Logs";
            this.btnExportLogs.UseVisualStyleBackColor = true;
            this.btnExportLogs.Click += new System.EventHandler(this.btnExportLogs_Click);
            // 
            // btnImportRecordings
            // 
            this.btnImportRecordings.Location = new System.Drawing.Point(180, 150);
            this.btnImportRecordings.Name = "btnImportRecordings";
            this.btnImportRecordings.Size = new System.Drawing.Size(120, 40);
            this.btnImportRecordings.TabIndex = 7;
            this.btnImportRecordings.Text = "Import Recordings";
            this.btnImportRecordings.UseVisualStyleBackColor = true;
            this.btnImportRecordings.Click += new System.EventHandler(this.btnImportRecordings_Click);
            // 
            // btnExportRecordings
            // 
            this.btnExportRecordings.Location = new System.Drawing.Point(330, 150);
            this.btnExportRecordings.Name = "btnExportRecordings";
            this.btnExportRecordings.Size = new System.Drawing.Size(120, 40);
            this.btnExportRecordings.TabIndex = 8;
            this.btnExportRecordings.Text = "Export Recordings";
            this.btnExportRecordings.UseVisualStyleBackColor = true;
            this.btnExportRecordings.Click += new System.EventHandler(this.btnExportRecordings_Click);
            // 
            // btnSearchLogs
            // 
            this.btnSearchLogs.Location = new System.Drawing.Point(370, 480);
            this.btnSearchLogs.Name = "btnSearchLogs";
            this.btnSearchLogs.Size = new System.Drawing.Size(80, 30);
            this.btnSearchLogs.TabIndex = 9;
            this.btnSearchLogs.Text = "Search";
            this.btnSearchLogs.UseVisualStyleBackColor = true;
            this.btnSearchLogs.Click += new System.EventHandler(this.btnSearchLogs_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(30, 210);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(48, 17);
            this.lblStatus.TabIndex = 10;
            this.lblStatus.Text = "Status:";
            // 
            // listBoxCallLogs
            // 
            this.listBoxCallLogs.FormattingEnabled = true;
            this.listBoxCallLogs.ItemHeight = 16;
            this.listBoxCallLogs.Location = new System.Drawing.Point(30, 240);
            this.listBoxCallLogs.Name = "listBoxCallLogs";
            this.listBoxCallLogs.Size = new System.Drawing.Size(420, 132);
            this.listBoxCallLogs.TabIndex = 11;
            // 
            // txtSaveLocation
            // 
            this.txtSaveLocation.Location = new System.Drawing.Point(150, 380);
            this.txtSaveLocation.Name = "txtSaveLocation";
            this.txtSaveLocation.Size = new System.Drawing.Size(300, 22);
            this.txtSaveLocation.TabIndex = 12;
            // 
            // lblSaveLocation
            // 
            this.lblSaveLocation.AutoSize = true;
            this.lblSaveLocation.Location = new System.Drawing.Point(30, 380);
            this.lblSaveLocation.Name = "lblSaveLocation";
            this.lblSaveLocation.Size = new System.Drawing.Size(98, 17);
            this.lblSaveLocation.TabIndex = 13;
            this.lblSaveLocation.Text = "Save Location:";
            // 
            // comboBoxInputDevices
            // 
            this.comboBoxInputDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxInputDevices.FormattingEnabled = true;
            this.comboBoxInputDevices.Location = new System.Drawing.Point(150, 420);
            this.comboBoxInputDevices.Name = "comboBoxInputDevices";
            this.comboBoxInputDevices.Size = new System.Drawing.Size(300, 24);
            this.comboBoxInputDevices.TabIndex = 14;
            // 
            // lblInputDevices
            // 
            this.lblInputDevices.AutoSize = true;
            this.lblInputDevices.Location = new System.Drawing.Point(30, 420);
            this.lblInputDevices.Name = "lblInputDevices";
            this.lblInputDevices.Size = new System.Drawing.Size(96, 17);
            this.lblInputDevices.TabIndex = 15;
            this.lblInputDevices.Text = "Input Devices:";
            // 
            // comboBoxAudioFormats
            // 
            this.comboBoxAudioFormats.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxAudioFormats.FormattingEnabled = true;
            this.comboBoxAudioFormats.Location = new System.Drawing.Point(150, 460);
            this.comboBoxAudioFormats.Name = "comboBoxAudioFormats";
            this.comboBoxAudioFormats.Size = new System.Drawing.Size(300, 24);
            this.comboBoxAudioFormats.TabIndex = 16;
            // 
            // lblAudioFormats
            // 
            this.lblAudioFormats.AutoSize = true;
            this.lblAudioFormats.Location = new System.Drawing.Point(30, 460);
            this.lblAudioFormats.Name = "lblAudioFormats";
            this.lblAudioFormats.Size = new System.Drawing.Size(104, 17);
            this.lblAudioFormats.TabIndex = 17;
            this.lblAudioFormats.Text = "Audio Formats:";
            // 
            // trackBarVolume
            // 
            this.trackBarVolume.Location = new System.Drawing.Point(150, 500);
            this.trackBarVolume.Maximum = 100;
            this.trackBarVolume.Name = "trackBarVolume";
            this.trackBarVolume.Size = new System.Drawing.Size(300, 56);
            this.trackBarVolume.TabIndex = 18;
            this.trackBarVolume.Scroll += new System.EventHandler(this.trackBarVolume_Scroll);
            // 
            // lblVolume
            // 
            this.lblVolume.AutoSize = true;
            this.lblVolume.Location = new System.Drawing.Point(30, 500);
            this.lblVolume.Name = "lblVolume";
            this.lblVolume.Size = new System.Drawing.Size(59, 17);
            this.lblVolume.TabIndex = 19;
            this.lblVolume.Text = "Volume:";
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(150, 550);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(200, 22);
            this.txtSearch.TabIndex = 20;
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Location = new System.Drawing.Point(30, 550);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(57, 17);
            this.lblSearch.TabIndex = 21;
            this.lblSearch.Text = "Search:";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 600);
            this.Controls.Add(this.lblSearch);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.lblVolume);
            this.Controls.Add(this.trackBarVolume);
            this.Controls.Add(this.lblAudioFormats);
            this.Controls.Add(this.comboBoxAudioFormats);
            this.Controls.Add(this.lblInputDevices);
            this.Controls.Add(this.comboBoxInputDevices);
            this.Controls.Add(this.lblSaveLocation);
            this.Controls.Add(this.txtSaveLocation);
            this.Controls.Add(this.listBoxCallLogs);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnSearchLogs);
            this.Controls.Add(this.btnExportRecordings);
            this.Controls.Add(this.btnImportRecordings);
            this.Controls.Add(this.btnExportLogs);
            this.Controls.Add(this.btnDeleteRecording);
            this.Controls.Add(this.btnPlayRecording);
            this.Controls.Add(this.btnStopRecording);
            this.Controls.Add(this.btnResumeRecording);
            this.Controls.Add(this.btnPauseRecording);
            this.Controls.Add(this.btnStartRecording);
            this.Name = "MainForm";
            this.Text = "Call Recording Application";
            ((System.ComponentModel.ISupportInitialize)(this.trackBarVolume)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button btnStartRecording;
        private System.Windows.Forms.Button btnPauseRecording;
        private System.Windows.Forms.Button btnResumeRecording;
        private System.Windows.Forms.Button btnStopRecording;
        private System.Windows.Forms.Button btnPlayRecording;
        private System.Windows.Forms.Button btnDeleteRecording;
        private System.Windows.Forms.Button btnExportLogs;
        private System.Windows.Forms.Button btnImportRecordings;
        private System.Windows.Forms.Button btnExportRecordings;
        private System.Windows.Forms.Button btnSearchLogs;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ListBox listBoxCallLogs;
        private System.Windows.Forms.TextBox txtSaveLocation;
        private System.Windows.Forms.Label lblSaveLocation;
        private System.Windows.Forms.ComboBox comboBoxInputDevices;
        private System.Windows.Forms.Label lblInputDevices;
        private System.Windows.Forms.ComboBox comboBoxAudioFormats;
        private System.Windows.Forms.Label lblAudioFormats;
        private System.Windows.Forms.TrackBar trackBarVolume;
        private System.Windows.Forms.Label lblVolume;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblSearch;
    }
}
