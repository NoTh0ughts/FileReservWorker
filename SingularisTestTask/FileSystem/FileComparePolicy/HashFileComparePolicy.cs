namespace SingularisTestTask.FileSystem.FileComparePolicy;

/// <summary> Политика сравнения файлов на основе вычисления их хеша md5 </summary>
public class HashFileComparePolicy : IFileComparePolicy
{
    public bool Equals(string file1, string file2)
    {
        return FileHelper.GetFileHash(file1) == FileHelper.GetFileHash(file2);
    }
}