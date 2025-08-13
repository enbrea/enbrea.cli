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

using System;
using System.CommandLine;
using System.IO;

namespace Enbrea.Cli
{
    public static class CommandDefinitions
    {
        public static Command Backup()
        {
            var command = new Command("backup-offline", "Creates and downloads an Enbrea database backup for offline use")
            {
                new Option<FileInfo>("--config", "-c")
                {
                    Description = "Path to existing JSON configuration file",
                    Required = true
                },
                new Option<FileInfo>("--out", "-o")
                {
                    Description = "Path to output file",
                    Required = true
                }
            };

            command.SetAction(parseResult => CommandHandlers.BackupOffline(
                parseResult.GetValue(command.Options[0] as Option<FileInfo>), 
                parseResult.GetValue(command.Options[1] as Option<FileInfo>))
            );

            return command;
        }

        public static Command CreateExportTask()
        {
            var command = new Command("create-export-task", "Schedules a task for a data export from an Enbrea instance")
            {
                new Option<FileInfo>("--config", "-c")
                {
                    Description = "Path to existing JSON configuration file",
                    Required = true
                },
                new Option<ExportProvider>("--provider", "-p")
                {
                    Description = "Name of external data provider",
                    Required = true
                },
                new Option<uint>("--interval", "-i")
                {
                    Description = "Time interval in minutes",
                    Required = true,
                    DefaultValueFactory = _ => 10
                },
                new Option<string>("--suffix", "-s")
                {
                    Description = "Suffix of the task name",
                    Required = false
                }
            };

            command.SetAction(parseResult => CommandHandlers.CreateExportTask(
                parseResult.GetValue(command.Options[0] as Option<FileInfo>),
                parseResult.GetValue(command.Options[1] as Option<ExportProvider>),
                parseResult.GetValue(command.Options[2] as Option<uint>),
                parseResult.GetValue(command.Options[3] as Option<string>))
            );

            return command;
        }

        public static Command CreateImportTask()
        {
            var command = new Command("create-import-task", "Schedules a task for a data import to an Enbrea instance")
            {
                new Option<FileInfo>("--config", "-c")
                {
                    Description = "Path to existing JSON configuration file",
                    Required = true
                },
                new Option<ImportProvider>("--provider", "-p")
                {
                    Description = "Name of external data provider",
                    Required = true
                },
                new Option<uint>("--interval", "-i")
                {
                    Description = "Time interval in minutes",
                    Required = true,
                    DefaultValueFactory = _ => 10
                },
                new Option<string>("--suffix", "-s")
                {
                    Description = "Suffix of the task name",
                    Required = false
                }
            };

            command.SetAction(parseResult => CommandHandlers.CreateImportTask(
                parseResult.GetValue(command.Options[0] as Option<FileInfo>),
                parseResult.GetValue(command.Options[1] as Option<ImportProvider>),
                parseResult.GetValue(command.Options[2] as Option<uint>),
                parseResult.GetValue(command.Options[3] as Option<string>))
            );
            
            return command;
        }

        public static Command CreateSnaphot()
        {
            var command = new Command("create-snapshot", "Create a Enbrea database snapshot")
            {
                new Option<FileInfo>("--config", "-c")
                {
                    Description = "Path to existing JSON configuration file",
                    Required = true
                }
            };

            command.SetAction(parseResult => CommandHandlers.CreateSnapshot(
                parseResult.GetValue(command.Options[0] as Option<FileInfo>))
            );

            return command;
        }

        public static Command DeleteAllTasks()
        {
            var command = new Command("delete-tasks", "Deletes all scheduled import and export tasks for Enbrea");

            command.SetAction(parseResult => CommandHandlers.DeleteAllTasks());

            return command;
        }

        public static Command DeleteExportTask()
        {
            var command = new Command("delete-export-task", "Deletes a scheduled export task for Enbrea")
            {
                new Option<ExportProvider>("--provider", "-p")
                {
                    Description = "Name of external data provider",
                    Required = true
                },
                new Option<string>("--suffix", "-s")
                {
                    Description = "Suffix of the task name",
                    Required = false
                }
            };

            command.SetAction(parseResult => CommandHandlers.DeleteExportTask(
                parseResult.GetValue(command.Options[0] as Option<ExportProvider>),
                parseResult.GetValue(command.Options[1] as Option<string>))
            );

            return command;
        }

        public static Command DeleteImportTask()
        {
            var command = new Command("delete-import-task", "Deletes a scheduled import task for Enbrea")
            {
                new Option<ImportProvider>("--provider", "-p")
                {
                    Description = "Name of external data provider",
                    Required = true
                },
                new Option<string>("--suffix", "-s")
                {
                    Description = "Suffix of the task name",
                    Required = false
                }
            };

            command.SetAction(parseResult => CommandHandlers.DeleteImportTask(
                parseResult.GetValue(command.Options[0] as Option<ImportProvider>),
                parseResult.GetValue(command.Options[1] as Option<string>))
            );

            return command;
        }

        public static Command DeleteSnaphot()
        {
            var command = new Command("delete-snapshot", "Delete an Enbrea database snapshot"){
                new Option<FileInfo>("--config", "-c")
                {
                    Description = "Path to existing JSON configuration file",
                    Required = true
                },
                new Option<Guid>("--id", "-id")
                {
                    Description = "Unique ID of the snapshot",
                    Required = true
                }
            };

            command.SetAction(parseResult => CommandHandlers.DeleteSnapshot(
                parseResult.GetValue(command.Options[0] as Option<FileInfo>),
                parseResult.GetValue(command.Options[1] as Option<Guid>))
            );

            return command;
        }

        public static Command DisableExportTask()
        {
            var command = new Command("disable-export-task", "Deactivates a scheduled export task for Enbrea")
            {
                new Option<ExportProvider>("--provider", "-p")
                {
                    Description = "Name of external data provider",
                    Required = true
                },
                new Option<string>("--suffix", "-s")
                {
                    Description = "Suffix of the task name",
                    Required = false
                }
            };

            command.SetAction(parseResult => CommandHandlers.DisableExportTask(
                parseResult.GetValue(command.Options[0] as Option<ExportProvider>),
                parseResult.GetValue(command.Options[1] as Option<string>))
            );

            return command;
        }

        public static Command DisableImportTask()
        {
            var command = new Command("disable-import-task", "Deactivates a scheduled import task for Enbrea")
            {
                new Option<ImportProvider>("--provider", "-p")
                {
                    Description = "Name of external data provider",
                    Required = true
                },
                new Option<string>("--suffix", "-s")
                {
                    Description = "Suffix of the task name",
                    Required = false
                }
            };

            command.SetAction(parseResult => CommandHandlers.DisableImportTask(
                parseResult.GetValue(command.Options[0] as Option<ImportProvider>),
                parseResult.GetValue(command.Options[1] as Option<string>))
            );

            return command;
        }

        public static Command EnableExportTask()
        {
            var command = new Command("enable-export-task", "Activates a scheduled export task for Enbrea")
            {
                new Option<ExportProvider>("--provider", "-p")
                {
                    Description = "Name of external data provider",
                    Required = true
                },
                new Option<string>("--suffix", "-s")
                {
                    Description = "Suffix of the task name",
                    Required = false
                }
            };

            command.SetAction(parseResult => CommandHandlers.EnableExportTask(
                parseResult.GetValue(command.Options[0] as Option<ExportProvider>),
                parseResult.GetValue(command.Options[1] as Option<string>))
            );

            return command;
        }

        public static Command EnableImportTask()
        {
            var command = new Command("enable-import-task", "Activates a scheduled import task for Enbrea")
            {
                new Option<ImportProvider>("--provider", "-p")
                {
                    Description = "Name of external data provider",
                    Required = true
                },
                new Option<string>("--suffix", "-s")
                {
                    Description = "Suffix of the task name",
                    Required = false
                }
            };

            command.SetAction(parseResult => CommandHandlers.EnableImportTask(
                parseResult.GetValue(command.Options[0] as Option<ImportProvider>),
                parseResult.GetValue(command.Options[1] as Option<string>))
            );

            return command;
        }

        public static Command Export()
        {
            var command = new Command("export", "Exports data from an Enbrea instance to an external provider")
            {
                new Option<FileInfo>("--config", "-c")
                {
                    Description = "Path to existing JSON configuration file",
                    Required = true
                },
                new Option<ExportProvider>("--provider", "-p")
                {
                    Description = "Name of external data provider",
                    Required = true
                },
                new Option<bool>("--skip-enbrea")
                {
                    Description = "Skip export of ECF files from Enbrea",
                    Required = false,
                    DefaultValueFactory = _ => false
                },
                new Option<bool>("--skip-provider")
                {
                    Description = "Skip import of ECF files to external provider",
                    Required = false,
                    DefaultValueFactory = _ => false
                },
                new Option<string>("--log", "-l")
                {
                    Description = "Log file folder",
                    Required = false,
                    DefaultValueFactory = _ => null
                }
            };

            command.SetAction(parseResult => CommandHandlers.Export(
                parseResult.GetValue(command.Options[0] as Option<FileInfo>),
                parseResult.GetValue(command.Options[1] as Option<ExportProvider>),
                parseResult.GetValue(command.Options[2] as Option<bool>),
                parseResult.GetValue(command.Options[3] as Option<bool>),
                parseResult.GetValue(command.Options[4] as Option<string>))
            );

            return command;
        }

        public static Command Import()
        {
            var command = new Command("import", "Imports data from external provider to an Enbrea instance")
            {
                new Option<FileInfo>("--config", "-c")
                {
                    Description = "Path to existing JSON configuration file\"",
                    Required = true
                },
                new Option<ImportProvider>("--provider", "-p")
                {
                    Description = "Name of external data provider",
                    Required = true
                },
                new Option<ImportBehaviour>("--behaviour", "-b")
                {
                    Description = "Import behaviour",
                    Required = false,
                    DefaultValueFactory = _ => ImportBehaviour.diff
                },
                new Option<bool>("--skip-provider")
                {
                    Description = "Skip import of ECF files from external provider",
                    Required = false,
                    DefaultValueFactory = _ => false
                },
                new Option<bool>("--skip-enbrea")
                {
                    Description = "Skip import of ECF files to Enbrea",
                    Required = false,
                    DefaultValueFactory = _ => false
                },
                new Option<bool>("--skip-snapshot")
                {
                    Description = "Skip creating of a snapshot",
                    Required = false,
                    DefaultValueFactory = _ => false
                },
                new Option<string>("--log", "-l")
                {
                    Description = "Log file folder",
                    Required = false,
                    DefaultValueFactory = _ => null
                }
            };

            command.SetAction(parseResult => CommandHandlers.Import(
                parseResult.GetValue(command.Options[0] as Option<FileInfo>),
                parseResult.GetValue(command.Options[1] as Option<ImportProvider>),
                parseResult.GetValue(command.Options[2] as Option<ImportBehaviour>),
                parseResult.GetValue(command.Options[3] as Option<bool>),
                parseResult.GetValue(command.Options[4] as Option<bool>),
                parseResult.GetValue(command.Options[5] as Option<bool>),
                parseResult.GetValue(command.Options[6] as Option<string>))
            );

            return command;
        }

        public static Command Init()
        {
            var command = new Command("init", "Create an Enbrea configuration file template")
            {
                new Option<FileInfo>("--config", "-c")
                {
                    Description = "Path for new JSON configuration file",
                    Required = true
                }
            };

            command.SetAction(parseResult => CommandHandlers.Init(
                parseResult.GetValue(command.Options[0] as Option<FileInfo>))
            );

            return command;
        }

        public static Command ListAllTasks()
        {
            var command = new Command("list-tasks", "Get list of all scheduled import and export tasks for Enbrea");

            command.SetAction(parseResult => CommandHandlers.ListAllTasks());

            return command;
        }

        public static Command ListSchoolTerms()
        {
            var command = new Command("list-schoolterms", "Get list of Enbrea school terms"){
                new Option<FileInfo>("--config", "-c")
                {
                    Description = "Path to existing JSON configuration file",
                    Required = true
                }
            };

            command.SetAction(parseResult => CommandHandlers.ListSchoolTerms(
                parseResult.GetValue(command.Options[0] as Option<FileInfo>))
            );

            return command;
        }

        public static Command ListSnaphots()
        {
            var command = new Command("list-snapshots", "Get list of Enbrea database snapshots"){
                new Option<FileInfo>("--config", "-c")
                {
                    Description = "Path to existing JSON configuration file",
                    Required = true
                }
            };

            command.SetAction(parseResult => CommandHandlers.ListSnapshots(
                parseResult.GetValue(command.Options[0] as Option<FileInfo>))
            );

            return command;
        }

        public static Command RestoreSnaphot()
        {
            var command = new Command("restore-snapshot", "Restore an Enbrea database from an Enbrea snapshot"){
                new Option<FileInfo>("--config", "-c")
                {
                    Description = "Path to existing JSON configuration file",
                    Required = true
                },
                new Option<Guid>("--id", "-id")
                {
                    Description = "Unique ID of the snapshot",
                    Required = true
                }
            };

            command.SetAction(parseResult => CommandHandlers.RestoreSnapshot(
                parseResult.GetValue(command.Options[0] as Option<FileInfo>),
                parseResult.GetValue(command.Options[1] as Option<Guid>))
            );

            return command;
        }
    }
}
