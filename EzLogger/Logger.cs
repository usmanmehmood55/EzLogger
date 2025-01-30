using System.Collections.Concurrent;
using System.Text;

namespace EzLogger
{
    public enum Verbosity
    {
        Critical,
        Error,
        Warning,
        Info,
        Debug,
    }

    public class Logger
    {
        private readonly struct Log
        {
            internal readonly DateTime TimeStamp;
            internal readonly Verbosity Verbosity;
            internal readonly string Message;

            internal Log(Verbosity verbosity, string message)
            {
                TimeStamp = DateTime.Now;
                Verbosity = verbosity;
                Message   = message;
            }

            /// <summary>
            /// Use this only when copying an old log into a new object.
            /// For example in <see cref="BreakDownMultiLine"/> where
            /// a single multi-line log is split into multiple single-line
            /// logs, so old timestamps are needed.
            /// </summary>
            internal Log(DateTime timestamp, Verbosity verbosity, string message)
            {
                TimeStamp = timestamp;
                Verbosity = verbosity;
                Message   = message;
            }

            public override string ToString()
            {
                return $"{Verbosity}:{Message}";
            }
        }

        #region LoggerInterface
        /// <summary>
        /// Prints a debug message to the terminal if <see cref="ConsoleVerbosity"/> is set
        /// to <see cref="Verbosity.Debug"/>. The same message is also written to a log file
        /// if the <see cref="FileVerbosity"/> is set to <see cref="Verbosity.Debug"/>.
        /// </summary>
        /// 
        /// <param name="message">Debug message</param>
        public static void Debug(string message = "") => Instance.LogPush(Verbosity.Debug, message);

        /// <summary>
        /// Prints an info message to the terminal if <see cref="ConsoleVerbosity"/> is set
        /// to <see cref="Verbosity.Info"/> or higher. The same message is also written to a
        /// log file if the <see cref="FileVerbosity"/> is set to <see cref="Verbosity.Info"/>
        /// or higher.
        /// </summary>
        /// 
        /// <param name="message">Info message</param>
        public static void Info(string message = "") => Instance.LogPush(Verbosity.Info, message);

        /// <summary>
        /// Prints a warning message to the terminal if <see cref="ConsoleVerbosity"/> is set
        /// to <see cref="Verbosity.Warning"/> or higher. The same message is also written to a
        /// log file if the <see cref="FileVerbosity"/> is set to <see cref="Verbosity. Warning"/>
        /// or higher.
        /// </summary>
        /// 
        /// <param name="message">Warning message</param>
        public static void Warning(string message = "") => Instance.LogPush(Verbosity.Warning, message);

        /// <summary>
        /// Prints an error message to the terminal if <see cref="ConsoleVerbosity"/> is set
        /// to <see cref="Verbosity.Error"/> or higher. The same message is also written to a
        /// log file if the <see cref="FileVerbosity"/> is set to <see cref="Verbosity. Error"/>
        /// or higher.
        /// </summary>
        /// 
        /// <param name="message">Error message</param>
        public static void Error(string message = "") => Instance.LogPush(Verbosity.Error, message);

        /// <summary>
        /// Prints a critical message to the terminal if <see cref="ConsoleVerbosity"/> is set
        /// to <see cref="Verbosity.Critical"/> or higher. The same message is also written to a
        /// log file if the <see cref="FileVerbosity"/> is set to <see cref="Verbosity.Critical"/>
        /// or higher.
        /// </summary>
        /// 
        /// <param name="message">Critical error message</param>
        public static void Critical(string message = "") => Instance.LogPush(Verbosity.Critical, message);

        /// <summary>
        /// Sets the <see cref="ConsoleVerbosity"/> level of the singleton instance.
        /// </summary>
        /// <param name="consoleVerbosity"></param>
        public static void SetConsoleVerbosityLevel(Verbosity consoleVerbosity) => Instance.ConsoleVerbosity = consoleVerbosity;

        /// <summary>
        /// Sets the <see cref="FileVerbosity"/> level of the singleton instance.
        /// </summary>
        /// <param name="fileVerbosity"></param>
        public static void SetFileVerbosityLevel(Verbosity fileVerbosity) => Instance.FileVerbosity = fileVerbosity;

        /// <summary>
        /// Sets the <see cref="MaxLogFolderSizeBytes"/> in Mb
        /// </summary>
        public static long MaxLogsFolderSizeMb => Instance.MaxLogFolderSizeMb;

        /// <summary>
        /// Sets the <see cref="MaxLogFolderSizeBytes"/> in Gb
        /// </summary>
        public static long MaxLogsFolderSizeGb => Instance.MaxLogFolderSizeGb;

        /// <summary>
        /// Provides a path to the logs file.
        /// </summary>
        public static string LogsPath => Instance.GetLogsPath();

        /// <summary>
        /// Stops the background cleanup task that ensures the total size of log files does not exceed
        /// the maximum limit.
        /// </summary>
        /// <remarks>
        /// Call this method to stop the cleanup task, for example, when the application is about to exit.
        /// </remarks>
        public static void StopLoggingTasks() => Instance.LoggingTasksGracefulExit();

        /// <summary>
        /// Sets the configuration for the logger
        /// </summary>
        /// <param name="consoleVerbosity"></param>
        /// <param name="fileVerbosity"></param>
        public static void SetConfig(Verbosity consoleVerbosity, Verbosity fileVerbosity, long maxLogSizeGb = 1)
        {
            SetConsoleVerbosityLevel(consoleVerbosity);
            SetFileVerbosityLevel(fileVerbosity);
            Instance.MaxLogFolderSizeGb = maxLogSizeGb;
            PrintBanner(Instance.ApplicationName);
        }
        #endregion

        private static Logger? _instance = null;
        private static readonly object lockObject = new();

        private static Logger Instance
        {
            get
            {
                lock (lockObject)
                {
                    _instance ??= new Logger();
                    return _instance;
                }
            }
        }

        private long MaxLogFolderSizeBytes { get; set; } = 1 * (1024 * 1024 * 1024); // 1 GB
        private long MaxLogFolderSizeKb { get => MaxLogFolderSizeBytes / 1024; set => MaxLogFolderSizeBytes = value * 1024; }
        private long MaxLogFolderSizeMb { get => MaxLogFolderSizeKb / 1024; set => MaxLogFolderSizeKb = value * 1024; }
        private long MaxLogFolderSizeGb { get => MaxLogFolderSizeMb / 1024; set => MaxLogFolderSizeMb = value * 1024; }

        private CancellationTokenSource LoggingTaskCancellationTokenSource { get; } = new();
        private CancellationTokenSource CleanupTaskCancellationTokenSource { get; } = new();
        private BlockingCollection<Log> LogQueue { get; } = new();
        private Verbosity ConsoleVerbosity { get; set; }
        private Verbosity FileVerbosity { get; set; }
        private string ApplicationName { get; set; }
        private static ConsoleColor DefaultForeground => Console.ForegroundColor;
        private static ConsoleColor DefaultBackground => Console.BackgroundColor;
        private Task LoggingTask { get; }
        private Task CleanupTask { get; }
        private SemaphoreSlim LogFileSemaphore { get; } = new SemaphoreSlim(1, 1);
        private bool IsLoggingServiceRunning { get; set; } = false;
        private bool IsCleaningServiceRunning { get; set; } = false;

        private Logger(Verbosity consoleVerbosity = Verbosity.Debug, Verbosity fileVerbosity = Verbosity.Warning)
        {
            ConsoleVerbosity = consoleVerbosity;
            FileVerbosity    = fileVerbosity;

            ApplicationName = AppDomain.CurrentDomain.FriendlyName;
            LoggingTask = Task.Run(() => Instance.LoggerService(LoggingTaskCancellationTokenSource.Token));
            CleanupTask = Task.Run(() => Instance.LoggerCleanerService(CleanupTaskCancellationTokenSource.Token));
        }

        /// <summary>
        /// Pushes the a to the <see cref="LogQueue"/> with the current time.
        /// </summary>
        /// 
        /// <param name="verbosity">Verbosity level of the message</param>
        /// <param name="message">Log message</param>
        private void LogPush(Verbosity verbosity, string message)
        {
            if (verbosity > ConsoleVerbosity)
                return;

            Log log = new(verbosity, message);
            LogQueue.Add(log);
        }

        /// <summary>
        /// Prints a batch of logs on to the console.
        /// </summary>
        /// <param name="logBatch"></param>
        private static void InternalLogConsole(List<Log> logBatch)
        {
            foreach (Log log in logBatch)
            {
                string formattedLogstring = ComposeLogString(log.TimeStamp, log.Verbosity, log.Message);
                Console.ResetColor();

                Console.ForegroundColor = log.Verbosity switch
                {
                    Verbosity.Debug    => ConsoleColor.Green,
                    Verbosity.Warning  => ConsoleColor.Yellow,
                    Verbosity.Error    => ConsoleColor.Red,
                    Verbosity.Critical => ConsoleColor.White,

                    _ => DefaultForeground,
                };

                Console.BackgroundColor = log.Verbosity switch
                {
                    Verbosity.Critical => ConsoleColor.DarkRed,

                    _ => DefaultBackground,
                };

                Console.Write("\r");
                Console.Write(formattedLogstring);
                Console.ResetColor();
                Console.Write("\n");
            }
        }

        /// <summary>
        /// Writes a batch of logs on to the log file.
        /// </summary>
        /// <param name="logBatch"></param>
        /// <returns></returns>
        private async Task InternalLogFile(List<Log> logBatch)
        {
            await LogFileSemaphore.WaitAsync();

            try
            {
                List<Log> filteredLogs = logBatch.Where(log => log.Verbosity <= FileVerbosity).ToList();
                if (filteredLogs.Count > 0)
                {
                    string filePath = GetLogsPath();
                    string fileDir = GetLogsDir();
                    if (!File.Exists(filePath))
                    {
                        Directory.CreateDirectory(fileDir);
                        FileStream createStream = File.Create(filePath);
                        createStream.Close();
                    }

                    using FileStream fileStream = new(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                    using StreamWriter streamWriter = new(fileStream);
                    foreach (Log log in filteredLogs)
                    {
                        string formattedLogString = ComposeLogString(log.TimeStamp, log.Verbosity, log.Message);
                        await streamWriter.WriteLineAsync(formattedLogString);
                    }
                }
            }
            finally
            {
                LogFileSemaphore.Release();
            }
        }

        /// <summary>
        /// Breaks down multi-line logs into multiple single-line logs
        /// </summary>
        /// <param name="logList"></param>
        /// <returns></returns>
        private static List<Log> BreakDownMultiLine(List<Log> logList)
        {
            List<Log> breakdownList = new(logList.Count * 3);

            foreach (Log multiLineLog in logList)
            {
                if (multiLineLog.Message.Contains('\n'))
                {
                    string[] lines = multiLineLog.Message.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    foreach (string singleLine in lines)
                    {
                        Log breakDown = new(multiLineLog.TimeStamp, multiLineLog.Verbosity, singleLine);
                        breakdownList.Add(breakDown);
                    }
                }
                else
                {
                    breakdownList.Add(multiLineLog);
                }
            }

            return breakdownList;
        }

        /// <summary>
        /// Writes a batch of logs on to the console and the log file.
        /// </summary>
        /// <param name="logBatch"></param>
        private void InternalLog(List<Log> logBatch)
        {
            List<Log> brokenDownLogs = BreakDownMultiLine(logBatch);
            Task a = Task.Run(() => InternalLogConsole(brokenDownLogs));
            Task b = Task.Run(() => InternalLogFile(brokenDownLogs));
            Task.WaitAll(a, b);
        }

        /// <summary>
        /// Logger's main service/task that takes any available logs from the <see cref="LogQueue"/>
        /// and write them on to the console and the log file.
        /// </summary>
        /// <param name="token">A cancellation token that can be used to cancel the logger task.</param>
        /// <returns></returns>
        private async Task LoggerService(CancellationToken token)
        {
            IsLoggingServiceRunning = true;
            try
            {
                List<Log> logList = new();
                while (!token.IsCancellationRequested || !LogQueue.IsCompleted)
                {
                    logList.Clear();
                    try
                    {
                        if (LogQueue.TryTake(out Log firstInBatch, 500, token))
                        {
                            logList.Add(firstInBatch);
                            while (LogQueue.TryTake(out Log restInBatch, 0, token))
                            {
                                logList.Add(restInBatch);
                            }

                            InternalLog(logList);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                List<Log> remainingLogs = new();
                while (LogQueue.TryTake(out Log log))
                {
                    remainingLogs.Add(log);
                }
                if (remainingLogs.Any())
                {
                    InternalLog(remainingLogs);
                }

                // InternalLogConsole(new List<Log> { new(Verbosity.Info, $"{ApplicationName}: Logging task closed") });
            }

            IsLoggingServiceRunning = false;
        }

        /// <summary>
        /// Ensures that the log directory does not exceed the maximum allowed size.
        /// Deletes the oldest files if the directory size exceeds the limit.
        /// </summary>
        /// <param name="token">A cancellation token that can be used to cancel the cleanup task.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method runs indefinitely and periodically checks the size of the log directory.
        /// If the size exceeds the maximum allowed size, it deletes the oldest files until
        /// the total size is within the allowed limit.
        /// </remarks>
        private async Task LoggerCleanerService(CancellationToken token)
        {
            IsCleaningServiceRunning = true;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                    EnsureLogFolderSizeLimit();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Error($"Error in cleanup task: {ex.Message}");
                }
            }

            // InternalLogConsole(new List<Log> { new(Verbosity.Info, $"{ApplicationName}: Logger cleanup task closed") });
            IsCleaningServiceRunning = false;
        }

        /// <summary>
        /// Ensures that the total size of log files in the log folder doesn't exceed the maximum limit.
        /// If it does, deletes the oldest files until the size is within the limit.
        /// </summary>
        private void EnsureLogFolderSizeLimit()
        {
            string logsDir = GetLogsDir();
            if (!Directory.Exists(logsDir))
            {
                return;
            }

            LogFileSemaphore.Wait();

            try
            {
                // Calculate the total size of the log files in the log folder
                string[] logFiles = Directory.GetFiles(logsDir, "*.txt", SearchOption.AllDirectories);
                long totalSize = logFiles.Sum(file => new FileInfo(file).Length);

                // Sort the log files by creation time ascending
                List<string> orderedLogFiles = logFiles.OrderBy(file => new FileInfo(file).CreationTime).ToList();

                // Delete the oldest files until the total size is within the limit
                int index = 0;
                while (totalSize > MaxLogFolderSizeBytes && index < orderedLogFiles.Count)
                {
                    totalSize -= new FileInfo(orderedLogFiles[index]).Length;
                    File.Delete(orderedLogFiles[index]);
                    Warning($"Logs cleanup task deleted file {orderedLogFiles[index]}");
                    index++;
                }
            }
            finally
            {
                LogFileSemaphore.Release();
            }
        }

        /// <summary>
        /// Composes the actual log string to be printed or written to a file. Appends the
        /// timestamp as well as verbosity to the message string.
        /// </summary>
        /// 
        /// <param name="verbosity">verbosity level</param>
        /// <param name="message">log message</param>
        /// 
        /// <returns>timestamped string</returns>
        private static string ComposeLogString(DateTime timeStamp, Verbosity verbosity, string message)
        {
            var logString = new StringBuilder(128);
            logString.AppendFormat("[{0:D2}:{1:D2}:{2:D2}:{4:D3}] ", timeStamp.Hour, timeStamp.Minute, timeStamp.Second, timeStamp.Millisecond);
            logString.Append(verbosity.ToString().PadRight(8));
            logString.Append(" -> ");
            logString.Append(message);
            return logString.ToString();
        }

        /// <summary>
        /// Returns path to the current logs file. The logs folder is supposed to be on the same
        /// location as the app itself, and named Application Name + "Logs". Inside which would be
        /// a text file with the year and week of year as its name. 
        /// </summary>
        /// 
        /// <returns>Path to logs file</returns>
        private string GetLogsPath()
        {
            string LogsPath = Path.Combine(GetLogsDir(), GetLogsFileName());
            return LogsPath;
        }

        /// <summary>
        /// The name of the logs file should be in this syntax:
        /// Year_`CurrentYear`_Week_`CurrentWeekOfYear`.txt. E.g: Year_2023_Week_25.txt
        /// </summary>
        /// <returns>Name of the current log file.</returns>
        private static string GetLogsFileName()
        {
            DateTime now = DateTime.Now;
            string logsFileName = $"log_y{now.Year}_m{now.Month}_d{now.Day}.txt";
            return logsFileName;
        }

        /// <summary>
        /// The path of the logs directory should be the Name of the application + "Logs".
        /// </summary>
        /// <returns>Path of the current logs directory.</returns>
        private string GetLogsDir()
        {
            string logsDir = Path.Combine(Directory.GetCurrentDirectory(), $"{ApplicationName}Logs");
            return logsDir;
        }

        /// <summary>
        /// Prints a banner with the application name to the console.
        /// </summary>
        /// <param name="applicationName">The name of the application to be printed in the banner.</param>
        /// <remarks>
        /// Useful for providing a visual indicator of the application's start in the logs.
        /// </remarks>
        private static void PrintBanner(string applicationName)
        {
            int bannerPadding = 10;
            int bannerLength = applicationName.Length + (bannerPadding * 2) + 2;

            var banner = new StringBuilder(bannerLength * 5);

            // Line of stars
            banner.Append('*', bannerLength);
            banner.Append('\n');

            // Empty line
            banner.Append('*');
            banner.Append(' ', bannerLength - 2);
            banner.Append('*');
            banner.Append('\n');

            // App name
            banner.Append('*');
            banner.Append(' ', bannerPadding);
            banner.Append(applicationName);
            banner.Append(' ', bannerPadding);
            banner.Append('*');
            banner.Append('\n');

            // Empty line
            banner.Append('*');
            banner.Append(' ', bannerLength - 2);
            banner.Append('*');
            banner.Append('\n');

            // Line of stars
            banner.Append('*', bannerLength);
            banner.Append('\n');

            Info(banner.ToString());
        }

        /// <summary>
        /// Stops the background logger and cleanup tasks.
        /// </summary>
        /// <remarks>
        /// Call this method to gracefully close the logger, for example, when the application is about to exit.
        /// </remarks>
        private void LoggingTasksGracefulExit()
        {
            LogQueue.CompleteAdding();

            LoggingTaskCancellationTokenSource.Cancel();

            try
            {
                LoggingTask.Wait();
            }
            catch (AggregateException ae)
            {
                ae.Handle((x) =>
                {
                    if (x is TaskCanceledException) return true;
                    return false;
                });
            }

            CleanupTaskCancellationTokenSource.Cancel();

            try
            {
                CleanupTask.Wait();
            }
            catch (AggregateException ae)
            {
                ae.Handle((x) =>
                {
                    if (x is TaskCanceledException) return true;
                    return false;
                });
            }

            if (IsLoggingServiceRunning is false && IsCleaningServiceRunning is false)
                InternalLogConsole(new List<Log> { new(Verbosity.Info, $"{ApplicationName}: Logging tasks graceful exit") });
            else
                InternalLogConsole(new List<Log> { new(Verbosity.Critical, $"{ApplicationName}: Logging tasks not closed!") });
        }
    }
}
