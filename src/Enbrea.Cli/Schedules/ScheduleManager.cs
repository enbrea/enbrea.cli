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
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Enbrea.Cli
{
    public class ScheduleManager
    {
        private const string NetServiceAccount = "NT AUTHORITY\\NETWORKSERVICE";
        private readonly ConsoleWriter _consoleWriter;

        public ScheduleManager()
        {
            _consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
        }

        public void CreateExportTask(string configFile, Configuration config, ExportProvider provider, uint interval)
        {
            _consoleWriter.StartProgress($"Create or update an export task to {provider}");
            try
            {
                Directory.CreateDirectory(GetLogFolderName(config, provider));

                var task = GetTask(provider);

                if (task != null)
                {
                    if (task.Definition.Triggers.Count > 0)
                    {
                        task.Definition.Triggers[0].Repetition.Interval = TimeSpan.FromMinutes(interval);
                        task.RegisterChanges();
                        _consoleWriter.FinishProgress().Success($"Task successfully updated");
                    }
                    else
                    {
                        throw new ScheduleException($"No trigger defined");
                    }
                }
                else
                {
                    var taskDefinition = TaskService.Instance.NewTask();

                    taskDefinition.RegistrationInfo.Description = $"ENBREA Export to {provider}";
                    taskDefinition.Settings.Enabled = false;
                    taskDefinition.Principal.Id = NetServiceAccount;
                    taskDefinition.Principal.LogonType = TaskLogonType.ServiceAccount;

                    var weekTrigger = new WeeklyTrigger
                    {
                        StartBoundary = DateTime.Now,
                        DaysOfWeek = DaysOfTheWeek.AllDays,
                        WeeksInterval = 1
                    };

                    weekTrigger.Repetition.Interval = TimeSpan.FromMinutes(interval);

                    taskDefinition.Triggers.Add(weekTrigger);

                    taskDefinition.Actions.Add(
                        "enbrea.exe",
                        $"export -p {provider} -c \"{configFile}\" -l \"{Path.Combine(GetLogFolderName(config, provider), "log.txt")}\"",
                        Path.GetDirectoryName(configFile));

                    var taskFolder = AddEnbreaTaskFolder();

                    taskFolder.RegisterTaskDefinition($"enbrea.to.{provider}", taskDefinition, TaskCreation.CreateOrUpdate,
                        NetServiceAccount, null, TaskLogonType.ServiceAccount);

                    _consoleWriter.FinishProgress().Success($"Task successfully created");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void CreateImportTask(string configFile, Configuration config, ImportProvider provider, uint interval)
        {
            _consoleWriter.StartProgress($"Create or update an import task from {provider}");
            try
            {
                Directory.CreateDirectory(GetLogFolderName(config, provider));

                var task = GetTask(provider);

                if (task != null)
                {
                    if (task.Definition.Triggers.Count > 0)
                    {
                        task.Definition.Triggers[0].Repetition.Interval = TimeSpan.FromMinutes(interval);
                        task.RegisterChanges();
                        _consoleWriter.FinishProgress().Success($"Task successfully updated");
                    }
                    else
                    {
                        throw new ScheduleException($"No trigger defined");
                    }
                }
                else
                {
                    var taskDefinition = TaskService.Instance.NewTask();

                    taskDefinition.RegistrationInfo.Description = $"ENBREA Import from {provider}";
                    taskDefinition.Settings.Enabled = false;
                    taskDefinition.Principal.Id = NetServiceAccount;
                    taskDefinition.Principal.LogonType = TaskLogonType.ServiceAccount;

                    var weekTrigger = new WeeklyTrigger
                    {
                        StartBoundary = DateTime.Now,
                        DaysOfWeek = DaysOfTheWeek.AllDays,
                        WeeksInterval = 1
                    };

                    weekTrigger.Repetition.Interval = TimeSpan.FromMinutes(interval);

                    taskDefinition.Triggers.Add(weekTrigger);

                    taskDefinition.Actions.Add(
                        "enbrea.exe",
                        $"import -p {provider} -c \"{configFile}\" -l \"{Path.Combine(GetLogFolderName(config, provider), "log.txt")}\"",
                        Path.GetDirectoryName(configFile));

                    var taskFolder = AddEnbreaTaskFolder();

                    taskFolder.RegisterTaskDefinition($"enbrea.from.{provider}", taskDefinition, TaskCreation.CreateOrUpdate,
                        NetServiceAccount, null, TaskLogonType.ServiceAccount);

                    _consoleWriter.FinishProgress().Success($"Task successfully created");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void DeleteAllTasks()
        {
            _consoleWriter.StartProgress("Delete all import and export tasks");
            try
            {
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var tasks = new List<Task>();

                    foreach (ImportProvider provider in Enum.GetValues(typeof(ImportProvider)))
                    {
                        var task = GetTask(provider);

                        if (task != null)
                        {
                            tasks.Add(task);
                        }
                    }

                    foreach (ExportProvider provider in Enum.GetValues(typeof(ExportProvider)))
                    {
                        var task = GetTask(provider);

                        if (task != null)
                        {
                            tasks.Add(task);
                        }
                    }

                    _consoleWriter.FinishProgress();

                    if (tasks.Count > 0)
                    {
                        foreach (var task in tasks)
                        {
                            taskFolder.DeleteTask(task.Name);
                        }

                        _consoleWriter.Success($"{tasks.Count} task(s) successfully deleted");
                    }
                    else
                    {
                        _consoleWriter.Information("No tasks found!");
                    }
                }
                else
                {
                    _consoleWriter.FinishProgress().Information($"No tasks found!");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void DeleteExportTask(ExportProvider provider)
        {
            _consoleWriter.StartProgress("Delete export task");
            try
            {
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var task = GetTask(provider);

                    if (task != null)
                    {
                        taskFolder.DeleteTask(task.Name);

                        _consoleWriter.FinishProgress().Success($"Task {task.Name} successfully deleted");
                    }
                    else
                    {
                        throw new ScheduleException($"The task enbrea.export.{provider} does not exists");
                    }
                }
                else
                {
                    throw new ScheduleException($"No ENBREA tasks found");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void DeleteImportTask(ImportProvider provider)
        {
            _consoleWriter.StartProgress("Delete import task");
            try
            {
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var task = GetTask(provider);

                    if (task != null)
                    {
                        taskFolder.DeleteTask(task.Name);

                        _consoleWriter.FinishProgress().Success($"Task {task.Name} successfully deleted");
                    }
                    else
                    {
                        throw new ScheduleException($"The task enbrea.import.{provider} does not exists");
                    }
                }
                else
                {
                    throw new ScheduleException($"No ENBREA tasks found");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void DisableExportTask(ExportProvider provider)
        {
            _consoleWriter.StartProgress("Disable export task");
            try
            {
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var task = GetTask(provider);

                    if (task != null)
                    {
                        task.Definition.Settings.Enabled = false;
                        task.RegisterChanges();

                        _consoleWriter.FinishProgress().Success($"Task {task.Name} successfully updated");
                    }
                    else
                    {
                        throw new ScheduleException($"The task enbrea.import.{provider} does not exists");
                    }
                }
                else
                {
                    throw new ScheduleException($"No ENBREA tasks found");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void DisableImportTask(ImportProvider provider)
        {
            _consoleWriter.StartProgress("Disable import task");
            try
            {
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var task = GetTask(provider);

                    if (task != null)
                    {
                        task.Definition.Settings.Enabled = false;
                        task.RegisterChanges();

                        _consoleWriter.FinishProgress().Success($"Task {task.Name} successfully updated");
                    }
                    else
                    {
                        throw new ScheduleException($"The task enbrea.import.{provider} does not exists");
                    }
                }
                else
                {
                    throw new ScheduleException($"No ENBREA tasks found");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void EnableExportTask(ExportProvider provider)
        {
            _consoleWriter.StartProgress("Enable export task");
            try
            {
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var task = GetTask(provider);

                    if (task != null)
                    {
                        task.Definition.Settings.Enabled = true;
                        task.RegisterChanges();

                        _consoleWriter.FinishProgress().Success($"Task {task.Name} successfully updated");
                    }
                    else
                    {
                        throw new ScheduleException($"The task enbrea.import.{provider} does not exists");
                    }
                }
                else
                {
                    throw new ScheduleException($"No ENBREA tasks found");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void EnableImportTask(ImportProvider provider)
        {
            _consoleWriter.StartProgress("Enable import task");
            try
            {
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var task = GetTask(provider);

                    if (task != null)
                    {
                        task.Definition.Settings.Enabled = true;
                        task.RegisterChanges();

                        _consoleWriter.FinishProgress().Success($"Task {task.Name} successfully updated");
                    }
                    else
                    {
                        throw new ScheduleException($"The task enbrea.import.{provider} does not exists");
                    }
                }
                else
                {
                    throw new ScheduleException($"No ENBREA tasks found");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void ListAllTasks()
        {
            _consoleWriter.StartProgress("List all import and export tasks...");
            try
            {
                var tasks = new List<Task>();

                foreach (ImportProvider provider in Enum.GetValues(typeof(ImportProvider)))
                {
                    var task = GetTask(provider);

                    if (task != null)
                    {
                        tasks.Add(task);
                    }
                }

                foreach (ExportProvider provider in Enum.GetValues(typeof(ExportProvider)))
                {
                    var task = GetTask(provider);

                    if (task != null)
                    {
                        tasks.Add(task);
                    }
                }

                _consoleWriter.FinishProgress();

                if (tasks.Count > 0)
                {
                    _consoleWriter.NewLine();
                    _consoleWriter.Message($"Task name                 | Enabled | Interval");
                    _consoleWriter.Message($"------------------------- | ------- | --------");

                    foreach (var task in tasks)
                    {
                        _consoleWriter.Message($"{task.Name,-25} | {task.Definition.Settings.Enabled, -7} | {task.Definition.Triggers.FirstOrDefault()?.Repetition?.Interval}");
                    }

                    _consoleWriter.NewLine();
                }

                _consoleWriter.Success($"{tasks.Count} tasks found");
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        private static string GetLogFolderName(Configuration config, ImportProvider provider)
        {
            switch (provider)
            {
                case ImportProvider.davinci:
                    return Path.Combine(config.DaVinci.TargetFolder, "log");
                case ImportProvider.magellan:
                    return Path.Combine(config.Magellan.TargetFolder, "log");
                case ImportProvider.untis:
                    return Path.Combine(config.Untis.TargetFolder, "log");
                case ImportProvider.bbsplanung:
                    return Path.Combine(config.BbsPlanung.TargetFolder, "log");
                case ImportProvider.edoosys:
                    return Path.Combine(config.Edoosys.TargetFolder, "log");
                case ImportProvider.schildnrw:
                    return Path.Combine(config.SchildNRW.TargetFolder, "log");
                case ImportProvider.excel:
                    return Path.Combine(config.Excel.TargetFolder, "log");
                default:
                    return null;
            }
        }

        private static string GetLogFolderName(Configuration config, ExportProvider provider)
        {
            switch (provider)
            {
                case ExportProvider.davinci:
                    return Path.Combine(config.DaVinci.SourceFolder, "log");
                case ExportProvider.magellan:
                    return Path.Combine(config.Magellan.SourceFolder, "log");
                default:
                    return null;
            }
        }

        private TaskFolder AddEnbreaTaskFolder()
        {
            return TaskService.Instance.RootFolder.CreateFolder("Enbrea", exceptionOnExists: false);
        }

        private TaskFolder GetEnbreaTaskFolder()
        {
            return TaskService.Instance.GetFolder("Enbrea");
        }

        private Task GetTask(ImportProvider provider)
        {
            return TaskService.Instance.GetTask($"Enbrea\\enbrea.from.{provider}");
        }

        private Task GetTask(ExportProvider provider)
        {
            return TaskService.Instance.GetTask($"Enbrea\\enbrea.to.{provider}");
        }

    }
}
