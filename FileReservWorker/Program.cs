using FileReservWorker.Config;
using FileReservWorker.FileSystem.FileComparePolicy;
using FileReservWorker.FileSystem.FilenamePolicy;
using FileReservWorker.ScheduleWorker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static FileReservWorker.AppConstants.AppConstants;


namespace FileReservWorker;


/// <summary>
/// Программа производит инкрементальное копирование файлов согласно конфигурации из appsetting.json
/// Перемещает измененные или добавленые файлы из папки source в папку destination
/// 
/// Проверка изменений производится с помощью политик сравнения:
///     - <see cref="HashFileComparePolicy"/> - Сравнение с помощью вычисления хэша файла
///     - <see cref="LastWriteTimeFileComparePolicy"/> - Сравнение с помощью получения даты изменения файла
///
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        await new HostBuilder()
            .ConfigureAppConfiguration(builder =>
            {
                var root = Directory.GetCurrentDirectory();
                var optionsPath = Path.Combine(root, APP_SETTINGS_FILENAME);
                builder.AddJsonFile(optionsPath);
            })
            .ConfigureServices((hostBuilderContext, services) =>
            {
                services.AddLogging();
                services.AddOptions<AppOptions>()
                    .Bind(hostBuilderContext.Configuration.GetSection(AppOptions.SectionName))
                    .ValidateDataAnnotations();
                
                services.AddSingleton<IFileComparePolicy, HashFileComparePolicy>();
                services.AddSingleton<IFilenameGenerationPolicy, DatetimeFilenameGenerationPolicy>();
                services.AddHostedService<TimedBackupHostedService>();
            })
            .ConfigureLogging((_, config) =>
            {
                config.AddConsole();
                config.AddDebug();
            }).RunConsoleAsync();
    }
}