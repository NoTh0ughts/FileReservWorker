namespace FileReservWorker.FileSystem.FilenamePolicy;

/// <summary> Интерфейс политики генерации имени файла </summary>
public interface IFilenameGenerationPolicy
{
    /// <summary> Генерирует строку для имени фиксации </summary>
    /// <returns> Сгенерированное имя фиксации </returns>
    string Get();
    
    /// <summary> Генерирует заголовок для первичной фиксации </summary>
    /// <returns> Сгенерированное имя фиксации </returns>
    string GetPrimary();
}