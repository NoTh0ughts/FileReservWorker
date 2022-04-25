using SingularisTestTask.FileSystem.FileComparePolicy;

namespace SingularisTestTask.ScheduleWorker;

/// <summary> Выполняет поиск изменений, формируя объект фиксацию </summary>
public static class Repository
{
    /// <summary> Формирует фиксацию, производит поиск всех изменений </summary>
    /// <param name="commit"> объект фиксации </param>
    /// <param name="comparePolicy"> политика сравнения файлов </param>
    /// <returns></returns>
    public static Commit MakeResolve(Commit commit, IFileComparePolicy comparePolicy)
    {
        return GetChangedFilesList(commit, comparePolicy);
    }

    /// <summary> Производит поиск всех файлов, учитывая последние изменения </summary>
    /// <param name="from"> Источник файлов </param>
    /// <param name="commitRoot"> Корень фиксации </param>
    /// <returns> Словарь типа (string относительный путь, string абсолютный путь) </returns>
    private static Dictionary<string, string> GetVersionedFilesFromDestination(string from, string commitRoot)
    {
        // Получаем все версионные файлы в папке назначения, отсортированные по имени (дате изменения)
        var destinationFiles = GetAllFilesOrderedByChanges(from);
        
        if (destinationFiles.Any() == false) return new Dictionary<string, string>();

        //                                     Relative Absolute
        var uniqVersionedFiles = new Dictionary<string, string>();
        
        // Перебираем версионные файлы, если такой уже существует, то заменяем его
        // Такое возможно, только если файлы отсортированны 
        foreach (var file in destinationFiles)
        {
            var relativePathToFile = Path.GetRelativePath(commitRoot, file);
            if (uniqVersionedFiles.ContainsKey(relativePathToFile)) // обновление уже существ.
            {
                uniqVersionedFiles[relativePathToFile] = file;
            }
            else // Добавление нового
            {
                uniqVersionedFiles.Add(relativePathToFile, file);
            }
        }

        return uniqVersionedFiles;
    }

    private static string[] GetAllFilesOrderedByChanges(string from)
    {
        return Directory.GetFileSystemEntries(from, "*", SearchOption.AllDirectories)
                .Where(x => (new FileInfo(x).Attributes & FileAttributes.Directory) != FileAttributes.Directory)
                .OrderBy(x => x)
                .ToArray();
    }

    /// <summary>
    /// Создает список изменений для фиксации,
    /// Проверяет с помощью политики сравнения файлы, если они были изменены с даты последней фиксации,
    /// то добавляет их в результат 
    /// </summary>
    /// <param name="commit"> Объект фиксации </param>
    /// <param name="policy"> Политика сравнения файлов </param>
    /// <returns></returns>
    private static Commit GetChangedFilesList(Commit commit, IFileComparePolicy policy)
    {
        var sourceFiles = GetAllFilesOrderedByChanges(commit.SourceRoot);
        var fixedFiles = GetVersionedFilesFromDestination(commit.DestinationRoot, commit.CommitDirectory);

        commit.ChangedFiles = new List<string>();
        
        for (var i = 0; i < sourceFiles.Length; i++)
        {
            var sourceRelativeFilename = Path.GetRelativePath(commit.SourceRoot, sourceFiles[i]);
            if (fixedFiles.ContainsKey(sourceRelativeFilename) == false 
                || policy.Equals(sourceFiles[i], fixedFiles[sourceRelativeFilename]) == false)
            {
                commit.ChangedFiles.Add(sourceFiles[i]);
            }
        }

        return commit;
    }
}