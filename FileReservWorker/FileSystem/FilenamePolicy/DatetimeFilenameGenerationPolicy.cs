namespace FileReservWorker.FileSystem.FilenamePolicy;
using static AppConstants.AppConstants;

/// <summary> Политика генерации имя файла на основе даты инкрементации </summary>
public class DatetimeFilenameGenerationPolicy : IFilenameGenerationPolicy
{
    public string Get() => DIRECTORY_NAME_PREFIX + DateTime.Now.ToString(DATETIME_DIRECTORY_NAME_FORMAT);

    public string GetPrimary() => PRIMARY_DIRRECTORY_NAME;
}