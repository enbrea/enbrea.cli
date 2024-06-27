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
using Enbrea.Csv;
using Enbrea.Ecf;
using Enbrea.Edoosys.Db;
using Enbrea.Konsoli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.Edoosys
{
    public class DbExportManager : EcfCustomManager
    {
        private readonly Configuration _config;
        private HashSet<string> _ecfSchoolClassesCache = [];
        private HashSet<string> _ecfTeacherCache = [];
        private int _recordCounter = 0;
        private int _tableCounter = 0;

        public DbExportManager(Configuration config, ConsoleWriter consoleWriter, CancellationToken cancellationToken)
            : base(config.TargetFolder, consoleWriter, cancellationToken)
        {
            _config = config;
        }

        public async override Task Execute()
        {
            var edoosysDbReader = new EdoosysDbReader(_config.DatabaseConnection);

            // Init counters
            _tableCounter = 0;
            _recordCounter = 0;

            // Report status
            _consoleWriter.Caption("Export from edoo.sys");

            // Preperation
            PrepareEcfFolder();

            // Education
            await Execute(EcfTables.Subjects, edoosysDbReader, ExportSubjects);
            await Execute(EcfTables.Students, edoosysDbReader, ExportStudents);
            await Execute(EcfTables.StudentSchoolClassAttendances, edoosysDbReader, ExportStudentSchoolClassAttendances);
            await Execute(EcfTables.StudentSubjects, edoosysDbReader, ExportStudentSubjects);
            await Execute(EcfTables.SchoolClasses, edoosysDbReader, ExportSchoolClasses);
            await Execute(EcfTables.Teachers, edoosysDbReader, ExportTeachers);

            // Report status
            _consoleWriter.Success($"{_tableCounter} table(s) and {_recordCounter} record(s) extracted").NewLine();
        }

        private static Guid GenerateKey(params string[] array)
        {
            var csvLineBuilder = new CsvLineBuilder();

            foreach (var arrayItem in array)
            {
                csvLineBuilder.Append(arrayItem);
            }
            return IdFactory.CreateIdFromValue(csvLineBuilder.ToString());
        }

        private async Task Execute(string ecfTableName, EdoosysDbReader edoosysDbReader, Func<EdoosysDbReader, EcfTableWriter, Task<int>> action)
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
                var ecfRecordCounter = await action(edoosysDbReader, ecfTableWriter);

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

        private async Task<int> ExportSchoolClasses(EdoosysDbReader edoosysDbReader, EcfTableWriter ecfTableWriter)
        {
            if ((_config.SchoolNo != null) && (_config.SchoolYearCode != null))
            {

                var ecfCache = new HashSet<string>();
                var ecfRecordCounter = 0;

                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code);

                await foreach (var schoolClass in edoosysDbReader.SchoolClassesAsync(_config.SchoolNo, _config.SchoolYearCode))
                {
                    if (_config.NoSchoolClassGroups)
                    {
                        if (!ecfCache.Contains(schoolClass.RootId))
                        {
                            if (_ecfSchoolClassesCache.Contains(schoolClass.RootId))
                            {
                                ecfTableWriter.SetValue(EcfHeaders.Id, schoolClass.RootId);
                                ecfTableWriter.SetValue(EcfHeaders.Code, schoolClass.RootCode);
                                ecfTableWriter.TrySetValue(EcfHeaders.Name, schoolClass.RootName);

                                await ecfTableWriter.WriteAsync();

                                ecfCache.Add(schoolClass.RootId);

                                _consoleWriter.ContinueProgress(++ecfRecordCounter);
                            }
                        }
                    }
                    else
                    {
                        if (_ecfSchoolClassesCache.Contains(schoolClass.Id))
                        {
                            ecfTableWriter.SetValue(EcfHeaders.Id, schoolClass.Id);
                            ecfTableWriter.SetValue(EcfHeaders.Code, $"{schoolClass.RootCode}_{schoolClass.Code}");
                            ecfTableWriter.TrySetValue(EcfHeaders.Name, $"{schoolClass.RootCode}_{schoolClass.Code}");

                            await ecfTableWriter.WriteAsync();

                            _consoleWriter.ContinueProgress(++ecfRecordCounter);
                        }
                    }
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for edoo.sys database defined");
            }
        }

        private async Task<int> ExportStudents(EdoosysDbReader edoosysDbReader, EcfTableWriter ecfTableWriter)
        {
            if ((_config.SchoolNo != null) && (_config.SchoolYearCode != null))
            {
                var ecfRecordCounter = 0;

                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.LastName,
                    EcfHeaders.FirstName,
                    EcfHeaders.Gender,
                    EcfHeaders.Birthdate);

                await foreach (var student in edoosysDbReader.StudentsAsync(_config.SchoolNo, _config.SchoolYearCode, activeStudentsOnly: true))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, student.Id);
                    ecfTableWriter.SetValue(EcfHeaders.LastName, student.Lastname);
                    ecfTableWriter.SetValue(EcfHeaders.FirstName, student.Firstname);
                    ecfTableWriter.TrySetValue(EcfHeaders.Gender, ValueConverter.GetGenderOrDefault(student.Gender));
                    ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, ValueConverter.GetDateOrDefault(student.Birthdate));

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for edoo.sys database defined");
            }
        }

        private async Task<int> ExportStudentSchoolClassAttendances(EdoosysDbReader edoosysDbReader, EcfTableWriter ecfTableWriter)
        {
            if ((_config.SchoolNo != null) && (_config.SchoolYearCode != null))
            {
                var ecfRecordCounter = 0;

                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.StudentId,
                    EcfHeaders.SchoolClassId);

                await foreach (var attendance in edoosysDbReader.StudentSchoolClassAttendancesAsync(_config.SchoolNo, _config.SchoolYearCode, activeStudentsOnly: true))
                {
                    var schoolClassId = _config.NoSchoolClassGroups ? attendance.SchoolClassRootId : attendance.SchoolClassId;

                    ecfTableWriter.SetValue(EcfHeaders.Id, GenerateKey(attendance.StudentId, schoolClassId));
                    ecfTableWriter.SetValue(EcfHeaders.StudentId, attendance.StudentId);
                    ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, schoolClassId);
                    await ecfTableWriter.WriteAsync();

                    _ecfSchoolClassesCache.Add(schoolClassId);

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for edoo.sys database defined");
            }
        }

        private async Task<int> ExportStudentSubjects(EdoosysDbReader edoosysDbReader, EcfTableWriter ecfTableWriter)
        {
            if ((_config.SchoolNo != null) && (_config.SchoolYearCode != null))
            {
                var ecfRecordCounter = 0;

                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.StudentId,
                    EcfHeaders.SchoolClassId,
                    EcfHeaders.SubjectId,
                    EcfHeaders.TeacherId);

                await foreach (var studentSubject in edoosysDbReader.StudentSubjectsAsync(_config.SchoolNo, _config.SchoolYearCode, activeStudentsOnly: true))
                {
                    var schoolClassId = _config.NoSchoolClassGroups ? studentSubject.SchoolClassRootId : studentSubject.SchoolClassId;

                    ecfTableWriter.SetValue(EcfHeaders.Id, GenerateKey(studentSubject.StudentId, studentSubject.SubjectId, schoolClassId, studentSubject.TeacherId));
                    ecfTableWriter.SetValue(EcfHeaders.StudentId, studentSubject.StudentId);
                    ecfTableWriter.SetValue(EcfHeaders.SubjectId, studentSubject.SubjectId);
                    ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, schoolClassId);
                    ecfTableWriter.SetValue(EcfHeaders.TeacherId, studentSubject.TeacherId);

                    await ecfTableWriter.WriteAsync();

                    _ecfTeacherCache.Add(studentSubject.TeacherId);

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for edoo.sys database defined");
            }
        }

        private async Task<int> ExportSubjects(EdoosysDbReader edoosysDbReader, EcfTableWriter ecfTableWriter)
        {
            if ((_config.SchoolNo != null) && (_config.SchoolYearCode != null))
            {
                var ecfRecordCounter = 0;

                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code,
                    EcfHeaders.Name);

                await foreach (var subject in edoosysDbReader.SubjectsAsync(_config.SchoolNo, _config.SchoolYearCode))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, subject.Id);
                    ecfTableWriter.SetValue(EcfHeaders.Code, subject.Code);
                    ecfTableWriter.TrySetValue(EcfHeaders.Name, subject.Name);

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for edoo.sys database defined");
            }
        }

        private async Task<int> ExportTeachers(EdoosysDbReader edoosysDbReader, EcfTableWriter ecfTableWriter)
        {
            if ((_config.SchoolNo != null) && (_config.SchoolYearCode != null))
            {
                var ecfRecordCounter = 0;

                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code,
                    EcfHeaders.LastName,
                    EcfHeaders.FirstName,
                    EcfHeaders.Gender,
                    EcfHeaders.Birthdate);

                await foreach (var teacher in edoosysDbReader.TeachersAsync(_config.SchoolNo, _config.SchoolYearCode))
                {
                    if (_ecfTeacherCache.Contains(teacher.Id))
                    { 
                        ecfTableWriter.SetValue(EcfHeaders.Id, teacher.Id);
                        ecfTableWriter.SetValue(EcfHeaders.Code, teacher.Code);
                        ecfTableWriter.TrySetValue(EcfHeaders.LastName, teacher.Lastname);
                        ecfTableWriter.TrySetValue(EcfHeaders.FirstName, teacher.Firstname);
                        ecfTableWriter.TrySetValue(EcfHeaders.Gender, ValueConverter.GetGenderOrDefault(teacher.Gender));
                        ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, ValueConverter.GetDateOrDefault(teacher.Birthdate));

                        await ecfTableWriter.WriteAsync();

                        _consoleWriter.ContinueProgress(++ecfRecordCounter);
                    }
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for edoo.sys database defined");
            }
        }
    }
}
