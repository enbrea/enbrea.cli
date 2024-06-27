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
using Enbrea.Danis.Db;
using Enbrea.Ecf;
using Enbrea.Konsoli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.Danis
{
    public class ExportManager : EcfCustomManager
    {
        private readonly Configuration _config;
        private int _recordCounter = 0;
        private int _tableCounter = 0;

        public ExportManager(Configuration config, ConsoleWriter consoleWriter, CancellationToken cancellationToken)
            : base(config.TargetFolder, consoleWriter, cancellationToken)
        {
            _config = config;
        }

        public async override Task Execute()
        {
            var danisDbReader = new DanisDbReader(_config.DatabaseConnection);

            // Init counters
            _tableCounter = 0;
            _recordCounter = 0;

            // Report status
            _consoleWriter.Caption("Export from DaNiS");

            // Preperation
            PrepareEcfFolder();

            // Education
            await Execute(EcfTables.Subjects, danisDbReader, ExportSubjects);
            await Execute(EcfTables.Students, danisDbReader, ExportStudents);
            await Execute(EcfTables.StudentSchoolClassAttendances, danisDbReader, ExportStudentSchoolClassAttendances);
            await Execute(EcfTables.StudentSubjects, danisDbReader, ExportStudentSubjects);
            await Execute(EcfTables.SchoolClasses, danisDbReader, ExportSchoolClasses);
            await Execute(EcfTables.Teachers, danisDbReader, ExportTeachers);

            // Report status
            _consoleWriter.Success($"{_tableCounter} table(s) and {_recordCounter} record(s) extracted").NewLine();
        }

        private async Task Execute(string ecfTableName, DanisDbReader danisDbReader, Func<DanisDbReader, EcfTableWriter, Task<int>> action)
        {
            // Report status
            _consoleWriter.StartProgress($"Extracting {ecfTableName}...");
            try
            {
                // Init CSV file stream
                using var ecfStreamWriter = new StreamWriter(Path.ChangeExtension(Path.Combine(GetEcfFolderName(), ecfTableName), "csv"));

                // Init ECF Writer
                var ecfTableWriter = new EcfTableWriter(ecfStreamWriter);

                // Call table specific action
                var ecfRecordCounter = await action(danisDbReader, ecfTableWriter);

                // Inc counters
                _recordCounter += ecfRecordCounter;
                _tableCounter++;

                // Report status
                _consoleWriter.FinishProgress();
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        private async Task<int> ExportSchoolClasses(DanisDbReader danisDbReader, EcfTableWriter ecfTableWriter)
        {
            if (_config.Year > 0)
            {
                var ecfCache = new HashSet<string>();
                var ecfRecordCounter = 0;

                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code);

                await foreach (var group in danisDbReader.GroupsAsync(_config.Year))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, group.Id);
                    ecfTableWriter.SetValue(EcfHeaders.Code, group.Name);

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for DaNiS database defined");
            }
        }

        private async Task<int> ExportStudents(DanisDbReader danisDbReader, EcfTableWriter ecfTableWriter)
        {
            if (_config.Year > 0)
            {
                var ecfRecordCounter = 0;

                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.LastName,
                    EcfHeaders.FirstName,
                    EcfHeaders.Gender,
                    EcfHeaders.Birthdate);

                await foreach (var student in danisDbReader.StudentsAsync(_config.Year))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, student.Id);
                    ecfTableWriter.SetValue(EcfHeaders.LastName, student.LastName);
                    ecfTableWriter.SetValue(EcfHeaders.FirstName, student.FirstNames);
                    ecfTableWriter.TrySetValue(EcfHeaders.Gender, ValueConverter.GetGenderOrDefault(student.Gender));
                    ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, ValueConverter.GetDateOrDefault(student.Birthdate));

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for DaNiS database defined");
            }
        }

        private async Task<int> ExportStudentSchoolClassAttendances(DanisDbReader danisDbReader, EcfTableWriter ecfTableWriter)
        {
            if (_config.Year > 0)
            {
                var ecfRecordCounter = 0;

                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.StudentId,
                    EcfHeaders.SchoolClassId);

                await foreach (var attendance in danisDbReader.StudentGroupAttendancesAsync(_config.Year))
                {
                    if ((attendance.StudentId != null) || (attendance.StudentId != 0))
                    {
                        ecfTableWriter.SetValue(EcfHeaders.Id, attendance.Id);
                        ecfTableWriter.SetValue(EcfHeaders.StudentId, attendance.StudentId);
                        ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, attendance.GroupId);

                        await ecfTableWriter.WriteAsync();

                        _consoleWriter.ContinueProgress(++ecfRecordCounter);
                    }
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for DaNiS database defined");
            }
        }

        private async Task<int> ExportStudentSubjects(DanisDbReader danisDbReader, EcfTableWriter ecfTableWriter)
        {
            if (_config.Year > 0)
            {
                var ecfRecordCounter = 0;

                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.StudentId,
                    EcfHeaders.SchoolClassId,
                    EcfHeaders.SubjectId,
                    EcfHeaders.TeacherId);

                await foreach (var attendance in danisDbReader.StudentCourseAttendancesAsync(_config.Year))
                {
                    if ((attendance.StudentId != null) || (attendance.StudentId != 0))
                    {
                        ecfTableWriter.SetValue(EcfHeaders.Id, attendance.Id);
                        ecfTableWriter.SetValue(EcfHeaders.StudentId, attendance.StudentId);
                        ecfTableWriter.SetValue(EcfHeaders.SubjectId, attendance.SubjectId != 0 ? attendance.GroupId : null);
                        ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, attendance.GroupId != 0 ? attendance.GroupId : null);
                        ecfTableWriter.SetValue(EcfHeaders.TeacherId, attendance.TeacherId != 0 ? attendance.TeacherId : null);

                        await ecfTableWriter.WriteAsync();

                        _consoleWriter.ContinueProgress(++ecfRecordCounter);
                    }
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for DaNiS database defined");
            }
        }

        private async Task<int> ExportSubjects(DanisDbReader danisDbReader, EcfTableWriter ecfTableWriter)
        {
            if (_config.Year > 0)
            {
                var ecfRecordCounter = 0;

                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code,
                    EcfHeaders.Name);

                await foreach (var subject in danisDbReader.SubjectsAsync())
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, subject.Id);
                    ecfTableWriter.SetValue(EcfHeaders.Code, subject.Key);
                    ecfTableWriter.TrySetValue(EcfHeaders.Name, subject.Name);

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for DaNiS database defined");
            }
        }

        private async Task<int> ExportTeachers(DanisDbReader danisDbReader, EcfTableWriter ecfTableWriter)
        {
            if (_config.Year > 0)
            {
                var ecfRecordCounter = 0;

                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code,
                    EcfHeaders.LastName,
                    EcfHeaders.FirstName,
                    EcfHeaders.Gender,
                    EcfHeaders.Birthdate);

                await foreach (var teacher in danisDbReader.TeachersAsync())
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, teacher.Id);
                    ecfTableWriter.SetValue(EcfHeaders.Code, teacher.Code);
                    ecfTableWriter.TrySetValue(EcfHeaders.LastName, teacher.LastName);
                    ecfTableWriter.TrySetValue(EcfHeaders.FirstName, teacher.FirstNames);
                    ecfTableWriter.TrySetValue(EcfHeaders.Gender, ValueConverter.GetGenderOrDefault(teacher.Gender));
                    ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, teacher.Birthdate);

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for DaNiS database defined");
            }
        }
    }
}
