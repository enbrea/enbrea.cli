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
                new Option<FileInfo>(new[] { "--config", "-c" }, "Path to existing JSON configuration file")
                {
                    IsRequired = true
                },
                new Option<FileInfo>(new[] { "--out", "-o" }, "Path to output file")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.BackupOffline,
                command.Options[0] as Option<FileInfo>,
                command.Options[1] as Option<FileInfo>
            );

            return command;
        }

        public static Command CreateExportTask()
        {
            var command = new Command("create-export-task", "Schedules a task for a data export from an Enbrea instance")
            {
                new Option<FileInfo>(new[] { "--config", "-c" }, "Path to existing JSON configuration file")
                {
                    IsRequired = true
                },
                new Option<ExportProvider>(new[] { "--provider", "-p" }, "Name of external data provider")
                {
                    IsRequired = true
                },
                new Option<uint>(new[] { "--interval", "-i" }, delegate() { return 10; }, "Time interval in minutes")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.CreateExportTask,
                command.Options[0] as Option<FileInfo>,
                command.Options[1] as Option<ExportProvider>,
                command.Options[2] as Option<uint>);

            return command;
        }

        public static Command CreateImportTask()
        {
            var command = new Command("create-import-task", "Schedules a task for a data import to an Enbrea instance")
            {
                new Option<FileInfo>(new[] { "--config", "-c" }, "Path to existing JSON configuration file")
                {
                    IsRequired = true
                },
                new Option<ImportProvider>(new[] { "--provider", "-p" }, "Name of external data provider")
                {
                    IsRequired = true
                },
                new Option<uint>(new[] { "--interval", "-i" }, delegate() { return 10; }, "Time interval in minutes")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.CreateImportTask,
                command.Options[0] as Option<FileInfo>,
                command.Options[1] as Option<ImportProvider>,
                command.Options[2] as Option<uint>);

            return command;
        }

        public static Command CreateSnaphot()
        {
            var command = new Command("create-snapshot", "Create a Enbrea database snapshot")
            {
                new Option<FileInfo>(new[] { "--config", "-c" }, "Path to existing JSON configuration file")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.CreateSnapshot,
                command.Options[0] as Option<FileInfo>
            );

            return command;
        }

        public static Command DeleteAllTasks()
        {
            var command = new Command("delete-tasks", "Deletes all scheduled import and export tasks for Enbrea");

            command.SetHandler(CommandHandlers.DeleteAllTasks);

            return command;
        }

        public static Command DeleteExportTask()
        {
            var command = new Command("delete-export-task", "Deletes a scheduled export task for Enbrea")
            {
                new Option<ExportProvider>(new[] { "--provider", "-p" }, "Name of external data provider")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.DeleteExportTask,
                command.Options[0] as Option<ExportProvider>);

            return command;
        }

        public static Command DeleteImportTask()
        {
            var command = new Command("delete-import-task", "Deletes a scheduled import task for Enbrea")
            {
                new Option<ImportProvider>(new[] { "--provider", "-p" }, "Name of external data provider")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.DeleteImportTask,
                command.Options[0] as Option<ImportProvider>);

            return command;
        }

        public static Command DeleteSnaphot()
        {
            var command = new Command("delete-snapshot", "Delete an Enbrea database snapshot"){
                new Option<FileInfo>(new[] { "--config", "-c" }, "Path to existing JSON configuration file")
                {
                    IsRequired = true
                },
                new Option<Guid>(new[] { "--id", "-id" }, "Unique ID of the snapshot")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.DeleteSnapshot,
                command.Options[0] as Option<FileInfo>,
                command.Options[1] as Option<Guid>
            );

            return command;
        }

        public static Command DisableExportTask()
        {
            var command = new Command("disable-export-task", "Deactivates a scheduled export task for Enbrea")
            {
                new Option<ExportProvider>(new[] { "--provider", "-p" }, "Name of external data provider")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.DisableExportTask,
                command.Options[0] as Option<ExportProvider>);

            return command;
        }

        public static Command DisableImportTask()
        {
            var command = new Command("disable-import-task", "Deactivates a scheduled import task for Enbrea")
            {
                new Option<ImportProvider>(new[] { "--provider", "-p" }, "Name of external data provider")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.DisableImportTask,
                command.Options[0] as Option<ImportProvider>);

            return command;
        }

        public static Command EnableExportTask()
        {
            var command = new Command("enable-export-task", "Activates a scheduled export task for Enbrea")
            {
                new Option<ExportProvider>(new[] { "--provider", "-p" }, "Name of external data provider")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.EnableExportTask,
                command.Options[0] as Option<ExportProvider>);

            return command;
        }

        public static Command EnableImportTask()
        {
            var command = new Command("enable-import-task", "Activates a scheduled import task for Enbrea")
            {
                new Option<ImportProvider>(new[] { "--provider", "-p" }, "Name of external data provider")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.EnableImportTask,
                command.Options[0] as Option<ImportProvider>);

            return command;
        }

        public static Command Export()
        {
            var command = new Command("export", "Exports data from an Enbrea instance to an external provider")
            {
                new Option<FileInfo>(new[] { "--config", "-c" }, "Path to existing JSON configuration file")
                {
                    IsRequired = true
                },
                new Option<ExportProvider>(new[] { "--provider", "-p" }, "Name of external data provider")
                {
                    IsRequired = true
                },
                new Option<bool>(new[] { "--skip-enbrea" }, delegate() { return false; }, "Skip export of ECF files from Enbrea")
                {
                    IsRequired = false
                },
                new Option<bool>(new[] { "--skip-provider" }, delegate() { return false; }, "Skip import of ECF files to external provider")
                {
                    IsRequired = false
                },
                new Option<string>(new[] { "--log", "-l" }, delegate () { return null; }, "Log file folder")
                {
                    IsRequired = false
                }
            };

            command.SetHandler(CommandHandlers.Export,
                command.Options[0] as Option<FileInfo>,
                command.Options[1] as Option<ExportProvider>,
                command.Options[2] as Option<bool>,
                command.Options[3] as Option<bool>,
                command.Options[4] as Option<string>);

            return command;
        }

        public static Command Import()
        {
            var command = new Command("import", "Imports data from external provider to an Enbrea instance")
            {
                new Option<FileInfo>(new[] { "--config", "-c" }, "Path to existing JSON configuration file")
                {
                    IsRequired = true
                },
                new Option<ImportProvider>(new[] { "--provider", "-p" }, "Name of external data provider")
                {
                    IsRequired = true
                },
                new Option<bool>(new[] { "--skip-provider" }, delegate() { return false; }, "Skip import of ECF files from external provider")
                {
                    IsRequired = false
                },
                new Option<bool>(new[] { "--skip-enbrea" }, delegate() { return false; }, "Skip import of ECF files to Enbrea")
                {
                    IsRequired = false
                },
                new Option<bool>(new[] { "--skip-snapshot" }, delegate() { return false; }, "Skip creating of a snapshot")
                {
                    IsRequired = false
                },
                new Option<string>(new[] { "--log", "-l" }, delegate () { return null; }, "Log file folder")
                {
                    IsRequired = false
                }
            };

            command.SetHandler(CommandHandlers.Import,
                command.Options[0] as Option<FileInfo>,
                command.Options[1] as Option<ImportProvider>,
                command.Options[2] as Option<bool>,
                command.Options[3] as Option<bool>,
                command.Options[4] as Option<bool>,
                command.Options[5] as Option<string>);

            return command;
        }

        public static Command Init()
        {
            var command = new Command("init", "Create an Enbrea configuration file template")
            {
                new Option<FileInfo>(new[] { "--config", "-c" }, "Path for new JSON configuration file")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.Init,
                command.Options[0] as Option<FileInfo>
            );

            return command;
        }

        public static Command ListAllTasks()
        {
            var command = new Command("list-tasks", "Get list of all scheduled import and export tasks for Enbrea");

            command.SetHandler(CommandHandlers.ListAllTasks);

            return command;
        }

        public static Command ListSchoolTerms()
        {
            var command = new Command("list-schoolterms", "Get list of Enbrea school terms"){
                new Option<FileInfo>(new[] { "--config", "-c" }, "Path to existing JSON configuration file")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.ListSchoolTerms,
                command.Options[0] as Option<FileInfo>
            );

            return command;
        }

        public static Command ListSnaphots()
        {
            var command = new Command("list-snapshots", "Get list of Enbrea database snapshots"){
                new Option<FileInfo>(new[] { "--config", "-c" }, "Path to existing JSON configuration file")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.ListSnapshots,
                command.Options[0] as Option<FileInfo>
            );

            return command;
        }

        public static Command RestoreSnaphot()
        {
            var command = new Command("restore-snapshot", "Restore an Enbrea database from an Enbrea snapshot"){
                new Option<FileInfo>(new[] { "--config", "-c" }, "Path to existing JSON configuration file")
                {
                    IsRequired = true
                },
                new Option<Guid>(new[] { "--id", "-id" }, "Unique ID of the snapshot")
                {
                    IsRequired = true
                }
            };

            command.SetHandler(CommandHandlers.RestoreSnapshot,
                command.Options[0] as Option<FileInfo>,
                command.Options[1] as Option<Guid>
            );

            return command;
        }
    }
}
