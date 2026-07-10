using System.IO;
using UnityEngine;

public class LocalFileStorage: ISaveStorage
{
    private readonly string _basePath;

    public LocalFileStorage()
    {
        _basePath = Application.persistentDataPath;
    }

    public void Write(string fileName, string json)
        {File.WriteAllText(Path.Combine(_basePath, fileName), json);
        Debug.Log(Application.persistentDataPath);}

    public string Read(string fileName)
        => File.ReadAllText(Path.Combine(_basePath, fileName));

    public bool Exists(string fileName)
        => File.Exists(Path.Combine(_basePath, fileName));

    public void Delete(string fileName)
        => File.Delete(Path.Combine(_basePath, fileName));
}
