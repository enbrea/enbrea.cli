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
using Enbrea.Csv;
using Enbrea.Ecf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.Edoosys
{
    public class CsvExportManager : EcfCustomManager
    {
        private readonly Configuration _config;
        private int _recordCounter = 0;
        private int _tableCounter = 0;

        public CsvExportManager(Configuration config, ConsoleWriter consoleWriter, CancellationToken cancellationToken)
            : base(config.TargetFolder, consoleWriter, cancellationToken)
        {
            _config = config;
        }

        public async override Task Execute()
        {
            // Init counters
            _tableCounter = 0;
            _recordCounter = 0;

            // Report status
            _consoleWriter.Caption("Export from edoo.sys");

            // Preperation
            PrepareEcfFolder();

            // Education
            await Execute(EcfTables.Teachers, async (r, w) => await ExportTeachers(r, w));
            await Execute(EcfTables.Subjects, async (r, w) => await ExportSubjects(r, w));
            await Execute(EcfTables.SchoolClasses, async (r, w) => await ExportSchoolClasses(r, w));
            await Execute(EcfTables.Students, async (r, w) => await ExportStudents(r, w));
            await Execute(EcfTables.StudentSchoolClassAttendances, async (r, w) => await ExportStudentSchoolClassAttendances(r, w));
            await Execute(EcfTables.StudentSubjects, async (r, w) => await ExportStudentSubjects(r, w));

            // Report status
            _consoleWriter.Success($"{_tableCounter} table(s) and {_recordCounter} record(s) extracted").NewLine();
        }

        private async Task Execute(string ecfTableName, Func<CsvTableReader, EcfTableWriter, Task<int>> action)
        {
            // Report status
            _consoleWriter.StartProgress($"Extracting {ecfTableName}...");
            try
            {
                // Open Edoosys file stream for import
                using var strReader = new StreamReader(_config.CsvExportFile);

                // Create CSV Reader for import
                var csvTableReader = new CsvTableReader(strReader, new CsvConfiguration()
                {
                    Quote = _config.CsvExportQuote,
                    Separator = _config.CsvExportSeparator
                }, new CsvConverterResolver());

                // Generate ECF file name
                var ecfFileName = Path.ChangeExtension(Path.Combine(GetEcfFolderName(), ecfTableName), "csv");

                // Create ECF file stream for export
                using var strWriter = new StreamWriter(ecfFileName, false, Encoding.UTF8);

                // Create ECF Writer for export
                var ecfTableWriter = new EcfTableWriter(strWriter);

                // Call table specific action
                var ecfRecordCounter = await action(csvTableReader, ecfTableWriter);

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

        private async Task<int> ExportSchoolClasses(CsvTableReader csvTableReader, EcfTableWriter ecfTableWriter)
        {
            var ecfCache = new HashSet<string>();
            var ecfRecordCounter = 0;

            await csvTableReader.ReadHeadersAsync();

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code);

            while (await csvTableReader.ReadAsync() > 0)
            {
                var schoolClass = new CsvExportSchoolClass(csvTableReader);

                if (!string.IsNullOrEmpty(schoolClass.Id) && !ecfCache.Contains(schoolClass.Id))
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.Id, schoolClass.Id);
                    ecfTableWriter.TrySetValue(EcfHeaders.Code, schoolClass.Code);

                    await ecfTableWriter.WriteAsync();

                    ecfCache.Add(schoolClass.Id);

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudents(CsvTableReader csvTableReader, EcfTableWriter ecfTableWriter)
        {
            var ecfCache = new HashSet<string>();
            var ecfRecordCounter = 0;

            await csvTableReader.ReadHeadersAsync();

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.LastName,
                EcfHeaders.FirstName,
                EcfHeaders.Gender,
                EcfHeaders.Birthdate);

            while (await csvTableReader.ReadAsync() > 0)
            {
                var student = new CsvExportStudent(csvTableReader);

                if (!ecfCache.Contains(student.Id))
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.Id, student.Id);
                    ecfTableWriter.TrySetValue(EcfHeaders.LastName, student.LastName);
                    ecfTableWriter.TrySetValue(EcfHeaders.FirstName, student.FirstName);
                    ecfTableWriter.TrySetValue(EcfHeaders.Gender, student.Gender);
                    ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, student.BirthDate);

                    await ecfTableWriter.WriteAsync();

                    ecfCache.Add(student.Id);

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSchoolClassAttendances(CsvTableReader csvTableReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await csvTableReader.ReadHeadersAsync();

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.StudentId,
                EcfHeaders.SchoolClassId);

            while (await csvTableReader.ReadAsync() > 0)
            {
                var student = new CsvExportStudent(csvTableReader);
                var schoolClass = new CsvExportSchoolClass(csvTableReader);

                if (!string.IsNullOrEmpty(schoolClass.Id))
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.Id, IdFactory.CreateIdFromValues(student.Id, schoolClass.Id));
                    ecfTableWriter.TrySetValue(EcfHeaders.StudentId, student.Id);
                    ecfTableWriter.TrySetValue(EcfHeaders.SchoolClassId, schoolClass.Id);

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSubjects(CsvTableReader csvTableReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await csvTableReader.ReadHeadersAsync();

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.StudentId,
                EcfHeaders.SchoolClassId,
                EcfHeaders.SubjectId,
                EcfHeaders.TeacherId);

            while (await csvTableReader.ReadAsync() > 0)
            {
                var student = new CsvExportStudent(csvTableReader);
                var schoolClass = new CsvExportSchoolClass(csvTableReader);

                if (csvTableReader.TryGetValue("Alle Lehrkräfte (Kürzel) mit Fach", out var value))
                {
                    var csvLineParser = new CsvLineParser(',');

                    var subValues = csvLineParser.Parse(value);

                    csvLineParser.Configuration.Separator = ' ';

                    foreach (var subValue in subValues)
                    {
                        if (!string.IsNullOrEmpty(subValue))
                        {
                            var subValueParts = csvLineParser.Parse(subValue.Trim());
                            if (subValueParts.Length == 2)
                            {
                                var teacherCode = subValueParts[0];
                                var subjectCode = subValueParts[1];

                                if (!string.IsNullOrEmpty(subjectCode))
                                {
                                    ecfTableWriter.TrySetValue(EcfHeaders.Id, IdFactory.CreateIdFromValues(student.Id, schoolClass.Id, subjectCode, teacherCode));
                                    ecfTableWriter.TrySetValue(EcfHeaders.StudentId, student.Id);
                                    ecfTableWriter.TrySetValue(EcfHeaders.SchoolClassId, schoolClass.Id);
                                    ecfTableWriter.TrySetValue(EcfHeaders.SubjectId, subjectCode);
                                    ecfTableWriter.TrySetValue(EcfHeaders.TeacherId, teacherCode);

                                    await ecfTableWriter.WriteAsync();

                                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                                }
                            }
                        }
                    }
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSubjects(CsvTableReader csvTableReader, EcfTableWriter ecfTableWriter)
        {
            var ecfCache = new HashSet<string>();
            var ecfRecordCounter = 0;

            await csvTableReader.ReadHeadersAsync();

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code);

            while (await csvTableReader.ReadAsync() > 0)
            {
                if (csvTableReader.TryGetValue("Alle Lehrkräfte (Kürzel) mit Fach", out var value))
                {
                    var csvLineParser = new CsvLineParser(',');

                    var subValues = csvLineParser.Parse(value);

                    csvLineParser.Configuration.Separator = ' ';

                    foreach (var subValue in subValues)
                    {
                        if (!string.IsNullOrEmpty(subValue))
                        {
                            var subValueParts = csvLineParser.Parse(subValue.Trim());
                            if (subValueParts.Length == 2)
                            {
                                var subjectCode = subValueParts[1];

                                if (!string.IsNullOrEmpty(subjectCode) && !ecfCache.Contains(subjectCode))
                                {
                                    ecfTableWriter.TrySetValue(EcfHeaders.Id, subjectCode);
                                    ecfTableWriter.TrySetValue(EcfHeaders.Code, subjectCode);

                                    await ecfTableWriter.WriteAsync();

                                    ecfCache.Add(subjectCode);

                                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                                }
                            }
                        }
                    }
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportTeachers(CsvTableReader csvTableReader, EcfTableWriter ecfTableWriter)
        {
            var ecfCache = new HashSet<string>();
            var ecfRecordCounter = 0;

            await csvTableReader.ReadHeadersAsync();

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code);

            while (await csvTableReader.ReadAsync() > 0)
            {
                if (csvTableReader.TryGetValue("Alle Lehrkräfte (Kürzel) mit Fach", out var value))
                {
                    var csvLineParser = new CsvLineParser(',');

                    var subValues = csvLineParser.Parse(value);

                    csvLineParser.Configuration.Separator = ' ';

                    foreach (var subValue in subValues)
                    {
                        if (!string.IsNullOrEmpty(subValue))
                        {
                            var subValueParts = csvLineParser.Parse(subValue.Trim());
                            if (subValueParts.Length == 2)
                            {
                                var teacherCode = subValueParts[0];

                                if (!string.IsNullOrEmpty(teacherCode) && !ecfCache.Contains(teacherCode))
                                {
                                    ecfTableWriter.TrySetValue(EcfHeaders.Id, teacherCode);
                                    ecfTableWriter.TrySetValue(EcfHeaders.Code, teacherCode);

                                    await ecfTableWriter.WriteAsync();

                                    ecfCache.Add(teacherCode);

                                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                                }
                            }
                        }
                    }
                }
            }

            return ecfRecordCounter;
        }
    }
}
