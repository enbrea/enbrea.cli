#region ENBREA - Copyright (c) STÜBER SYSTEMS GmbH
/*    
 *    ENBREA
 *    
 *    Copyright (c) STÜBER SYSTEMS GmbH
 *
 *    This program is free software: you can redistribute it and/or modify
 *    it under the terms of the GNU Affero General Public License, version 3,
 *    as published by the Free Software Foundation.
 *
 *    This program is distributed in the hope that it will be useful,
 *    but WITHOUT ANY WARRANTY; without even the implied warranty of
 *    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *    GNU Affero General Public License for more details.
 *
 *    You should have received a copy of the GNU Affero General Public License
 *    along with this program. If not, see <http://www.gnu.org/licenses/>.
 *
 */
#endregion

using Enbrea.Cli.Common;
using Enbrea.Konsoli;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli
{
    public static class CommandHandlers
    {
        public static async Task BackupOffline(FileInfo configFile, FileInfo outFile)
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
                try
                {
                    var config = await ConfigurationManager.LoadFromFile(configFile, cancellationToken);
                    var snapshotManager = new SnapshotManager(config, cancellationToken);
                    await snapshotManager.BackupOffline(outFile);
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                }
            });
        }

        public static async Task CreateExportTask(FileInfo configFile, ExportProvider provider, uint interval)
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var config = await ConfigurationManager.LoadFromFile(configFile, cancellationToken);
                        var scheduleManager = new ScheduleManager();
                        await scheduleManager.CreateExportTask(configFile.FullName, config, provider, interval);
                    }
                    else
                    {
                        throw new PlatformNotSupportedException("This command is only supported by the Windows platform.");
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                }
            });
        }

        public static async Task CreateImportTask(FileInfo configFile, ImportProvider provider, uint interval)
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var config = await ConfigurationManager.LoadFromFile(configFile, cancellationToken);
                        var scheduleManager = new ScheduleManager();
                        await scheduleManager.CreateImportTask(configFile.FullName, config, provider, interval);
                    }
                    else
                    {
                        throw new PlatformNotSupportedException("This command is only supported by the Windows platform.");
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                }
            });
        }

        public static async Task CreateSnapshot(FileInfo configFile)
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
                try
                {
                    var config = await ConfigurationManager.LoadFromFile(configFile, cancellationToken);
                    var snapshotManager = new SnapshotManager(config, cancellationToken);
                    await snapshotManager.CreateSnapshot();
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                }
            });
        }

        public static async Task DeleteAllTasks()
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var scheduleManager = new ScheduleManager();
                        await scheduleManager.DeleteAllTasks();
                    }
                    else
                    {
                        throw new PlatformNotSupportedException("This command is only supported by the Windows platform.");
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                }
            });
        }

        public static async Task DeleteExportTask(ExportProvider provider)
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var scheduleManager = new ScheduleManager();
                        await scheduleManager.DeleteExportTask(provider);
                    }
                    else
                    {
                        throw new PlatformNotSupportedException("This command is only supported by the Windows platform.");
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                }
            });
        }

        public static async Task DeleteImportTask(ImportProvider provider)
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var scheduleManager = new ScheduleManager();
                        await scheduleManager.DeleteImportTask(provider);
                    }
                    else
                    {
                        throw new PlatformNotSupportedException("This command is only supported by the Windows platform.");
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                }
            });
        }

        public static async Task DeleteSnapshot(FileInfo configFile, Guid uid)
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
                try
                {
                    var config = await ConfigurationManager.LoadFromFile(configFile, cancellationToken);
                    var snapshotManager = new SnapshotManager(config, cancellationToken);
                    await snapshotManager.DeleteSnapshot(uid);
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                }
            });
        }

        public static async Task Export(FileInfo configFile, ExportProvider provider, bool skipEnbrea, bool skipProvider, string logFile)
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count, LoggerFactory.CreateLogger(logFile));
                try
                {
                    var config = await ConfigurationManager.LoadFromFile(configFile, cancellationToken);

                    if (!skipEnbrea)
                    {
                        var exportManager = ExportManagerFactory.CreateExportFromEnbreaManager(provider, config, consoleWriter, cancellationEvent, cancellationToken);
                        if (exportManager != null)
                        {
                            await exportManager.Execute();
                        }

                    }

                    if (!skipProvider)
                    {
                        var importManager = ImportManagerFactory.CreateImportToProviderManager(provider, config, consoleWriter, cancellationToken);
                        if (importManager != null)
                        {
                            await importManager.Execute();
                        }
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                };
            });
        }

        public static async Task Import(FileInfo configFile, ImportProvider provider, bool skipProvider, bool skipEnbrea, bool skipSnapshot, string logFile)
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count, LoggerFactory.CreateLogger(logFile));
                try
                {
                    var config = await ConfigurationManager.LoadFromFile(configFile, cancellationToken);

                    if (!skipProvider)
                    {
                        var exportManager = ExportManagerFactory.CreateExportFromProviderManager(provider, config, consoleWriter, cancellationToken);
                        if (exportManager != null)
                        {
                            await exportManager.Execute();
                        }

                    }

                    if (!skipEnbrea)
                    {
                        var importManager = ImportManagerFactory.CreateImportToEnbreaManager(provider, config, skipSnapshot, consoleWriter, cancellationEvent, cancellationToken);
                        if (importManager != null)
                        {
                            await importManager.Execute();
                        }
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                };
            });
        }

        public static async Task Init(FileInfo configFile)
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
                try
                {
                    consoleWriter.StartProgress("Create configuration template...");

                    await ConfigurationManager.SaveTemplateToFile(configFile, cancellationToken);

                    consoleWriter.FinishProgress();
                    consoleWriter.Success($"Successfully created configuration template {configFile.FullName}");
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                }
            });
        }

        public static async Task ListAllTasks()
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var scheduleManager = new ScheduleManager();
                        await scheduleManager.ListAllTasks();
                    }
                    else
                    {
                        throw new PlatformNotSupportedException("This command is only supported by the Windows platform.");
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                }
            });
        }

        public static async Task ListSchoolTerms(FileInfo configFile)
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
                try
                {
                    var config = await ConfigurationManager.LoadFromFile(configFile, cancellationToken);
                    var schoolTermManager = new SchoolTermManager(config, cancellationToken);
                    await schoolTermManager.ListSchoolTerms();
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                }
            });
        }

        public static async Task ListSnapshots(FileInfo configFile)
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
                try
                {
                    var config = await ConfigurationManager.LoadFromFile(configFile, cancellationToken);
                    var snapshotManager = new SnapshotManager(config, cancellationToken);
                    await snapshotManager.ListSnapshots();
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                }
            });
        }
        public static async Task RestoreSnapshot(FileInfo configFile, Guid uid)
        {
            await Execute(async (cancellationEvent, cancellationToken) =>
            {
                var consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
                try
                {
                    var config = await ConfigurationManager.LoadFromFile(configFile, cancellationToken);
                    var snapshotManager = new SnapshotManager(config, cancellationToken);
                    await snapshotManager.RestoreSnapshot(uid);
                }
                catch (Exception ex)
                {
                    consoleWriter.NewLine().Error($"Command failed! {ex.Message}");
                    throw;
                }
            });
        }

        private static async Task Execute(Func<EventWaitHandle, CancellationToken, Task> action)
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            using var cancellationEvent = new EventWaitHandle(false, EventResetMode.AutoReset);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellationTokenSource.Cancel();
                cancellationEvent.Set();
            };

            var stopwatch = new Stopwatch();

            stopwatch.Start();

            try
            {
                await action(cancellationEvent, cancellationTokenSource.Token);
            }
            catch (Exception)
            {
                Environment.ExitCode = 1;
            }

            stopwatch.Stop();

            Console.WriteLine();
            Console.WriteLine($"Time elapsed: {stopwatch.Elapsed}.");
            Console.WriteLine();
        }
    }
}