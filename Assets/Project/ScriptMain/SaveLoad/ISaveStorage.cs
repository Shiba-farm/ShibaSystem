public interface ISaveStorage
{
    void   Write(string fileName, string json);
    string Read(string fileName);
    bool   Exists(string fileName);
    void   Delete(string fileName);
}
