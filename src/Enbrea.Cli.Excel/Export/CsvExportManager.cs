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
using Enbrea.Csv;
using Enbrea.Ecf;
using Enbrea.Konsoli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.Excel
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
            _consoleWriter.Caption("Export from CSV");

            // Preperation
            PrepareEcfFolder();

            // Education
            await Execute(EcfTables.Subjects, ExportSubjects);
            await Execute(EcfTables.SchoolClasses, ExportSchoolClasses);
            await Execute(EcfTables.Students, ExportStudents);
            await Execute(EcfTables.StudentSchoolClassAttendances, ExportStudentSchoolClassAttendances);
            await Execute(EcfTables.StudentSubjects, ExportStudentSubjects);

            // Report status
            _consoleWriter.Success($"{_tableCounter} table(s) and {_recordCounter} record(s) extracted").NewLine();
        }

        private async Task Execute(string ecfTableName, Func<CsvTableReader, EcfTableWriter, Task<int>> action)
        {
            // Report status
            _consoleWriter.StartProgress($"Extracting {ecfTableName}...");
            try
            {
                // Open CSV file stream for import
                using var strReader = new StreamReader(_config.DataFile);

                // Create CSV reader for import
                var csvTableReader = new CsvTableReader(strReader, new CsvConfiguration()
                {
                    Quote = _config.CsvQuote,
                    Separator = _config.CsvSeparator
                }, new CsvConverterResolver());

                // Calculate ECF file name
                var ecfFileName = Path.ChangeExtension(Path.Combine(GetEcfFolderName(), ecfTableName), "csv");

                // Create ECF file stream for export
                using var strWriter = new StreamWriter(ecfFileName, false, Encoding.UTF8);

                // Create ECF writer for export
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
                var schoolClass = new ExportSchoolClass(_config, csvTableReader);

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

        private async Task<int> ExportStudents(CsvTableReader csvTableReader, EcfTableWriter ecfTableWriter)
        {
            var ecfCache = new HashSet<string>();
            var ecfRecordCounter = 0;

            await csvTableReader.ReadHeadersAsync();

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.LastName,
                EcfHeaders.FirstName,
                EcfHeaders.MiddleName,
                EcfHeaders.NickName,
                EcfHeaders.Salutation,
                EcfHeaders.Gender,
                EcfHeaders.Birthdate);

            while (await csvTableReader.ReadAsync() > 0)
            {
                var student = new ExportStudent(_config, csvTableReader);

                if (!ecfCache.Contains(student.Id))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, student.Id);
                    ecfTableWriter.SetValue(EcfHeaders.LastName, student.LastName);
                    ecfTableWriter.SetValue(EcfHeaders.FirstName, student.FirstName);
                    ecfTableWriter.SetValue(EcfHeaders.MiddleName, student.MiddleName);
                    ecfTableWriter.SetValue(EcfHeaders.NickName, student.NickName);
                    ecfTableWriter.SetValue(EcfHeaders.Salutation, student.Salutation);
                    ecfTableWriter.SetValue(EcfHeaders.Gender, student.Gender);
                    ecfTableWriter.SetValue(EcfHeaders.Birthdate, student.BirthDate);

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
                var student = new ExportStudent(_config, csvTableReader);
                var schoolClass = new ExportSchoolClass(_config, csvTableReader);

                if (!string.IsNullOrEmpty(schoolClass.Id))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, IdFactory.CreateIdFromValues(student.Id, schoolClass.Id));
                    ecfTableWriter.SetValue(EcfHeaders.StudentId, student.Id);
                    ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, schoolClass.Id);

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
                EcfHeaders.SubjectId);

            while (await csvTableReader.ReadAsync() > 0)
            {
                var student = new ExportStudent(_config, csvTableReader);
                var schoolClass = new ExportSchoolClass(_config, csvTableReader);

                if (!string.IsNullOrEmpty(schoolClass.Id))
                {
                    for (int i = 1; i < 20; i++)
                    {
                        var subject = new ExportSubject(_config, csvTableReader, $"Fach{i}");

                        if (!string.IsNullOrEmpty(subject.Id))
                        {
                            ecfTableWriter.SetValue(EcfHeaders.Id, IdFactory.CreateIdFromValues(student.Id, schoolClass.Id, subject.Id));
                            ecfTableWriter.SetValue(EcfHeaders.StudentId, student.Id);
                            ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, schoolClass.Id);
                            ecfTableWriter.SetValue(EcfHeaders.SubjectId, subject.Id);

                            await ecfTableWriter.WriteAsync();

                            _consoleWriter.ContinueProgress(++ecfRecordCounter);
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
                for (int i = 1; i < 20; i++)
                {
                    var subject = new ExportSubject(_config, csvTableReader, $"Fach{i}");

                    if (!string.IsNullOrEmpty(subject.Id) && !ecfCache.Contains(subject.Id))
                    {
                        ecfTableWriter.SetValue(EcfHeaders.Id, subject.Id);
                        ecfTableWriter.SetValue(EcfHeaders.Code, subject.Code);

                        await ecfTableWriter.WriteAsync();

                        ecfCache.Add(subject.Id);

                        _consoleWriter.ContinueProgress(++ecfRecordCounter);
                    }
                }
            }

            return ecfRecordCounter;
        }
    }
}
