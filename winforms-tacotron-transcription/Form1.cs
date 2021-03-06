﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.IO;
using System.Diagnostics;
using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Cloud.SDK.Core.Http;
using IBM.Watson.SpeechToText.v1.Model;
using System.Net.Http;
using IBM.Watson.SpeechToText.v1;
using NAudio.Wave;

namespace winforms_tacotron_transcription
{
    public partial class Form1 : Form
    {
        public Dictionary<String, String> transcriptions = new Dictionary<string, string>();
        public int current_line_number = 0;
        public string folder_path = "";
        public SoundPlayer player;
        public bool loaded_shit = false;
        public Form1()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            player = new SoundPlayer();
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void PlayAudioButton_Click(object sender, EventArgs e)
        {
            player.SoundLocation = folder_path + "\\" + transcriptions.ElementAt(current_line_number).Key;
            player.Load();
            player.Play();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void NewTranscriptionButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowser = new OpenFileDialog();
            folderBrowser.ValidateNames = false;
            folderBrowser.CheckFileExists = false;
            folderBrowser.CheckPathExists = true;
            folderBrowser.FileName = "Navigate to folder with WAV files";
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                string folderPath = Path.GetDirectoryName(folderBrowser.FileName);
                folder_path = folderPath;
                string[] files = Directory.GetFiles(folderPath, "*.wav", SearchOption.TopDirectoryOnly);
                if (files.Length == 0)
                    return;
                transcriptions = new Dictionary<string, string>();
                foreach (string item in files)
                {
                    transcriptions.Add(Path.GetFileName(item), "");
                }
                current_line_number = 0;
                SaveTranscriptionButton.Enabled = true;
                PlayAudioButton.Enabled = true;
                IBMButton.Enabled = true;
                NextLineButton.Enabled = true;
                PreviousLineButton.Enabled = true;
                TranscriptionBox.Enabled = true;
                WavName.Enabled = true;
                loaded_shit = true;
                if (AutoSPT.Checked)
                {
                    IBM_audio_guess();
                }
                ChangeLineInBox();
                UpdateProgressBar();
                SaveTranscriptionLine();
            }
        }
        public void SaveTranscriptionLine()
        {
            transcriptions[transcriptions.ElementAt(current_line_number).Key] = TranscriptionBox.Text;
        }
        public void ChangeLine(bool up_or_down)
        {
            SaveTranscriptionLine();
            if (up_or_down)
            {
                if((current_line_number + 1) < transcriptions.Count())
                {
                    current_line_number++;
                    ChangeLineInBox();
                    UpdateProgressBar();
                }
            }
            else
            {
                if (current_line_number > 0)
                {
                    current_line_number--;
                    ChangeLineInBox();
                    UpdateProgressBar();
                }
            }
            if (AutoSPT.Checked && TranscriptionBox.TextLength == 0) // don't speech to text already transcribed lines
            {
                IBM_audio_guess();
            }
        }
        public void UpdateProgressBar()
        {
            ProgressBarTranscripts.Maximum = transcriptions.Count();
            ProgressBarTranscripts.Value = current_line_number;
            int current_num = ProgressBarTranscripts.Value + 1;
            int max_num = transcriptions.Count();
            ProgressLabel.Text = "Transcription Progress: " + current_num.ToString() + " of " + max_num.ToString();
        }
        public void ChangeLineInBox()
        {
            TranscriptionBox.Text = transcriptions.ElementAt(current_line_number).Value;
            WavName.Text = transcriptions.ElementAt(current_line_number).Key;
        }

        private void NextLineButton_Click(object sender, EventArgs e)
        {
            ChangeLine(true);
        }

        private void PreviousLineButton_Click(object sender, EventArgs e)
        {
            ChangeLine(false);
        }

        private void LoadTranscriptionButton_Click(object sender, EventArgs e)
        {

        }

        private void SaveTranscriptionButton_Click(object sender, EventArgs e)
        {
            SaveTranscriptionLine();
            save_file();
        }
        public void save_file()
        {
            Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    using (StreamWriter file = new StreamWriter(myStream))
                        foreach (var entry in transcriptions)
                            file.WriteLine("wavs/out/{0}|{1};", entry.Key, entry.Value);
                    myStream.Close();
                }
            }
        }
        public void IBM_audio_guess()
        {
            if(IBM_APIkey.TextLength == 0 || IBM_URL.TextLength == 0)
            {
                return;
            }
            IamAuthenticator authenticator = new IamAuthenticator(
                apikey: IBM_APIkey.Text);

            SpeechToTextService service = new SpeechToTextService(authenticator);
            service.SetServiceUrl(IBM_URL.Text);
            var model_to_use = "en-US_BroadbandModel";
            using (var reader = new WaveFileReader(folder_path + "\\" + transcriptions.ElementAt(current_line_number).Key))
            {
                if(reader.WaveFormat.SampleRate < 16000)
                {
                    model_to_use = "en-US_NarrowbandModel";
                }
            }

            DetailedResponse<SpeechRecognitionResults> result = service.Recognize(
                audio: File.ReadAllBytes(folder_path + "\\" + transcriptions.ElementAt(current_line_number).Key),
                contentType: "audio/wav",
                profanityFilter: false,
                model: model_to_use
            );
            SpeechRecognitionResults results = result.Result;
            SpeechRecognitionResult final_result = results.Results[0];
            SpeechRecognitionAlternative real_result = final_result.Alternatives[0];
            TranscriptionBox.Text = real_result.Transcript;
            SaveTranscriptionLine();
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.PageDown && loaded_shit)
            {
                ChangeLine(true);
                return true;
            }
            if(keyData == Keys.PageUp && loaded_shit)
            {
                ChangeLine(false);
                return true;
            }
            if (keyData == Keys.End && loaded_shit)
            {
                player.SoundLocation = folder_path + "\\" + transcriptions.ElementAt(current_line_number).Key;
                player.Load();
                player.Play();
                return true;
            }
            if(keyData == (Keys.Control | Keys.S) && loaded_shit)
            {
                SaveTranscriptionLine();
                save_file();
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void IBMButton_Click(object sender, EventArgs e)
        {
            IBM_audio_guess();
        }
    }
}
