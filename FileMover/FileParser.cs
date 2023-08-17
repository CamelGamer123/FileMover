using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FileMover;

/// <summary>
///     Reads and parses files for the FileMover class.
/// </summary>
public class FileParser
{
    private readonly FileInfo _file;

    public FileParser(string path)
    {
        _file = new FileInfo(path);
    }

    public FileParser(FileInfo file)
    {
        _file = file;
    }

    private string FilePath => _file.FullName;

    /// <summary>
    ///     Attempts to read the file a in a variety of ways, looking for directories associated with the key "folders".
    /// </summary>
    /// <returns></returns>
    public List<DirectoryInfo> GetDirectories()
    {
        // Read the file
        string fileContents = File.ReadAllText(FilePath);

        // Create a list of directories
        List<DirectoryInfo> directories = new();

        // Attempt to parse the file as JSON
        try
        {
            // Parse the file as JSON
            JObject json = JObject.Parse(fileContents);

            // Check to ensure that the file contains the key "folders"
            if (!json.ContainsKey("folders")) throw new Exception("The file does not contain the key \"folders\".");

            // Get the folders from the JSON
            JArray folders = (JArray)json["folders"]!;

            // Add each directory to the list
            foreach (JToken folder in folders) directories.Add(new DirectoryInfo(folder.ToString()));

            // Return the list of directories
            return directories;
        }
        catch (JsonReaderException)
        {
            // Ignore the exception and continue
        }

        try
        {
            // Read the file as an XML document
            XmlDocument xml = new();
            xml.LoadXml(fileContents);

            // Get the folders from the XML
            XmlNodeList folders = xml.GetElementsByTagName("folders");

            // Add each directory to the list
            foreach (XmlNode folder in folders) directories.Add(new DirectoryInfo(folder.InnerText));

            // Return the list of directories
            return directories;
        }
        catch (XmlException)
        {
            // Ignore the exception and continue
        }

        // Split the file by newlines
        List<string> lines = fileContents.Split('\n').ToList();
        List<string> values = fileContents.Split(',').ToList();

        if (lines.Count < 1 && values.Count < 1) throw new Exception("The file does not contain any directories.");

        // Handle if the user used commas and newlines
        if (lines.Count == values.Count) lines.Clear();

        // Add each directory to the list
        foreach (string line in lines) directories.Add(new DirectoryInfo(line));
        foreach (string value in values) directories.Add(new DirectoryInfo(value));

        // Return the list of directories
        return directories;
    }
}