using System.Globalization;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Text.RegularExpressions.Generated;
using System.Text.RegularExpressions;

namespace FileMover
{
    public partial class MainWindow : Form

    {
        private string _selectedDirectory = "";
        private List<Tuple<string, string>> _sourceFiles = new List<Tuple<string, string>>();  // NOT THE SAME AS THE LIST BOX, THIS IS A LIST OF ALL FILES IN THE SELECTED DIRECTORY
        private List<Tuple<string, string>> _includedFiles = new List<Tuple<string, string>>();  // This the list of files that will be moved to the destination directory

        private List<string> _includedFileNames = new List<string>();
        private List<string> _includedFileExtensions = new List<string>();

        private string _destinationDirectory = "";

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
            selectSourceFilesButton_Click(sender, eventArgs);
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

        // Main UI Elements

        private void selectSourceFilesButton_Click(object sender, EventArgs eventArgs)
        {
            if (_folderPicker.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _selectedDirectory = _folderPicker.FileName;
                // Create a new thread to scan the selected directory for files
                var thread = new Thread(() => RecursivelyScanDirectories(_folderPicker.FileName));
                thread.Start();
            }
        }

        private void actionsListSaveLocationChangeButton_Click(object sender, EventArgs eventArgs)
        {
            if (_folderPicker.ShowDialog() == CommonFileDialogResult.Ok)
            {
                MessageBox.Show(@"You selected: " + _folderPicker.FileName);
            }
        }
        /// <summary>
        /// Recursively scan all directories in the given directory and add them to the list box. This is a separate method
        /// to allow for the process to be run asynchronously.
        /// </summary>
        /// <param name="directory">The directory to scan</param>
        private void RecursivelyScanDirectories(string directory)
        {
            // Get a dictionary of all files in the selected directory recursively structured as follows:
            // Key: File name and extension Example: 'file.txt'
            // Value: Full file path Example: 'C:\Users\user\file.txt'
            // If the file name already exists in the dictionary, add (1) to the end of the key. This will be
            // incremented until a unique key is found.
            // Example: 'file.txt' -> 'file.txt(1)'
            Console.WriteLine(@"Scanning directory: " + directory);
            try
            {
                // Get a list of tuples containing the file name and full file path
                _sourceFiles = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                                .Select(file =>
                                                new Tuple<string, string>(Path.GetFileName(file),
                                                                Path.GetFullPath(file))).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(@"Access to the directory was denied.");
                return;
            }
        }

        private void addFileNameButton_Click(object sender, EventArgs e)
        {
            // Check if the user has written a filename in the text box
            if (fileNameInputTextBox.Text == "")
            {
                MessageBox.Show(@"Please enter a file name.");
                return;
            }

            // Check if the file name is valid
            if (fileNameInputTextBox.Text.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                MessageBox.Show(@"The file name contains invalid characters.");
                return;
            }

            // Check if the file name already exists in the list of file names
            if (_includedFileNames.Contains(fileNameInputTextBox.Text))
            {
                MessageBox.Show(@"The file name already exists in the list box.");
                return;
            }

            // Add the file name to the list of file names
            _includedFileNames.Add(fileNameInputTextBox.Text);
            includedFileNamesListBox.Items.Add(fileNameInputTextBox.Text);
        }

        private void addFileExtensionButton_Click(object sender, EventArgs e)
        {
            // Check if the user has written a file extension in the text box
            if (fileNameExtensionTextBox.Text == "")
            {
                MessageBox.Show(@"Please enter a file extension.");
                return;
            }

            // Check if the file extension is valid
            if (fileNameExtensionTextBox.Text.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                MessageBox.Show(@"The file extension contains invalid characters.");
                return;
            }

            // Check if the file extension already exists in the list of file extensions
            if (_includedFileExtensions.Contains(fileNameExtensionTextBox.Text))
            {
                MessageBox.Show(@"The file extension already exists in the list box.");
                return;
            }

            // Add the file extension to the list
            _includedFileExtensions.Add(fileNameExtensionTextBox.Text);
            includedFileExtensionsListBox.Items.Add(fileNameExtensionTextBox.Text);
        }

        private void UpdateSourceFilesListBox()
        {
            // Updates the source files list box with the files that match the file names and extensions in the included file names and extensions list boxes

            // Start by clearing the list of current files 
            _includedFiles.Clear();
            
            // Generate the regex expression to match the full file name to 
            var regexExpression = "(";
            if (optionsCaseInsensitiveCheckbox.Checked)
            {
                // Add the case insensitive flag to the regex expression
                regexExpression += "?i";
            }

            // Iterate through each file name in the included file names list box and add it to the regex expression
            foreach (var fileName in _includedFileNames)
            {
                regexExpression += fileName + "|";
            }
            
            // Remove the last '|' character
            regexExpression = regexExpression.Remove(regexExpression.Length - 1);
            
            // Add the file extension separator to the regex expression
            regexExpression += ")\\.(";
            
            // Iterate through each file extension in the included file extensions list box and add it to the regex expression
            foreach (var fileExtension in _includedFileExtensions)
            {
                regexExpression += fileExtension + "|";
            }
            
            // Remove the last '|' character
            regexExpression = regexExpression.Remove(regexExpression.Length - 1);
            
            // Add the end of the regex expression
            regexExpression += ")";
            
            // Match each filename in the source files list to the regex expression, if it matches add it to the list of included files
            foreach (var file in _sourceFiles)
            {
                if (Regex.IsMatch(file.Item1, regexExpression))
                {
                    _includedFiles.Add(file);
                }
            }
            
            // Add the files to the list box
            sourceFilesListBox.Items.Clear();  // Do not clear the list box until the new list is ready to be added

            // Iterate through and add each file to the source files list box 
            foreach (var file in _includedFiles)
            {
                sourceFilesListBox.Items.Add(file.Item1);
            }
        }
    }
}