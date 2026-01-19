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
using Enbrea.Ecf;
using Enbrea.Konsoli;
using Enbrea.SchildNRW.Db;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.SchildNRW
{
    public class ExportManager : EcfCustomManager
    {
        private readonly Configuration _config;
        private Dictionary<int, string> _ecfStudentsCache = [];
        private int _recordCounter = 0;
        private int _tableCounter = 0;

        public ExportManager(Configuration config, ConsoleWriter consoleWriter, CancellationToken cancellationToken)
            : base(config.TargetFolder, consoleWriter, cancellationToken)
        {
            _config = config;
        }

        public async override Task Execute()
        {
            var schildNRWDbReader = new SchildNRWDbReader(_config.DataProvider, _config.DatabaseConnection);

            // Init counters
            _tableCounter = 0;
            _recordCounter = 0;

            // Report status
            _consoleWriter.Caption("Export from Schild-NRW");

            // Preperation
            PrepareEcfFolder();

            // Education
            await Execute(EcfTables.Teachers, schildNRWDbReader, ExportTeachers);
            await Execute(EcfTables.Subjects, schildNRWDbReader, ExportSubjects);
            await Execute(EcfTables.SchoolClasses, schildNRWDbReader, ExportSchoolClasses);
            await Execute(EcfTables.Students, schildNRWDbReader, ExportStudents);
            await Execute(EcfTables.StudentSchoolClassAttendances, schildNRWDbReader, ExportStudentSchoolClassAttendances);
            await Execute(EcfTables.StudentSubjects, schildNRWDbReader, ExportStudentSubjects);

            // Report status
            _consoleWriter.Success($"{_tableCounter} table(s) and {_recordCounter} record(s) extracted").NewLine();
        }

        private async Task Execute(string ecfTableName, SchildNRWDbReader schildNRWDbReader, Func<SchildNRWDbReader, EcfTableWriter, Task<int>> action)
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
                var ecfRecordCounter = await action(schildNRWDbReader, ecfTableWriter);

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

        private async Task<int> ExportSchoolClasses(SchildNRWDbReader schildNRWDbReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.Name1);

            await foreach (var schoolClass in schildNRWDbReader.SchoolClassesAsync())
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, schoolClass.Code);
                ecfTableWriter.SetValue(EcfHeaders.Code, schoolClass.Code);
                ecfTableWriter.TrySetValue(EcfHeaders.Name1, schoolClass.Name);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudents(SchildNRWDbReader schildNRWDbReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.LastName,
                EcfHeaders.FirstName,
                EcfHeaders.Gender,
                EcfHeaders.Birthdate,
                EcfHeaders.EmailAddress);

            await foreach (var student in schildNRWDbReader.StudentsAsync(StudentStatus.Active))
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, student.Id.ToString());
                ecfTableWriter.SetValue(EcfHeaders.LastName, student.Lastname);
                ecfTableWriter.SetValue(EcfHeaders.FirstName, student.Firstname);
                ecfTableWriter.TrySetValue(EcfHeaders.Gender, student.GetGenderOrDefault());
                ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, student.GetBirthdateOrDefault());
                ecfTableWriter.TrySetValue(EcfHeaders.EmailAddress, student.Email);

                _ecfStudentsCache.Add(student.Id, student.SchoolClass);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSchoolClassAttendances(SchildNRWDbReader schildNRWDbReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.StudentId,
                EcfHeaders.SchoolClassId);

            await foreach (var student in schildNRWDbReader.StudentsAsync(StudentStatus.Active))
            {
                if (!string.IsNullOrWhiteSpace(student.SchoolClass))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, IdFactory.CreateIdFromValues(student.Id.ToString(), student.SchoolClass));
                    ecfTableWriter.SetValue(EcfHeaders.StudentId, student.Id.ToString());
                    ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, student.SchoolClass);

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSubjects(SchildNRWDbReader schildNRWDbReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.StudentId,
                EcfHeaders.SchoolClassId,
                EcfHeaders.SubjectId,
                EcfHeaders.TeacherId);

            await foreach (var course in schildNRWDbReader.CoursesAsync(_config.SchoolYear, _config.SchoolTerm))
            {
                await foreach (var attendance in schildNRWDbReader.StudentCourseAttendancesAsync(course.Id, _config.SchoolYear, _config.SchoolTerm))
                {
                    if (_ecfStudentsCache.TryGetValue(attendance.StudentId, out string schoolClass) && !string.IsNullOrWhiteSpace(schoolClass))
                    {
                        ecfTableWriter.SetValue(EcfHeaders.Id, IdFactory.CreateIdFromValues(attendance.StudentId.ToString(), course.Id.ToString()));
                        ecfTableWriter.SetValue(EcfHeaders.StudentId, attendance.StudentId.ToString());
                        ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, schoolClass);
                        ecfTableWriter.SetValue(EcfHeaders.SubjectId, course.SubjectId);
                        ecfTableWriter.SetValue(EcfHeaders.TeacherId, course.Teacher);

                        await ecfTableWriter.WriteAsync();

                        _consoleWriter.ContinueProgress(++ecfRecordCounter);
                    }
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSubjects(SchildNRWDbReader schildNRWDbReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.Name,
                EcfHeaders.StatisticalCode);

            await foreach (var subject in schildNRWDbReader.SubjectsAsync())
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, subject.Id.ToString());
                ecfTableWriter.SetValue(EcfHeaders.Code, subject.Code);
                ecfTableWriter.TrySetValue(EcfHeaders.Name, subject.Name);
                ecfTableWriter.TrySetValue(EcfHeaders.StatisticalCode, subject.StatisticalCode);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportTeachers(SchildNRWDbReader schildNRWDbReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.LastName,
                EcfHeaders.FirstName,
                EcfHeaders.Gender,
                EcfHeaders.Birthdate,
                EcfHeaders.EmailAddress);

            await foreach (var teacher in schildNRWDbReader.TeachersAsync())
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, teacher.Code);
                ecfTableWriter.SetValue(EcfHeaders.Code, teacher.Code);
                ecfTableWriter.TrySetValue(EcfHeaders.LastName, teacher.Lastname);
                ecfTableWriter.TrySetValue(EcfHeaders.FirstName, teacher.Firstname);
                ecfTableWriter.TrySetValue(EcfHeaders.Gender, teacher.GetGenderOrDefault());
                ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, teacher.GetBirthdateOrDefault());
                ecfTableWriter.TrySetValue(EcfHeaders.EmailAddress, teacher.Email);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }
    }
}
