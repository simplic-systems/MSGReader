﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using MsgReader;
using MsgViewer.Helpers;
using MsgViewer.Properties;

/*
   Copyright 2013-2015 Kees van Spelde

   Licensed under The Code Project Open License (CPOL) 1.02;
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

     http://www.codeproject.com/info/cpol10.aspx

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

namespace MsgViewer
{
    public partial class ViewerForm : Form
    {
        readonly List<string> _tempFolders = new List<string>(); 

        public ViewerForm()
        {
            InitializeComponent();
        }

        private void ViewerForm_Load(object sender, EventArgs e)
        {
            WindowPlacement.SetPlacement(Handle, Settings.Default.Placement);
            Closing += ViewerForm_Closing;
        }

        void ViewerForm_Closing(object sender, EventArgs e)
        {
            Settings.Default.Placement = WindowPlacement.GetPlacement(Handle);
            Settings.Default.Save();
            foreach (var tempFolder in _tempFolders)
            {
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
            }
        }

        public string GetTemporaryFolder()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private void BackButton_Click_1(object sender, EventArgs e)
        {
            webBrowser1.GoBack();
        }

        private void ForwardButton_Click_1(object sender, EventArgs e)
        {
            webBrowser1.GoForward();
        }

        private void PrintButton_Click(object sender, EventArgs e)
        {
            webBrowser1.ShowPrintDialog();
        }

        private void webBrowser1_Navigated_1(object sender, WebBrowserNavigatedEventArgs e)
        {
            StatusLabel.Text = e.Url.ToString();
        }

        private void SaveAsTextButton_Click(object sender, EventArgs e)
        {
            // Create an instance of the save file dialog box.
            var saveFileDialog1 = new SaveFileDialog
            {
                // ReSharper disable once LocalizableElement
                Filter = "TXT Files (.txt)|*.txt",
                FilterIndex = 1
            };

            if (Directory.Exists(Settings.Default.SaveDirectory))
                saveFileDialog1.InitialDirectory = Settings.Default.SaveDirectory;
            
            // Process input if the user clicked OK.
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.SaveDirectory = Path.GetDirectoryName(saveFileDialog1.FileName);
                var htmlToText = new HtmlToText();
                var text = htmlToText.Convert(webBrowser1.DocumentText);
                File.WriteAllText(saveFileDialog1.FileName, text);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Create an instance of the open file dialog box.
            var openFileDialog1 = new OpenFileDialog
            {
                // ReSharper disable once LocalizableElement
                Filter = "E-mail|*.msg;*.eml",
                FilterIndex = 1,
                Multiselect = false
            };

            if (Directory.Exists(Settings.Default.InitialDirectory))
                openFileDialog1.InitialDirectory = Settings.Default.InitialDirectory;

            // Process input if the user clicked OK.
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.InitialDirectory = Path.GetDirectoryName(openFileDialog1.FileName);
                OpenFile(openFileDialog1.FileName);
            }
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        #region OpenFile
        /// <summary>
        /// Opens the selected MSG of EML file
        /// </summary>
        /// <param name="fileName"></param>
        private void OpenFile(string fileName)
        {
            // Open the selected file to read.
            string tempFolder = null;

            try
            {
                tempFolder = GetTemporaryFolder();
                _tempFolders.Add(tempFolder);

                var msgReader = new Reader();
                //msgReader.SetCulture("nl-NL");
                //msgReader.SetCulture("de-DE");
                var files = msgReader.ExtractToFolder(fileName, tempFolder, genereateHyperlinksToolStripMenuItem.Checked);

                var error = msgReader.GetErrorMessage();

                if (!string.IsNullOrEmpty(error))
                    throw new Exception(error);

                if (!string.IsNullOrEmpty(files[0]))
                    webBrowser1.Navigate(files[0]);

                FilesListBox.Items.Clear();

                foreach (var file in files)
                    FilesListBox.Items.Add(file);
            }
            catch (Exception ex)
            {
                if (tempFolder != null && Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);

                MessageBox.Show(ex.Message);
            }
        }
        #endregion
    }
}
