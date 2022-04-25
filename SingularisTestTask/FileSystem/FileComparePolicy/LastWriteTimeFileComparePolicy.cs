namespace SingularisTestTask.FileSystem.FileComparePolicy;

/// <summary> Политика сравнения файлов на основе даты последнего изменения </summary>
public class LastWriteTimeFileComparePolicy : IFileComparePolicy
{
    public bool Equals(string file1, string file2)
    {
        var fileInfo1 = new FileInfo(file1);
        var fileInfo2 = new FileInfo(file2);

        return fileInfo1.LastWriteTimeUtc == fileInfo2.LastWriteTimeUtc;
    }
}