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

using ClosedXML.Excel;
using Enbrea.Cli.Common;
using Enbrea.Ecf;
using Enbrea.Konsoli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.LUSD
{
    public class XlsxExportManager : EcfCustomManager
    {
        private readonly Configuration _config;
        private int _recordCounter = 0;
        private int _tableCounter = 0;

        public XlsxExportManager(Configuration config, ConsoleWriter consoleWriter, CancellationToken cancellationToken)
            : base(config.TargetFolder, consoleWriter, cancellationToken)
        {
            _config = config;
        }

        public async override Task Execute()
        {
            using var xlsStream = new FileStream(_config.DataFile, FileMode.Open, FileAccess.Read, FileShare.None);
            using var xlsDocument = new XLWorkbook(xlsStream);

            // Init counters
            _tableCounter = 0;
            _recordCounter = 0;

            // Report status
            _consoleWriter.Caption("Export from LUSD");

            // Preperation
            PrepareEcfFolder();

            // Education
            await Execute(xlsDocument, EcfTables.Subjects, ExportSubjects);
            await Execute(xlsDocument, EcfTables.SchoolClasses, ExportSchoolClasses);
            await Execute(xlsDocument, EcfTables.Students, ExportStudents);
            await Execute(xlsDocument, EcfTables.Teachers, ExportTeachers);
            await Execute(xlsDocument, EcfTables.StudentSchoolClassAttendances, ExportStudentSchoolClassAttendances);
            await Execute(xlsDocument, EcfTables.StudentSubjects, ExportStudentSubjects);

            // Report status
            _consoleWriter.Success($"{_tableCounter} table(s) and {_recordCounter} record(s) extracted").NewLine();
        }

        private async Task Execute(IXLWorkbook xlsDocument, string ecfTableName, Func<XlsxReader, EcfTableWriter, Task<int>> action)
        {
            // Report status
            _consoleWriter.StartProgress($"Extracting {ecfTableName}...");
            try
            {
                // Create Excel reader
                var xlsxReader = new XlsxReader(xlsDocument, _config?.XlsxSheetName, _config?.XlsxFirstRowNumber, _config?.XlsxLastRowNumber);

                // Calculate ECF file name
                var ecfFileName = Path.ChangeExtension(Path.Combine(GetEcfFolderName(), ecfTableName), "csv");

                // Create ECF file stream for export
                using var strWriter = new StreamWriter(ecfFileName, false, Encoding.UTF8);

                // Create ECF writer for export
                var ecfTableWriter = new EcfTableWriter(strWriter);

                // Call table specific action
                var ecfRecordCounter = await action(xlsxReader, ecfTableWriter);

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

        private async Task<int> ExportSchoolClasses(XlsxReader xlsxReader, EcfTableWriter ecfTableWriter)
        {
            var ecfCache = new HashSet<string>();
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code);

            while (xlsxReader.ReadLine())
            {
                var schoolClass = new ExportSchoolClass(_config, xlsxReader);

                if (!string.IsNullOrEmpty(schoolClass.Id) && !ecfCache.Contains(schoolClass.Id))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, schoolClass.Id);
                    ecfTableWriter.SetValue(EcfHeaders.Code, schoolClass.Code);

                    await ecfTableWriter.WriteAsync();

                    ecfCache.Add(schoolClass.Id);

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudents(XlsxReader xlsxReader, EcfTableWriter ecfTableWriter)
        {
            var ecfCache = new HashSet<string>();
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.LastName,
                EcfHeaders.FirstName,
                EcfHeaders.MiddleName,
                EcfHeaders.Gender,
                EcfHeaders.Birthdate);

            while (xlsxReader.ReadLine())
            {
                var student = new ExportStudent(_config, xlsxReader);

                if (!ecfCache.Contains(student.Id))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, student.Id);
                    ecfTableWriter.SetValue(EcfHeaders.LastName, student.LastName);
                    ecfTableWriter.SetValue(EcfHeaders.FirstName, student.FirstName);
                    ecfTableWriter.SetValue(EcfHeaders.MiddleName, student.MiddleName);
                    ecfTableWriter.SetValue(EcfHeaders.Gender, student.Gender);
                    ecfTableWriter.SetValue(EcfHeaders.Birthdate, student.BirthDate);

                    await ecfTableWriter.WriteAsync();

                    ecfCache.Add(student.Id);

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSchoolClassAttendances(XlsxReader xlsxReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.StudentId,
                EcfHeaders.SchoolClassId);

            while (xlsxReader.ReadLine())
            {
                var student = new ExportStudent(_config, xlsxReader);
                var schoolClass = new ExportSchoolClass(_config, xlsxReader);

                if (!string.IsNullOrEmpty(schoolClass.Id))
                {
                    ecfTableWriter.SetValue(EcfHeaders.StudentId, student.Id);
                    ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, schoolClass.Id);

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSubjects(XlsxReader xlsxReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.StudentId,
                EcfHeaders.SchoolClassId,
                EcfHeaders.SubjectId,
                EcfHeaders.TeacherId);

            while (xlsxReader.ReadLine())
            {
                var student = new ExportStudent(_config, xlsxReader);
                var schoolClass = new ExportSchoolClass(_config, xlsxReader);
                var teacher = new ExportTeacher(_config, xlsxReader);

                if (!string.IsNullOrEmpty(schoolClass.Id))
                {
                    var subject = new ExportSubject(_config, xlsxReader);

                    if (!string.IsNullOrEmpty(subject.Id))
                    {
                        ecfTableWriter.SetValue(EcfHeaders.StudentId, student.Id);
                        ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, schoolClass.Id);
                        ecfTableWriter.SetValue(EcfHeaders.SubjectId, subject.Id);
                        ecfTableWriter.SetValue(EcfHeaders.TeacherId, teacher.Id);

                        await ecfTableWriter.WriteAsync();

                        _consoleWriter.ContinueProgress(++ecfRecordCounter);
                    }
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSubjects(XlsxReader xlsxReader, EcfTableWriter ecfTableWriter)
        {
            var ecfCache = new HashSet<string>();
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code);

            while (xlsxReader.ReadLine())
            {
                var subject = new ExportSubject(_config, xlsxReader);

                if (!string.IsNullOrEmpty(subject.Id) && !ecfCache.Contains(subject.Id))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, subject.Id);
                    ecfTableWriter.SetValue(EcfHeaders.Code, subject.Code);

                    await ecfTableWriter.WriteAsync();

                    ecfCache.Add(subject.Id);

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportTeachers(XlsxReader xlsxReader, EcfTableWriter ecfTableWriter)
        {
            var ecfCache = new HashSet<string>();
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code);

            while (xlsxReader.ReadLine())
            {
                var teacher = new ExportTeacher(_config, xlsxReader);

                if (!string.IsNullOrEmpty(teacher.Id) && !ecfCache.Contains(teacher.Id))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, teacher.Id);
                    ecfTableWriter.SetValue(EcfHeaders.Code, teacher.Code);

                    await ecfTableWriter.WriteAsync();

                    ecfCache.Add(teacher.Id);

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }
    }
}
