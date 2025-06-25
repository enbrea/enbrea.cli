#region Enbrea - Copyright (c) STÜBER SYSTEMS GmbH
/*    
 *    Enbrea
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
using System.Text.RegularExpressions;

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

        public void CreateExportTask(string configFile, Configuration config, ExportProvider provider, uint interval, string suffix)
        {
            _consoleWriter.StartProgress($"Create or update an export task to {provider}");
            try
            {
                Directory.CreateDirectory(GetLogFolderName(config, provider));

                var taskName = GetEnbreaTaskName(provider, suffix);
                var task = GetTask(taskName);

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

                    taskDefinition.RegistrationInfo.Description = $"Enbrea Export to {provider}";
                    taskDefinition.Settings.Enabled = false;
                    taskDefinition.Principal.Id = NetServiceAccount;
                    taskDefinition.Principal.LogonType = TaskLogonType.ServiceAccount;

                    var weekTrigger = new WeeklyTrigger
                    {
                        StartBoundary = DateTime.Now.AddSeconds(15),
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

                   taskFolder.RegisterTaskDefinition(taskName, taskDefinition, TaskCreation.CreateOrUpdate,
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

        public void CreateImportTask(string configFile, Configuration config, ImportProvider provider, uint interval, string suffix)
        {
            _consoleWriter.StartProgress($"Create or update an import task from {provider}");
            try
            {
                Directory.CreateDirectory(GetLogFolderName(config, provider));

                var taskName = GetEnbreaTaskName(provider, suffix);
                var task = GetTask(taskName);

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

                    taskDefinition.RegistrationInfo.Description = $"Enbrea Import from {provider}";
                    taskDefinition.Settings.Enabled = false;
                    taskDefinition.Principal.Id = NetServiceAccount;
                    taskDefinition.Principal.LogonType = TaskLogonType.ServiceAccount;

                    var weekTrigger = new WeeklyTrigger
                    {
                        StartBoundary = DateTime.Now.AddSeconds(15),
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

                    taskFolder.RegisterTaskDefinition(taskName, taskDefinition, TaskCreation.CreateOrUpdate,
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

                    foreach (ImportProvider provider in Enum.GetValues<ImportProvider>())
                    {
                        tasks.AddRange(FindAllTasks(taskFolder, provider));
                    }

                    foreach (ExportProvider provider in Enum.GetValues<ExportProvider>())
                    {
                        tasks.AddRange(FindAllTasks(taskFolder, provider));
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

        public void DeleteExportTask(ExportProvider provider, string suffix)
        {
            _consoleWriter.StartProgress("Delete export task");
            try
            {
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var taskName = GetEnbreaTaskName(provider, suffix);
                    var task = GetTask(taskName);

                    if (task != null)
                    {
                        taskFolder.DeleteTask(task.Name);

                        _consoleWriter.FinishProgress().Success($"Task {task.Name} successfully deleted");
                    }
                    else
                    {
                        throw new ScheduleException($"The task {taskName} does not exists");
                    }
                }
                else
                {
                    throw new ScheduleException($"No Enbrea tasks found");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void DeleteImportTask(ImportProvider provider, string suffix)
        {
            _consoleWriter.StartProgress("Delete import task");
            try
            {
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var taskName = GetEnbreaTaskName(provider, suffix);
                    var task = GetTask(taskName);

                    if (task != null)
                    {
                        taskFolder.DeleteTask(task.Name);

                        _consoleWriter.FinishProgress().Success($"Task {task.Name} successfully deleted");
                    }
                    else
                    {
                        throw new ScheduleException($"The task {taskName} does not exists");
                    }
                }
                else
                {
                    throw new ScheduleException($"No Enbrea tasks found");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void DisableExportTask(ExportProvider provider, string suffix)
        {
            _consoleWriter.StartProgress("Disable export task");
            try
            {
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var taskName = GetEnbreaTaskName(provider, suffix);
                    var task = GetTask(taskName);

                    if (task != null)
                    {
                        task.Definition.Settings.Enabled = false;
                        task.RegisterChanges();

                        _consoleWriter.FinishProgress().Success($"Task {task.Name} successfully updated");
                    }
                    else
                    {
                        throw new ScheduleException($"The task {taskName} does not exists");
                    }
                }
                else
                {
                    throw new ScheduleException($"No Enbrea tasks found");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void DisableImportTask(ImportProvider provider, string suffix)
        {
            _consoleWriter.StartProgress("Disable import task");
            try
            {
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var taskName = GetEnbreaTaskName(provider, suffix);
                    var task = GetTask(taskName);

                    if (task != null)
                    {
                        task.Definition.Settings.Enabled = false;
                        task.RegisterChanges();

                        _consoleWriter.FinishProgress().Success($"Task {task.Name} successfully updated");
                    }
                    else
                    {
                        throw new ScheduleException($"The task {taskName} does not exists");
                    }
                }
                else
                {
                    throw new ScheduleException($"No Enbrea tasks found");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void EnableExportTask(ExportProvider provider, string suffix)
        {
            _consoleWriter.StartProgress("Enable export task");
            try
            {
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var taskName = GetEnbreaTaskName(provider, suffix);
                    var task = GetTask(taskName);

                    if (task != null)
                    {
                        if (task.Definition.Triggers.Count > 0)
                        {
                            task.Definition.Triggers[0].StartBoundary = DateTime.Now.AddSeconds(15);
                        }

                        task.Definition.Settings.Enabled = true;
                        task.RegisterChanges();

                        _consoleWriter.FinishProgress().Success($"Task {task.Name} successfully updated");
                    }
                    else
                    {
                        throw new ScheduleException($"The task {taskName} does not exists");
                    }
                }
                else
                {
                    throw new ScheduleException($"No Enbrea tasks found");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public void EnableImportTask(ImportProvider provider, string suffix)
        {
            _consoleWriter.StartProgress("Enable import task");
            try
            {
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var taskName = GetEnbreaTaskName(provider, suffix);
                    var task = GetTask(taskName);

                    if (task != null)
                    {
                        if (task.Definition.Triggers.Count > 0)
                        {
                            task.Definition.Triggers[0].StartBoundary = DateTime.Now.AddSeconds(15);
                        }

                        task.Definition.Settings.Enabled = true;
                        task.RegisterChanges();

                        _consoleWriter.FinishProgress().Success($"Task {task.Name} successfully updated");
                    }
                    else
                    {
                        throw new ScheduleException($"The task {taskName} does not exists");
                    }
                }
                else
                {
                    throw new ScheduleException($"No Enbrea tasks found");
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
                var taskFolder = GetEnbreaTaskFolder();

                if (taskFolder != null)
                {
                    var tasks = new List<Task>();

                    foreach (ImportProvider provider in Enum.GetValues(typeof(ImportProvider)))
                    {
                        tasks.AddRange(FindAllTasks(taskFolder, provider));
                    }

                    foreach (ExportProvider provider in Enum.GetValues(typeof(ExportProvider)))
                    {
                        tasks.AddRange(FindAllTasks(taskFolder, provider));
                    }

                    _consoleWriter.FinishProgress();

                    if (tasks.Count > 0)
                    {
                        _consoleWriter.NewLine();
                        _consoleWriter.Message($" Enabled | Interval | Task name");
                        _consoleWriter.Message($" ------- | -------- | ---------");

                        foreach (var task in tasks)
                        {
                            _consoleWriter.Message($"{task.Definition.Settings.Enabled, -7} | {task.Definition.Triggers.FirstOrDefault()?.Repetition?.Interval, -8} | {task.Name} ");
                        }

                        _consoleWriter.NewLine();
                    }

                    _consoleWriter.Success($"{tasks.Count} tasks found");
                }
                else
                {
                    throw new ScheduleException($"No Enbrea tasks found");
                }
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

        private TaskCollection FindAllTasks(TaskFolder folder, ImportProvider provider)
        {
            return folder.GetTasks(new Regex(@$"enbrea\.from\.{provider}(\..+)?"));
        }

        private TaskCollection FindAllTasks(TaskFolder folder, ExportProvider provider)
        {
            return folder.GetTasks(new Regex(@$"enbrea\.to\.{provider}(\..+)?"));
        }

        private TaskFolder GetEnbreaTaskFolder()
        {
            return TaskService.Instance.GetFolder("Enbrea");
        }

        private string GetEnbreaTaskName(ImportProvider provider, string suffix)
        {
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return $"enbrea.from.{provider}";
            }
            else
            {
                return $"enbrea.from.{provider}.{suffix}";
            }
        }

        private string GetEnbreaTaskName(ExportProvider provider, string suffix)
        {
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return $"enbrea.to.{provider}";
            }
            else
            {
                return $"enbrea.to.{provider}.{suffix}";
            }
        }

        private Task GetTask(string taskName)
        {
            return TaskService.Instance.GetTask($"Enbrea\\{taskName}");
        }
    }
}
