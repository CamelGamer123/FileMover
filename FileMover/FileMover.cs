using System.Text.RegularExpressions;

namespace FileMover;

/// <summary>
/// Used for dynamically analysing and moving files and directories. 
/// </summary>
public class FileMover
{
    private readonly List<Tuple<string, string, string>> _sourceFiles = new(); // Initialise this as empty 
    // ReSharper disable once MemberCanBePrivate.Global  // This is because the SelectedFiles will most definitely be accessed publicly
    public List<Tuple<string, string, string>> SelectedFiles = new();
    
    public List<string> IncludedNameCriteria = new();
    public List<string> IncludedExtensionCriteria = new();

    public List<string> ExcludedNameCriteria = new();
    public List<string> ExcludedExtensionCriteria = new();

    public DirectoryInfo DestinationDirectory;
    
    private List<Thread> _folderScanningThreads = new();

    private readonly bool _parallel;

    private readonly object _globalLock = new();  // Lock object for thread-safety. This will slow down code but without it scanning multiple directories would not work
    private readonly object _sourceFilesLock = new();
    private readonly object _criteriaLock = new();
    
    // TODO: Add checks to make sure that the class cannot be updating the source files while the criteria or the source files are being modified
    // TODO: Add a way to exclude certain sub-directories (potentially recursively)
    // TODO: Optimise the code in any way possible
    // TODO: Document all of this in a separate document using a proper documentation standard 
    
    /// <summary>
    /// Initializes the FileMover class.
    /// </summary>
    /// <param name="parallel">Use multi-threading</param>
    /// <param name="destinationDirectory"></param>
    public FileMover(string destinationDirectory, bool parallel = true)
    {
       _parallel = parallel;
       DestinationDirectory = new DirectoryInfo(destinationDirectory);
    }
    
    /// <summary>
    /// Initializes the FileMover class.
    /// </summary>
    /// <param name="destinationDirectory"></param>
    /// <param name="parallel"></param>
    public FileMover(DirectoryInfo destinationDirectory, bool parallel = true)
    {
        _parallel = parallel;
        DestinationDirectory = destinationDirectory;
    }
    
    public FileMover(bool parallel = true)
    {
        _parallel = parallel;
        DestinationDirectory = new DirectoryInfo(@"C:\Users");  // This is a temporary fix for the DestinationDirectory not being set
    }
    
    /// <summary>
    /// Opens a folder and recursively scans for all data inside it. 
    /// </summary>
    /// <param name="folderPath">The path to the folder</param>
    public void OpenFolder(string folderPath)
    {
        // This is here to allow string paths to be passed instead of having to manually create DirectoryInfo paths 
        OpenFolder(new DirectoryInfo(folderPath));
    }
    
    /// <summary>
    /// Opens a folder and recursively scans for all data inside of it. 
    /// </summary>
    /// <param name="directory"></param>
    public void OpenFolder(DirectoryInfo directory)
    {
        if (!directory.Exists) throw new DirectoryNotFoundException();
        
        // Get all files inside the directory
        List<Tuple<string, string, string>> files = directory.GetFiles("*", SearchOption.AllDirectories).Select(file => 
                        new Tuple<string, string, string>(file.FullName, file.Name, file.Extension)).ToList();

        lock (_sourceFilesLock)
        {
            // Add all files in the list to the SourceFiles variable
            _sourceFiles.AddRange(files);
        }
    }


    public void OpenFolders(List<string> folderPaths)
    {
        List<DirectoryInfo> folders = new List<DirectoryInfo>();
        
        // TODO: Convert this to a LINQ expression for release
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (string folder in folderPaths) {
            folders.Add(new DirectoryInfo(folder));
        }
        
        OpenFolders(folders);
    }

    public void OpenFolders(List<DirectoryInfo> directories)
    {
        if (!_parallel)
        {
            foreach (DirectoryInfo directory in directories)
            {
                OpenFolder(directory);
            }
        }
        
        // TODO: Convert this to a LINQ expression for release
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (DirectoryInfo directory in directories)
        {
            Thread thread = new(() => OpenFolder(directory));  // This is hard to debug, but I don't know of any better ways to do it.
            _folderScanningThreads.Add(thread);
            thread.Start();
        }
    }
    
    /// <summary>
    /// Updates the selected files list from the selection criteria. Use `await Task.Run()` for this method as it is CPU bound.
    /// </summary>
    public async Task UpdateSelectedFiles(bool caseInsensitive)
    {
        // Start by clearing the selected files
        SelectedFiles.Clear();

        List<Tuple<string, string, string>> validFiles = new();

        List<string> regexList = new();  // This is here to reduce string reassignment, as it is slow
        
        // Iterate through each file in the source files list and check if it has been excluded
        // TODO: Test if surrounding the whole foreach loop in a lock is faster than attaining a releasing the lock for each iteration
        lock (_sourceFilesLock) 
        {
            // TODO: Convert this to a LINQ expression before release
            foreach (Tuple<string, string, string> file in _sourceFiles)
            {
                lock (_criteriaLock)
                {
                    // If the file name has been excluded, skip the file
                    if (ExcludedNameCriteria.Contains(file.Item2.Split('.')[0]))
                    {
                        continue;
                    }

                    // If the file extension has been excluded, skip the file
                    if (ExcludedExtensionCriteria.Contains(file.Item2))
                    {
                        continue;
                    }
                }

                // If the file name is included, add it to the list of valid files
                validFiles.Add(file);
            }
        }

        // Generate the regex expression to match the full file name to 
        regexList.Add("(");
        if (caseInsensitive)
        {
            // Add the case insensitive flag to the regex expression
            regexList.Add("?i");
        }

        // Iterate through each file name in the included file names list box and add it to the regex expression
        // TODO: Convert this to a LINQ expression before release
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        lock (_criteriaLock)
        {
            foreach (string fileName in IncludedNameCriteria)
            {
                regexList.Add(fileName + "|"); // TODO: Test this speed vs adding the filename and then adding the | in the next row
            }
        }

        // Remove the last '|' character
        if (regexList.Last() == "|") regexList.RemoveAt(regexList.Count - 1); // I think the - 1 is needed here? TODO: Test this

        // Add the file extension separator to the regex expression
        regexList.Add(")\\.(");

        // Iterate through each file extension in the included file extensions list box and add it to the regex expression
        // TODO: this to a LINQ expression before release
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        lock (_criteriaLock)
        {
            foreach (string includedExtension in IncludedExtensionCriteria)
            {
                regexList.Add(includedExtension + "|");
            }
        }

        if (regexList.Last() == "|") regexList.RemoveAt(regexList.Count - 1); // Remove the last '|' character
        regexList.Add(")"); // Add the end of the regex expression

        // Convert the list of regex data to a valid regex string
        string regexExpression = string.Join("", regexList);  
        
        // Match each filename in the source files list to the regex expression, if it matches add it to the list of included files
        // TODO: Convert this to a LINQ expression before release
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (Tuple<string, string, string> file in validFiles)
        {
            if (Regex.IsMatch(file.Item2, regexExpression))
            {
                SelectedFiles.Add(file);
            }
        }
    } 
}