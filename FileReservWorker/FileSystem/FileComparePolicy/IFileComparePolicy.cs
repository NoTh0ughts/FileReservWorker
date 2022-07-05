namespace FileReservWorker.FileSystem.FileComparePolicy;

/// <summary> Интерфейс политики сравнения файлов </summary>
public interface IFileComparePolicy
{
    /// <summary> Сравнивает файлы </summary>
    /// <returns> true - файлы идентичны, false - файлы имеют различия </returns>
    bool Equals(string file1, string file2);
}