using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SingularisTestTask.Config;
using SingularisTestTask.FileSystem;
using SingularisTestTask.FileSystem.FileComparePolicy;
using SingularisTestTask.FileSystem.FilenamePolicy;
using static SingularisTestTask.AppConstants.AppConstants;

namespace SingularisTestTask.ScheduleWorker;

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
            _logger.LogError($"Invalid input : {e.OptionsName} in {APP_SETTINGS_FILENAME}");
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
        _logger.LogInformation($"iteration number: {_iterationNumber++}");

        try
        {
            var options = ActualOptions;
            
            if (FileHelper.IsPrimaryResolve(options.DestinationPath))
                _logger.LogInformation("Make a primary backup");

            // Создается новая фиксация
            var commit = Commit.Create(options.SourcePath, options.DestinationPath, _filenameGenerationPolicy);
            
            // Определение изменений и отправка их в необходимую дирректорию
            Repository.MakeResolve(commit, _fileComparePolicy).Push();
        }
        catch (OptionsValidationException e)
        {
            _logger.LogError($"Invalid input : {e.OptionsName} in {APP_SETTINGS_FILENAME}");
            Environment.Exit(IVALID_SETTINGS_FORMAT);
        }
        catch (UnauthorizedAccessException e)
        {
            _logger.LogError($"Have not access to directory {e.Message}");
            Environment.Exit(UNAUTHORIZED);
        }
        catch (DirectoryNotFoundException e)
        {
            _logger.LogError($"Directory not found {e.Message}");
            Environment.Exit(DIRECTORY_NOT_FOUND);
        }
        catch (FileNotFoundException e)
        {
            _logger.LogError($"File not found {e.FileName}");
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