namespace P2PNode.Services;

public static class FileService
{
    public static void CreateNodeFolder(int port)
    {
        string resourcesFolder = Path.Combine(Environment.CurrentDirectory, "Resources", port.ToString());

        if (Directory.Exists(resourcesFolder))
        {
            Directory.Delete(resourcesFolder, true);
        }

        Directory.CreateDirectory(resourcesFolder);
    }

    public static string SearchFile(int port, string fileName)
    {
        string filePath = Path.Combine(Environment.CurrentDirectory, "Resources", port.ToString(), fileName);

        string fileContent = "";

        if (File.Exists(filePath))
        {
            fileContent = File.ReadAllText(filePath);
        }

        return fileContent;
    }

    public static void SaveFile(int port, string fileName, string fileContent)
    {
        string filePath = Path.Combine(Environment.CurrentDirectory, "Resources", port.ToString(), fileName);

        File.WriteAllText(filePath, fileContent);

        Console.WriteLine($"File {fileName} saved");
    }
}

