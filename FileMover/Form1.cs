using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Dialogs;
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

        private List<string> _excludedFileNames = new List<string>();
        private List<string> _excludedFileExtensions = new List<string>();

        private string _destinationDirectory = "";


        private CommonOpenFileDialog _folderPicker = new CommonOpenFileDialog()
        {
            IsFolderPicker = true,
            InitialDirectory = @"C:\Users\kylep\source\repos\FileMover\FileMover\Downloads"
        };

        public MainWindow()  // Called when the window is initialized 
        {
            InitializeComponent();

            // Set the progress bar to the correct values
            fileMoveProgressBar.Minimum = 0;
            fileMoveProgressBar.Step = 1;
        }

        // Click events

        private void toolStripFileOpenButton_Click(object sender, EventArgs eventArgs)
        {
            selectSourceFilesButton_Click(sender, eventArgs);
        }

        private void toolStripFileSaveButton_Click(object sender, EventArgs eventArgs)
        {

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
                var thread = new Thread(RecursivelyScanDirectories);
                thread.Start(); // Start the thread
                // While the thread is running, display a loading message
                MessageBox.Show(@"Loading files from directory: " + _selectedDirectory);

                // Wait for the thread to finish
                thread.Join();

                var updateSourceFilesListBoxThread = new Thread(UpdateSourceFilesListBox);
                updateSourceFilesListBoxThread.Start();
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
        private void RecursivelyScanDirectories()
        {
            // Get a dictionary of all files in the selected directory recursively structured as follows:
            // Key: File name and extension Example: 'file.txt'
            // Value: Full file path Example: 'C:\Users\user\file.txt'
            // If the file name already exists in the dictionary, add (1) to the end of the key. This will be
            // incremented until a unique key is found.
            // Example: 'file.txt' -> 'file.txt(1)'
            Console.WriteLine(@"Scanning directory: " + _selectedDirectory);
            try
            {
                // Get a list of tuples containing the file name and full file path
                _sourceFiles = Directory.GetFiles(_selectedDirectory, "*", SearchOption.AllDirectories)
                                .Select(file =>
                                                new Tuple<string, string>(Path.GetFileName(file),
                                                                Path.GetFullPath(file))).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                // Request admin privileges by restarting the application with admin privileges
                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = Application.ExecutablePath,
                    Verb = "runas"
                };
                try
                {
                    Process.Start(startInfo);
                }
                catch (Exception)
                {
                    // The user refused the elevation.
                    // Do nothing and return directly ...
                }
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

            var updateSourceFilesListBoxThread = new Thread(UpdateSourceFilesListBox);
            updateSourceFilesListBoxThread.Start();
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

            // Update the source files list box
            var updateSourceFilesListBoxThread = new Thread(UpdateSourceFilesListBox);
            updateSourceFilesListBoxThread.Start();
        }

        /// <summary>
        /// // Updates the source files list box with the files that match the file names and extensions in the included
        /// file names and extensions list boxes excluding the files in the excluded file names and extensions lists
        /// </summary>
        private void UpdateSourceFilesListBox()
        {
            // Start by clearing the list of current files and creating a new list of valid files
            _includedFiles.Clear();
            var validFiles = new List<Tuple<string, string>>();

            // Iterate through each file in the source files list and check if it has been excluded
            foreach (var file in _sourceFiles)
            {
                // If the file name has been excluded, skip the file
                if (_excludedFileNames.Contains(file.Item1.Split('.')[0]))
                {
                    continue;
                }

                // If the file extension has been excluded, skip the file
                if (_excludedFileExtensions.Contains(file.Item2))
                {
                    continue;
                }

                // If the file name is included, add it to the list of valid files
                validFiles.Add(file);
            }

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
            if (regexExpression.EndsWith('|')) regexExpression = regexExpression.Remove(regexExpression.Length - 1);

            // Add the file extension separator to the regex expression
            regexExpression += ")\\.(";

            // Iterate through each file extension in the included file extensions list box and add it to the regex expression
            regexExpression = _includedFileExtensions.Aggregate(regexExpression, (current, fileExtension) => current + (fileExtension + "|"));

            // Remove the last '|' character
            if (regexExpression.EndsWith('|')) regexExpression = regexExpression.Remove(regexExpression.Length - 1);

            // Add the end of the regex expression
            regexExpression += ")";


            // Match each filename in the source files list to the regex expression, if it matches add it to the list of included files
            foreach (var file in validFiles)
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

            // Set the max of the progress bar to the number of files in the source files list box
            fileMoveProgressBar.Maximum = _includedFiles.Count;
        }

        private void sourceFilesListBox_DoubleClick(object sender, EventArgs eventArgs)
        {
            if (sourceFilesListBox.SelectedItem != null)
            {
                // Open a new file explorer window with the file selected
                Process.Start("explorer.exe", "/select, " + _includedFiles[sourceFilesListBox.SelectedIndex].Item2);
            }
        }

        private void sourceFilesListBox_RightClick(object sender, EventArgs eventArgs)
        {
            // Check if the user has selected a file
            if (sourceFilesListBox.SelectedItem == null)
            {
                return;
            }

            // Create a new context menu
            var contextMenu = new ContextMenuStrip();

            // Create a new menu item to open the file in explorer
            var openFileInExplorerMenuItem = new ToolStripMenuItem("Open in Explorer");
            openFileInExplorerMenuItem.Click += (o, args) => Process.Start("explorer.exe", "/select, " + _includedFiles[sourceFilesListBox.SelectedIndex].Item2);
            contextMenu.Items.Add(openFileInExplorerMenuItem);

            // Create a new menu item to open the file in the default program
            var openFileInDefaultProgramMenuItem = new ToolStripMenuItem("Open in Default Program");
            openFileInDefaultProgramMenuItem.Click += (o, args) => Process.Start(_includedFiles[sourceFilesListBox.SelectedIndex].Item2);
            contextMenu.Items.Add(openFileInDefaultProgramMenuItem);

            // Create a new menu item to exclude the file from the list
            var excludeFileMenuItem = new ToolStripMenuItem("Exclude File");
            excludeFileMenuItem.Click += (o, args) =>
            {
                // Remove the file from the list of included files
                _includedFiles.RemoveAt(sourceFilesListBox.SelectedIndex);

                // Remove the file from the list box
                sourceFilesListBox.Items.RemoveAt(sourceFilesListBox.SelectedIndex);
            };
            contextMenu.Items.Add(excludeFileMenuItem);

            // Create a new menu item to exclude all files with the same name
            var excludeAllFilesWithSameNameMenuItem = new ToolStripMenuItem("Exclude All Files With Same Name");
            excludeAllFilesWithSameNameMenuItem.Click += (o, args) =>
            {
                // Get the file name of the selected file
                var fileName = _includedFiles[sourceFilesListBox.SelectedIndex].Item1;

                // Add the file to the list of excluded files
                _excludedFileNames.Add(fileName.Split('.')[0]);

                // Refresh the source files list box
                UpdateSourceFilesListBox();
            };
            contextMenu.Items.Add(excludeAllFilesWithSameNameMenuItem);

            // Create a new menu item to exclude all files with the same extension
            var excludeAllFilesWithSameExtensionMenuItem = new ToolStripMenuItem("Exclude All Files With Same Extension");
            excludeAllFilesWithSameExtensionMenuItem.Click += (o, args) =>
            {
                // Get the file extension of the selected file
                var fileExtension = _includedFiles[sourceFilesListBox.SelectedIndex].Item2;

                // Add the file to the list of excluded files
                _excludedFileExtensions.Add(fileExtension);

                // Refresh the source files list box
                UpdateSourceFilesListBox();
            };
            contextMenu.Items.Add(excludeAllFilesWithSameExtensionMenuItem);

            // If the user has selected a file, show the context menu
            contextMenu.Show(Cursor.Position);
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            // This is a dual-purpose button, it can be used to select the destination directory or to start and stop the file moving process
            // Create the file moving thread
            var moveFilesThread = new Thread(MoveSelectedFiles);
            
            // Check if the sourceFilesList
            if (_includedFiles.Count < 1)
            {
                // If the source files list is empty, display an error message
                MessageBox.Show(@"No files to move selected. Please select files to move.");
            }
            
            else if (_destinationDirectory == "")
            {
                // If the destination directory has not been selected, display an error message
                MessageBox.Show(@"No destination directory selected. Please select a destination directory.");
            }
            // Start the file moving process
            moveFilesThread.Start();
            startButton.Text = @"Stop";
            
            if (moveFilesThread.IsAlive)
            {
                // Stop the file moving process
                moveFilesThread.Interrupt();
                
                // Reset the progress bar
                fileMoveProgressBar.Value = 0;
                
                // Display a message to the user
                MessageBox.Show(@"File moving process stopped.");
                
                // Reset the button text
                startButton.Text = @"Start";
            }
        }

        private void MoveSelectedFiles()
        {
            // Loop through all files in the source files list box
            foreach (var file in _includedFiles)
            {
                // Create a new thread to move the file
                var thread = new Thread(() => MoveFile(file.Item2));
                thread.Start(); // Start the thread
            }
        }

        private void MoveFile(string sourcePath)
        {
            // Move the file to the destination directory
            FileInfo fileInfo = new FileInfo(sourcePath);
            fileInfo.MoveTo(_destinationDirectory + "\\" + fileInfo.Name);

            // Update the progress bar
            fileMoveProgressBar.PerformStep();
        }

        private void selectDestinationDirectoryButton_Click(object sender, EventArgs e)
        {
            // Check if the user has selected a destination directory
            if (_destinationDirectory == "")
            {
                // If the user has not selected a destination directory, open the folder picker
                if (_folderPicker.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    // Set the destination directory to the selected directory
                    _destinationDirectory = _folderPicker.FileName;
                }
            }
        }
    }
}