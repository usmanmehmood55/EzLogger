using EzLogger;

namespace EzLoggerApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Logger.SetConfig(Verbosity.Debug, Verbosity.Warning, 2);

            Logger.Debug("Some event has happened.");
            Logger.Info("Application initialized successfully.");
            Logger.Warning("Low disk space warning.");
            Logger.Error("File read error occurred.");
            Logger.Critical("Database connection failure!");

            Logger.Debug("This\nis\na\nmulti-line\ncomment.");

            Logger.Info($"Max logs size is set to {Logger.MaxLogsFolderSizeGb} Gb, {Logger.MaxLogsFolderSizeMb} Mb");

            for (int i = 0; i < 10; i++)
            {
                Logger.Debug($"{i}");
            }

            Thread.Sleep(1000);
            Logger.StopLoggingTasks();
        }
    }
}
