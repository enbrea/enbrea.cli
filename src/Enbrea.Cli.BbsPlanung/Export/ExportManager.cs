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

using Enbrea.BbsPlanung.Db;
using Enbrea.Cli.Common;
using Enbrea.Konsoli;
using Enbrea.Ecf;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.BbsPlanung
{
    public class ExportManager : EcfCustomManager
    {
        private int _recordCounter = 0;
        private int _tableCounter = 0;
        private readonly Configuration _config;

        public ExportManager(Configuration config, ConsoleWriter consoleWriter, CancellationToken cancellationToken)
            : base(config.TargetFolder, consoleWriter, cancellationToken)
        {
            _config = config;
        }

        public async override Task Execute()
        {
            await using var bbsPlanungDbReader = new BbsPlanungDbReader(_config.DatabaseConnection);

            // Init counters
            _tableCounter = 0;
            _recordCounter = 0;

            // Report status
            _consoleWriter.Caption("Export from BBS-Planung");

            // Preperation
            PrepareEcfFolder();

            // Connect reader
            await bbsPlanungDbReader.ConnectAsync();

            // Education
            await Execute(EcfTables.Teachers, bbsPlanungDbReader, async (r, w) => await ExportTeachers(r, w));
            await Execute(EcfTables.SchoolClasses, bbsPlanungDbReader, async (r, w) => await ExportSchoolClasses(r, w));
            await Execute(EcfTables.Students, bbsPlanungDbReader, async (r, w) => await ExportStudents(r, w));
            await Execute(EcfTables.StudentSchoolClassAttendances, bbsPlanungDbReader, async (r, w) => await ExportStudentSchoolClassAttendances(r, w));
            await Execute(EcfTables.StudentSubjects, bbsPlanungDbReader, async (r, w) => await ExportStudentSubjects(r, w));

            // Disconnect reader
            await bbsPlanungDbReader.DisconnectAsync();

            // Report status
            _consoleWriter.Success($"{_tableCounter} table(s) and {_recordCounter} record(s) extracted").NewLine();
        }

        private async Task Execute(string ecfTableName, BbsPlanungDbReader bbsPlanungDbReader, Func<BbsPlanungDbReader, EcfTableWriter, Task<int>> action)
        {
            // Report status
            _consoleWriter.StartProgress($"Extracting {ecfTableName}...");
            try
            {
                // Init CSV file stream
                using var ecfStreamWriter = new StreamWriter(Path.ChangeExtension(Path.Combine(GetEcfFolderName(), ecfTableName), "csv"));

                // Init ECF Writer
                var ecfTablefWriter = new EcfTableWriter(ecfStreamWriter);

                // Call table specific action
                var ecfRecordCounter = await action(bbsPlanungDbReader, ecfTablefWriter);

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

        private async Task<int> ExportSchoolClasses(BbsPlanungDbReader bbsPlanungDbReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.Notes);

            await foreach (var schoolClass in bbsPlanungDbReader.SchoolClassesAsync(_config.SchoolNo))
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, schoolClass.Code);
                ecfTableWriter.TrySetValue(EcfHeaders.Code, schoolClass.Code);
                ecfTableWriter.TrySetValue(EcfHeaders.Teacher1Id, schoolClass.Teacher);
                ecfTableWriter.TrySetValue(EcfHeaders.Notes, schoolClass.Notes);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudents(BbsPlanungDbReader bbsPlanungDbReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.LastName,
                EcfHeaders.FirstName,
                EcfHeaders.Gender,
                EcfHeaders.Birthdate,
                EcfHeaders.StudentNo);

            await foreach (var student in bbsPlanungDbReader.StudentsAsync(_config.SchoolNo))
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, student.Id.ToString());
                ecfTableWriter.TrySetValue(EcfHeaders.LastName, student.Lastname);
                ecfTableWriter.TrySetValue(EcfHeaders.FirstName, student.Firstname);
                ecfTableWriter.TrySetValue(EcfHeaders.Gender, student.GetGenderOrDefault());
                ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, student.GetBirthdateOrDefault());
                ecfTableWriter.TrySetValue(EcfHeaders.StudentNo, student.StudentNo);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSchoolClassAttendances(BbsPlanungDbReader bbsPlanungDbReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.StudentId,
                EcfHeaders.SchoolClassId);

            await foreach (var student in bbsPlanungDbReader.StudentsAsync(_config.SchoolNo))
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, student.Id.ToString() + "_" + student.SchoolClass);
                ecfTableWriter.TrySetValue(EcfHeaders.StudentId, student.Id.ToString());
                ecfTableWriter.TrySetValue(EcfHeaders.SchoolClassId, student.SchoolClass);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSubjects(BbsPlanungDbReader bbsPlanungDbReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.StudentId,
                EcfHeaders.SchoolClassId);

            await foreach (var student in bbsPlanungDbReader.StudentsAsync(_config.SchoolNo))
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, student.Id.ToString() + "_" + student.SchoolClass);
                ecfTableWriter.TrySetValue(EcfHeaders.StudentId, student.Id.ToString());
                ecfTableWriter.TrySetValue(EcfHeaders.SchoolClassId, student.SchoolClass);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportTeachers(BbsPlanungDbReader bbsPlanungDbReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.LastName,
                EcfHeaders.FirstName,
                EcfHeaders.Gender,
                EcfHeaders.Birthdate);

            await foreach (var teacher in bbsPlanungDbReader.TeachersAsync(_config.SchoolNo))
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, teacher.Id);
                ecfTableWriter.TrySetValue(EcfHeaders.Code, teacher.Code);
                ecfTableWriter.TrySetValue(EcfHeaders.LastName, teacher.Lastname);
                ecfTableWriter.TrySetValue(EcfHeaders.FirstName, teacher.Firstname);
                ecfTableWriter.TrySetValue(EcfHeaders.Gender, teacher.GetGenderOrDefault());
                ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, teacher.GetBirthdateOrDefault());

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }
    }
}
