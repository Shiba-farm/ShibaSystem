using UnityEngine;

// public class SteamCloudStorage: ISaveStorage
// {
//     public void Write(string fileName, string json)
//     {
//         byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
//         SteamRemoteStorage.FileWrite(fileName, bytes);  // writes to Steam Cloud directly
//     }

//     public string Read(string fileName)
//     {
//         byte[] bytes = SteamRemoteStorage.FileRead(fileName);
//         return System.Text.Encoding.UTF8.GetString(bytes);
//     }

//     public bool Exists(string fileName)
//         => SteamRemoteStorage.FileExists(fileName);

//     public void Delete(string fileName)
//         => SteamRemoteStorage.FileDelete(fileName);
// }
