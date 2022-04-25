using System.Security.Cryptography;
using System.Text;
using static SingularisTestTask.AppConstants.AppConstants;

namespace SingularisTestTask.FileSystem;

public static class FileHelper
{
    /// <summary>
    /// Определяет, есть ли папка в destination директории с именем первичного копирования
    /// Возвращает true, если папка не найдена
    /// </summary>
    /// <param name="destinationPath"> Путь, в который будет производиться копирование </param>
    /// <returns></returns>
    public static bool IsPrimaryResolve(string destinationPath) => 
        Directory.Exists(Path.Combine(destinationPath, PRIMARY_DIRRECTORY_NAME)) == false;
    
    
    /// <summary> Вычисляет хэш файла MD5 </summary>
    /// <param name="path"> Путь к файлу </param>
    /// <returns> Вычисленный хэш файла </returns>
    /// <exception cref="FileNotFoundException"> Файл не существует </exception>
    public static string GetFileHash(string path)
    {
        if (File.Exists(path) == false)
        {
            throw new FileNotFoundException("cant find file with path: " + path);
        }

        using var md5 = MD5.Create();
        using var stream = File.OpenRead(path);
        
        return Encoding.Default.GetString(md5.ComputeHash(stream));
    }

    /// <summary>
    /// Копирует файл в место назначения, если дирректории на пути нет, то создает ее
    /// </summary>
    /// <param name="sourceRoot"> Путь к корню источника </param>
    /// <param name="pathToFile"> Путь к копируемому файлу </param>
    /// <param name="to"> Дирректория, в которую будет перемещен файл </param>
    public static void CopyFile(string sourceRoot, string pathToFile, string to)
    {
        var relativePath = Path.GetRelativePath(sourceRoot, pathToFile);
        var directoryName = Path.GetDirectoryName(relativePath);

        var newPath = Path.Combine(to, relativePath);
        
        // Файл находится не в корне папки 
        if (string.IsNullOrEmpty(directoryName) == false)
        {
            var dirsPath = Path.Combine(to, directoryName);
            if (Directory.Exists(dirsPath) == false)
            {
                Directory.CreateDirectory(dirsPath);
            }
        }

        File.Copy(pathToFile, newPath);
    }
}