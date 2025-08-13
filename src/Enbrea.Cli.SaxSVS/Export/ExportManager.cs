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
using Enbrea.Konsoli;
using Enbrea.SaxSVS;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.SaxSVS
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
            using var strReader = File.OpenText(_config.DataFile);

            var saxSVSReader = new SaxSVSReader(strReader);
            var saxSVSDocument = await saxSVSReader.ReadDocumentAsync();

            // Init counters
            _tableCounter = 0;
            _recordCounter = 0;

            // Report status
            _consoleWriter.Caption("Export from SaxSVS");

            // Preperation
            PrepareEcfFolder();

            // Education
            await Execute(EcfTables.Teachers, saxSVSDocument, ExportTeachers);
            await Execute(EcfTables.SchoolClasses, saxSVSDocument, ExportSchoolClasses);
            await Execute(EcfTables.Students, saxSVSDocument, ExportStudents);
            await Execute(EcfTables.StudentSchoolClassAttendances, saxSVSDocument, ExportStudentSchoolClassAttendances);
            await Execute(EcfTables.StudentSubjects, saxSVSDocument, ExportStudentSubjects);

            // Report status
            _consoleWriter.Success($"{_tableCounter} table(s) and {_recordCounter} record(s) extracted").NewLine();
        }

        private async Task Execute(string ecfTableName, SaxSVSDocument saxSVSDocument, Func<SaxSVSDocument, EcfTableWriter, Task<int>> action)
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
                var ecfRecordCounter = await action(saxSVSDocument, ecfTableWriter);

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

        private async Task<int> ExportSchoolClasses(SaxSVSDocument saxSVSDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.Teacher1Id,
                EcfHeaders.Teacher2Id);

            foreach (var schoolClass in saxSVSDocument.Classes)
            {
                if (schoolClass.AcademicYear == saxSVSDocument.AcademicYear)
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, schoolClass.Id);
                    ecfTableWriter.SetValue(EcfHeaders.Code, schoolClass.Name);
                    ecfTableWriter.SetValue(EcfHeaders.Teacher1Id, schoolClass.FormTeacherId);
                    ecfTableWriter.SetValue(EcfHeaders.Teacher2Id, schoolClass.DeputyFormTeacherId);

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudents(SaxSVSDocument saxSVSDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.LastName,
                EcfHeaders.FirstName,
                EcfHeaders.Gender,
                EcfHeaders.Birthdate);

            foreach (var student in saxSVSDocument.Students)
            {
                if (saxSVSDocument.DoesStudentAttendAcademicYear(student.Id, saxSVSDocument.AcademicYear))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, student.Id.ToString());
                    ecfTableWriter.SetValue(EcfHeaders.LastName, student.FamilyName);
                    ecfTableWriter.SetValue(EcfHeaders.FirstName, student.GivenName);
                    ecfTableWriter.TrySetValue(EcfHeaders.Gender, student.Gender?.Code);
                    ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, student.BirthDate);

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSchoolClassAttendances(SaxSVSDocument saxSVSDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.StudentId,
                EcfHeaders.SchoolClassId);

            foreach (var schoolClass in saxSVSDocument.Classes)
            {
                if (schoolClass.AcademicYear == saxSVSDocument.AcademicYear)
                {
                    foreach (var studentAttendance in schoolClass.Students)
                    {
                        ecfTableWriter.SetValue(EcfHeaders.Id, IdFactory.CreateIdFromValues(studentAttendance.StudentId.ToString(), schoolClass.Id.ToString()));
                        ecfTableWriter.SetValue(EcfHeaders.StudentId, studentAttendance.StudentId);
                        ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, schoolClass.Id);

                        await ecfTableWriter.WriteAsync();

                        _consoleWriter.ContinueProgress(++ecfRecordCounter);
                    }
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSubjects(SaxSVSDocument saxSVSDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.SchoolClassId,
                EcfHeaders.TeacherId,
                EcfHeaders.StudentId,
                EcfHeaders.SubjectId);

            foreach (var lesson in saxSVSDocument.Lessons)
            {
                if (lesson.AcademicYear == saxSVSDocument.AcademicYear)
                {
                    if (lesson.Students.Count > 0)
                    {
                        foreach (var classRelation in lesson.Classes)
                        {
                            foreach (var studentAttendance in lesson.Students)
                            {
                                if (saxSVSDocument.DoesStudentAttendClass(classRelation.ClassId, studentAttendance.StudentId))
                                {
                                    ecfTableWriter.SetValue(EcfHeaders.Id, lesson.Id);
                                    ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, classRelation.ClassId);
                                    ecfTableWriter.SetValue(EcfHeaders.TeacherId, lesson.TeacherId);
                                    ecfTableWriter.SetValue(EcfHeaders.StudentId, studentAttendance.StudentId);
                                    ecfTableWriter.SetValue(EcfHeaders.SubjectId, lesson.Subject?.Name);

                                    await ecfTableWriter.WriteAsync(_cancellationToken);

                                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var classRelation in lesson.Classes)
                        {
                            foreach (var student in saxSVSDocument.Students)
                            {
                                if (saxSVSDocument.DoesStudentAttendClass(classRelation.ClassId, student.Id))
                                {
                                    ecfTableWriter.SetValue(EcfHeaders.Id, lesson.Id);
                                    ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, classRelation.ClassId);
                                    ecfTableWriter.SetValue(EcfHeaders.TeacherId, lesson.TeacherId);
                                    ecfTableWriter.SetValue(EcfHeaders.StudentId, student.Id);
                                    ecfTableWriter.SetValue(EcfHeaders.SubjectId, lesson.Subject?.Name);

                                    await ecfTableWriter.WriteAsync(_cancellationToken);

                                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                                }
                            }
                        }
                    }
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportTeachers(SaxSVSDocument saxSVSDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.LastName,
                EcfHeaders.FirstName,
                EcfHeaders.Gender,
                EcfHeaders.Birthdate);

            foreach (var workforce in saxSVSDocument.Workforces)
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, workforce.Id);
                ecfTableWriter.SetValue(EcfHeaders.Code, workforce.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.LastName, workforce.FamilyName);
                ecfTableWriter.TrySetValue(EcfHeaders.FirstName, workforce.GivenName);
                ecfTableWriter.TrySetValue(EcfHeaders.Gender, workforce.Gender?.Code);
                ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, workforce.BirthDate);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }
    }
}
