using Cronos;
using FileReservWorker.Config;
using FileReservWorker.FileSystem;
using FileReservWorker.FileSystem.FileComparePolicy;
using FileReservWorker.FileSystem.FilenamePolicy;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static FileReservWorker.AppConstants.AppConstants;

namespace FileReservWorker.ScheduleWorker;

/// <summary>
/// Выполняет периодичное инкрементальное копирование с интервалом указаным в appsettings.json
/// </summary>
public class TimedBackupHostedService : BackgroundService
{
    private AppOptions ActualOptions => _optionsMonitor.CurrentValue;
    
    private readonly IOptionsMonitor<AppOptions> _optionsMonitor;
    private readonly ILogger<TimedBackupHostedService> _logger;
    private readonly IFileComparePolicy _fileComparePolicy;
    private readonly IFilenameGenerationPolicy _filenameGenerationPolicy;
    
    private Task? _currentProcess;
    
    private Timer _timer;
    private int _iterationNumber = 1;
    
    public TimedBackupHostedService(IOptionsMonitor<AppOptions> optionsMonitor, 
                                    ILogger<TimedBackupHostedService> logger,
                                    IFileComparePolicy fileComparePolicy, 
                                    IFilenameGenerationPolicy filenameGenerationPolicy)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
        _fileComparePolicy = fileComparePolicy;
        _filenameGenerationPolicy = filenameGenerationPolicy;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started BackupService");
        try
        {
            var cronString = ActualOptions.Schedule;
            var cronExpression = CronExpression.Parse(cronString);
            var nowDate = DateTime.UtcNow;

            var periodicity = cronExpression.GetNextOccurrence(nowDate) - nowDate;

            if (periodicity.HasValue)
                _timer = new Timer(DoWork, cronExpression, TimeSpan.Zero, periodicity.Value);
            else
            {
                throw new OptionsValidationException("Periodicity",
                    typeof(string),
                    new[]
                    {
                        "Can`t parse periodicity to cron expression"
                    });
            }
        }
        catch (OptionsValidationException e)
        {
            _logger.LogError("Invalid input : {OptionsName} in {APP_SETTINGS_FILENAME}",
                e.OptionsName,
                APP_SETTINGS_FILENAME);
            Environment.Exit(IVALID_SETTINGS_FORMAT);
        }
        
        return Task.CompletedTask;
    }
    
    private void DoWork(object? state)
    {
        if (_currentProcess is null || _currentProcess.IsCompleted)
            _currentProcess = WorkProcess();
        else
            _logger.LogWarning("The previous copying process was not completed");
    }
    
    private Task WorkProcess()
    {
        try
        {
            var options = ActualOptions;
            
            if (FileHelper.IsPrimaryResolve(options.DestinationPath))
                _logger.LogInformation("Make a primary backup");
            
            _logger.LogInformation("Iteration number :{_iterationNumber}", _iterationNumber++);
            // Создается новая фиксация
            var commit = Commit.Create(options.SourcePath, options.DestinationPath, _filenameGenerationPolicy);
            
            // Определение изменений и отправка их в необходимую дирректорию
            Repository.MakeResolve(commit, _fileComparePolicy).Push();
        }
        catch (OptionsValidationException)
        {
            _logger.LogError($"Invalid input in {APP_SETTINGS_FILENAME}");
            Environment.Exit(IVALID_SETTINGS_FORMAT);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError($"Have not access to directory");
            Environment.Exit(UNAUTHORIZED);
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogError($"Directory not found");
            Environment.Exit(DIRECTORY_NOT_FOUND);
        }
        catch (FileNotFoundException)
        {
            _logger.LogError($"File not found ");
        }

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackupService is ended");
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        base.Dispose();
        
        _currentProcess?.Dispose();
        _timer?.Dispose();
    }
}