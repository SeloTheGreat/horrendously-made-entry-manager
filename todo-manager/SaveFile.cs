using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

internal static class SaveFile
{
    const string EntryKeyValueSeparator = "<[EQS]>";
    const string NewEntrySeparator = "<[NEW]>";

    public static string SaveFileDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\HorrendouslyMadeEntryManager";
    public static string SaveFileName = "data.txt";

    public static Dictionary<string, string> SaveData = new Dictionary<string, string>();

    public static void Initialize()
    {
        if (!Directory.Exists(SaveFileDir))
            Directory.CreateDirectory(SaveFileDir);

        string fullPath = Path.Combine(SaveFileDir, SaveFileName);

        if (!File.Exists(fullPath))
        {
            File.Create(fullPath).Close();
        }
        else
        {
            FullLoad();
        }
    }

    public static void FullSave()
    {
        string fullPath = Path.Combine(SaveFileDir, SaveFileName);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found at '{fullPath}'");

        string txt = "";

        foreach (KeyValuePair<string, string> pair in SaveData)
        {
            txt += $"{pair.Key}{EntryKeyValueSeparator}{pair.Value}{NewEntrySeparator}";
        }

        File.WriteAllText(fullPath, txt);
    }

    //dangerous. may lead to full data loss
    public static void FullLoad()
    {
        if (SaveData.Count > 0)
            SaveData.Clear();

        string fullPath = Path.Combine(SaveFileDir, SaveFileName);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found at '{fullPath}'");

        string read = File.ReadAllText(fullPath);

        string[] entries = read.Split(new string[] {NewEntrySeparator}, StringSplitOptions.RemoveEmptyEntries);

        string[] _sep = { EntryKeyValueSeparator };
        foreach (string entry in entries)
        {
            string[] pair = entry.Split(_sep, StringSplitOptions.None);
            if (pair.Length != 2)
                throw new FormatException($"Malformed data pair format '{entry}'");
            SaveData.Add(pair[0], pair[1]);
        }
    }
}
