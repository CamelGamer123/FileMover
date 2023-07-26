using System.Globalization;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace FileMover
{
    public partial class MainWindow : Form

    {
        private CommonOpenFileDialog _folderPicker = new CommonOpenFileDialog()
        {
            IsFolderPicker = true,
            InitialDirectory = "C:\\Users"
        };

        public MainWindow()  // Called when the window is initialized 
        {
            InitializeComponent();
            // For some reason i need to manually set the size and contents of all elements

        }

        // Click events 

        private void toolStripFileButton_Click(object sender, EventArgs eventArgs)
        {
            Console.WriteLine(@"toolStripFileButton Clicked");
        }

        private void toolStripFileOpenButton_Click(object sender, EventArgs eventArgs)
        {

        }

        private void toolStripFileSaveButton_Click(object sender, EventArgs eventArgs)
        {

        }

        private void toolStripAnalyzeButton_Click(object sender, EventArgs eventArgs)
        {
            Console.WriteLine(@"toolStripAnalyzeButton Clicked");
        }

        private void toolStripAnalyzeFileCountButton_Click(object sender, EventArgs eventArgs)
        {

        }

        private void toolStripAnalyzeFolderCountButton_Click(object sender, EventArgs eventArgs)
        {

        }

        private void toolStripAnalyzeListFilesButton_Click(object sender, EventArgs eventArgs)
        {

        }

        private void toolStripAnalyzeListFoldersButton_Click(object sender, EventArgs eventArgs)
        {

        }

        private void toolStripHelpButton_Click(object sender, EventArgs eventArgs)
        {
            Console.WriteLine(@"toolStripHelpButton Clicked");
        }

        private void toolStripHelpFeatureRequestButton_Click(object sender, EventArgs eventArgs)
        {

        }

        private void toolStripHelpInfoButton_Click(object sender, EventArgs eventArgs)
        {

        }

        private void selectSourceFilesButton_Click(object sender, EventArgs eventArgs)
        {
            if (_folderPicker.ShowDialog() == CommonFileDialogResult.Ok)
            {
                MessageBox.Show(@"You selected: " + _folderPicker.FileName);
            }
        }

        private void selectFilesDialogue_FileOk(object sender, System.ComponentModel.CancelEventArgs eventArgs)
        {
            // Get the file from the dialog
            var file = selectFilesDialouge.FileName;
            Console.WriteLine(file);
        }

        private void actionsListSaveLocationChangeButton_Click(object sender, EventArgs eventArgs)
        {
            if (_folderPicker.ShowDialog() == CommonFileDialogResult.Ok)
            {
                MessageBox.Show(@"You selected: " + _folderPicker.FileName);
            }
        }
    }
}