using FileReservWorker.FileSystem;
using FileReservWorker.FileSystem.FilenamePolicy;

namespace FileReservWorker.ScheduleWorker;

/// <summary>
/// Объект фиксации
/// </summary>
public class Commit
{
    public string DestinationRoot { get; init; }
    public string SourceRoot { get; init; }
    public string CommitDirectory { get; init; }
    
    public List<string> ChangedFiles { get; set; }
    
    private Commit(){}
    
    /// <summary>  Фиксирует все изменения в папку <see cref="CommitDirectory"/> </summary>
    public void Push()
    {
        foreach (var file in ChangedFiles)
        {
            FileHelper.CopyFile(SourceRoot, file, CommitDirectory);
        }
    }
    
    /// <summary> Создает объект фиксации и дирректории для нее </summary>
    /// <param name="source"> Источник данных </param>
    /// <param name="destination"> Приемник данных </param>
    /// <param name="filenameGenerationPolicy"> Политика генерации имени файлов </param>
    /// <returns> Новый объект фиксации </returns>
    public static Commit Create(string source, string destination, IFilenameGenerationPolicy filenameGenerationPolicy)
    {
        var commitName = FileHelper.IsPrimaryResolve(destination)
            ? filenameGenerationPolicy.GetPrimary()
            : filenameGenerationPolicy.Get();
        
        var commitDirectory = Directory.CreateDirectory(Path.Combine(destination, commitName)).FullName;
        
        var newCommit = new Commit
        {
            DestinationRoot = destination,
            SourceRoot = source,
            CommitDirectory = commitDirectory
        };
        return newCommit;
    }
}