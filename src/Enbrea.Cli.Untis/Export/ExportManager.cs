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
using Enbrea.Untis.Gpu;
using Enbrea.Untis.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.Untis
{
    public class ExportManager : EcfCustomManager
    {
        private readonly Configuration _config;
        private readonly HashSet<uint> _untisAbsencesCache = [];
        private int _recordCounter = 0;
        private int _tableCounter = 0;
        private UntisDocument _untisDocument;

        public ExportManager(Configuration config, ConsoleWriter consoleWriter, CancellationToken cancellationToken)
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
            _consoleWriter.Caption("Export from Untis");

            // Export XML and GPU files from Untis
            await ExportFromUntis();

            // Preperation
            PrepareEcfFolder();

            // Load Untis XML export file
            _untisDocument = UntisDocument.Load(Path.Combine(_config.ExportFolder, "untis.xml"));

            // Create manifest with global data
            await CreateManifest(_untisDocument);

            // Extract Untis data
            await Execute(EcfTables.Departments, _untisDocument, ExportDepartments);
            await Execute(EcfTables.Rooms, _untisDocument, ExportRooms);
            await Execute(EcfTables.Subjects, _untisDocument, ExportSubjects);
            await Execute(EcfTables.SchoolClasses, _untisDocument, ExportSchoolClasses);
            await Execute(EcfTables.Teachers, _untisDocument, ExportTeachers);
            await Execute(EcfTables.TeacherCourseAttendances, _untisDocument, ExportTeacherCourseAttendances);
            await Execute(EcfTables.Students, _untisDocument, ExportStudents);
            await Execute(EcfTables.StudentSchoolClassAttendances, _untisDocument, ExportStudentSchoolClassAttendances);
            await Execute(EcfTables.TeacherAbsenceReasons, "GPU012.txt", ExportAbsenceReasons);
            await Execute(EcfTables.TeacherAbsences, "GPU013.txt", ExportTeacherAbsences);
            await Execute(EcfTables.SchoolClassAbsenceReasons, "GPU012.txt", ExportAbsenceReasons);
            await Execute(EcfTables.SchoolClassAbsences, "GPU013.txt", ExportSchoolClassAbsences);
            await Execute(EcfTables.RoomAbsenceReasons, "GPU012.txt", ExportAbsenceReasons);
            await Execute(EcfTables.RoomAbsences, "GPU013.txt", ExportRoomAbsences);
            await Execute(EcfTables.Timeframes, _untisDocument, ExportTimeframes);
            await Execute(EcfTables.Holidays, _untisDocument, ExportHolidays);
            await Execute(EcfTables.Courses, _untisDocument, ExportCourses);
            await Execute(EcfTables.ScheduledLessons, _untisDocument, ExportScheduledLessons);
            await Execute(EcfTables.LessonGaps, "GPU014.txt", ExportLessonGaps);
            await Execute(EcfTables.SubstituteLessons, "GPU014.txt", ExportSubstituteLessons);

            // Report status
            _consoleWriter.Success($"{_tableCounter} table(s) and {_recordCounter} record(s) extracted").NewLine();
        }

        private async Task CreateManifest(UntisDocument untisDocument)
        {
            // Report status
            _consoleWriter.StartProgress($"Extracting {EcfTables.Manifest}...");
            try
            {
                var manifest = new EcfManifest();

                manifest.ValidFrom = DateOnlyUtils.ToDateTimeOffset(untisDocument.GeneralSettings.TermBeginDate);
                manifest.ValidTo = DateOnlyUtils.ToDateTimeOffset(untisDocument.GeneralSettings.TermEndDate.AddDays(1));

                await EcfManifestManager.SaveToFileAsync(GetEcfManifestFileName(), manifest, _cancellationToken);

                // Report status
                _consoleWriter.FinishProgress();
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        private async Task Execute(string ecfTableName, UntisDocument untisDocument, Func<UntisDocument, EcfTableWriter, Task<int>> action)
        {
            // Report status
            _consoleWriter.StartProgress($"Extracting {ecfTableName}...");
            try
            {
                // Calculate ECF file name
                var ecfFileName = Path.ChangeExtension(Path.Combine(GetEcfFolderName(), ecfTableName), "csv");

                // Create ECF file stream for export
                using var strWriter = new StreamWriter(ecfFileName, false, Encoding.UTF8);

                // Create ECF writer for export
                var ecfTableWriter = new EcfTableWriter(strWriter);

                // Call table specific action
                var ecfRecordCounter = await action(untisDocument, ecfTableWriter);

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

        private async Task Execute(string ecfTableName, string gpuFileName, Func<CsvReader, EcfTableWriter, Task<int>> action)
        {
            // Looking for table mapping info
            // Report status
            _consoleWriter.StartProgress($"Extracting {ecfTableName}...");
            try
            {
                // Open GPU file stream for import
                using var strReader = new StreamReader(Path.Combine(_config.ExportFolder, gpuFileName),
                    _config.ExportFilesAsUtf8 ? Encoding.UTF8 : Encoding.GetEncoding(28591/*Western European (ISO)*/));

                // Create CSV reader for import
                var csvReader = new CsvReader(strReader, new CsvConfiguration()
                {
                    Quote = _config.ExportQuote,
                    Separator = _config.ExportSeparator
                });

                // Calculate ECF file name
                var ecfFileName = Path.ChangeExtension(Path.Combine(GetEcfFolderName(), ecfTableName), "csv");

                // Create ECF file stream for export
                using var strWriter = new StreamWriter(ecfFileName, false, Encoding.UTF8);

                // Create ECF writer for export
                var ecfTableWriter = new EcfTableWriter(strWriter);

                // Call table specific action
                var ecfRecordCounter = await action(csvReader, ecfTableWriter);

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

        private async Task<int> ExportAbsenceReasons(CsvReader csvReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.Name,
                EcfHeaders.StatisticalCode);

            var gpuReader = new GpuReader<GpuAbsenceReason>(csvReader);

            await foreach (var reason in gpuReader.ReadAsync())
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, reason.ShortName);
                ecfTableWriter.SetValue(EcfHeaders.Code, reason.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.Name, reason.LongName);
                ecfTableWriter.TrySetValue(EcfHeaders.StatisticalCode, reason.StatisticalCode);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportCourses(UntisDocument untisDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Title,
                EcfHeaders.BlockNo,
                EcfHeaders.Description,
                EcfHeaders.SubjectId,
                EcfHeaders.SchoolClassIdList,
                EcfHeaders.ValidFrom,
                EcfHeaders.ValidTo);

            foreach (var lesson in untisDocument.Lessons)
            {
                if (!string.IsNullOrEmpty(lesson.SubjectId))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, lesson.Id);
                    ecfTableWriter.SetValue(EcfHeaders.Title, lesson.GetEcfCourseTitle(_untisDocument.Subjects));
                    ecfTableWriter.SetValue(EcfHeaders.BlockNo, lesson.Block);
                    ecfTableWriter.SetValue(EcfHeaders.Description, lesson.Text);
                    ecfTableWriter.SetValue(EcfHeaders.SubjectId, lesson.SubjectId);
                    ecfTableWriter.SetValue(EcfHeaders.SchoolClassIdList, lesson.ClassIds);
                    ecfTableWriter.SetValue(EcfHeaders.ValidFrom, lesson.ValidFrom);
                    ecfTableWriter.SetValue(EcfHeaders.ValidTo, lesson.ValidTo);

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportDepartments(UntisDocument untisDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.Name);

            foreach (var department in untisDocument.Departments)
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, department.Id);
                ecfTableWriter.SetValue(EcfHeaders.Code, department.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.Name, department.LongName);

                await ecfTableWriter.WriteAsync();

                ecfRecordCounter++;
            }

            return ecfRecordCounter;
        }

        private async Task ExportFromUntis()
        {
            if (_config.DataProvider != DataProvider.ManualExport)
            {
                // Create export folder
                Directory.CreateDirectory(_config.ExportFolder);

                // Backup file name
                var backupFile = Path.Combine(_config.ExportFolder, "untis.backup");

                // Backup Untis data
                if (_config.DataProvider == DataProvider.Server)
                {
                    await ConsoleUtils.RunUntisBackup(
                        _consoleWriter,
                        _config.ServerSchoolNo,
                        _config.ServerSchoolYear,
                        1,
                        _config.ServerUserName,
                        _config.ServerPassword,
                        backupFile,
                        _cancellationToken);
                }
                else
                {
                    ConsoleUtils.RunUntisBackup(
                        _consoleWriter,
                        _config.DataFile,
                        backupFile);
                }

                // Export XML file from Untis
                await ConsoleUtils.RunUntisXmlExport(_consoleWriter, backupFile,
                    _config.ExportFolder,
                    _cancellationToken);

                // Export GPU files from Untis
                await ConsoleUtils.RunUntisGpuExport(_consoleWriter, backupFile,
                    ["012", "013", "014"],
                    _config.ExportFolder,
                    _cancellationToken);
            }
        }

        private async Task<int> ExportHolidays(UntisDocument untisDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Title,
                EcfHeaders.Description,
                EcfHeaders.TemporalExpressions);

            foreach (var holiday in untisDocument.Holidays)
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, holiday.Id);
                ecfTableWriter.SetValue(EcfHeaders.Title, holiday.ShortName);
                ecfTableWriter.SetValue(EcfHeaders.Description, holiday.LongName);
                ecfTableWriter.SetValue(EcfHeaders.TemporalExpressions, holiday.GetEcfTemporalExpressions());

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportLessonGaps(CsvReader csvReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.LessonId,
                EcfHeaders.Reasons,
                EcfHeaders.Resolutions,
                EcfHeaders.Description,
                EcfHeaders.TemporalExpressions);

            var gpuReader = new GpuReader<GpuSubstitution>(csvReader);

            await foreach (var substitution in gpuReader.ReadAsync())
            {
                if (substitution.Date >= _untisDocument.GeneralSettings.TermBeginDate &&
                    substitution.Date <= _untisDocument.GeneralSettings.TermEndDate &&
                    substitution.GetEcfLessonId(_untisDocument.Lessons) != null)
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, substitution.GetEcfLessonGapId());
                    ecfTableWriter.SetValue(EcfHeaders.LessonId, substitution.GetEcfLessonId(_untisDocument.Lessons));
                    ecfTableWriter.SetValue(EcfHeaders.Reasons, substitution.GetEcfReasons(_untisAbsencesCache));
                    ecfTableWriter.SetValue(EcfHeaders.Resolutions, substitution.GetEcfResolutions());
                    ecfTableWriter.SetValue(EcfHeaders.Description, substitution.Remark);
                    ecfTableWriter.SetValue(EcfHeaders.TemporalExpressions, substitution.GetEcfTemporalExpressions(_untisDocument.TimeGrids, _untisDocument.Lessons));

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportRoomAbsences(CsvReader csvReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.RoomId,
                EcfHeaders.StartTimepoint,
                EcfHeaders.EndTimepoint,
                EcfHeaders.ReasonId,
                EcfHeaders.Description);

            var gpuReader = new GpuReader<GpuAbsence>(csvReader);

            await foreach (var absence in gpuReader.ReadAsync())
            {
                if (absence.Type == GpuAbsenceType.Room)
                {
                    if (absence.IsInsideTerm(_untisDocument.GeneralSettings) && absence.GetEcfRoomId(_untisDocument.Rooms) != null)
                    {
                        ecfTableWriter.SetValue(EcfHeaders.Id, absence.Id);
                        ecfTableWriter.SetValue(EcfHeaders.RoomId, absence.GetUntisRoomId());
                        ecfTableWriter.SetValue(EcfHeaders.StartTimepoint, absence.StartDate);
                        ecfTableWriter.SetValue(EcfHeaders.EndTimepoint, absence.EndDate);
                        ecfTableWriter.SetValue(EcfHeaders.ReasonId, absence.Reason);
                        ecfTableWriter.SetValue(EcfHeaders.Description, absence.Text);

                        await ecfTableWriter.WriteAsync();

                        _untisAbsencesCache.Add(absence.Id);

                        _consoleWriter.ContinueProgress(++ecfRecordCounter);
                    }
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportRooms(UntisDocument untisDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.Name,
                EcfHeaders.Description,
                EcfHeaders.DepartmentId,
                EcfHeaders.Capacity,
                EcfHeaders.Color);

            foreach (var room in untisDocument.Rooms)
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, room.Id);
                ecfTableWriter.SetValue(EcfHeaders.Code, room.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.Name, room.LongName);
                ecfTableWriter.TrySetValue(EcfHeaders.Description, room.GetEcfDescription(untisDocument.Descriptions));
                ecfTableWriter.TrySetValue(EcfHeaders.DepartmentId, room.DepartmentId);
                ecfTableWriter.TrySetValue(EcfHeaders.Capacity, room.Capacity);
                ecfTableWriter.TrySetValue(EcfHeaders.Color, room.BackgroundColor);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportScheduledLessons(UntisDocument untisDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.CourseId,
                EcfHeaders.SubjectId,
                EcfHeaders.SchoolClassIdList,
                EcfHeaders.TeacherIdList,
                EcfHeaders.RoomIdList,
                EcfHeaders.TemporalExpressions);

            foreach (var lesson in untisDocument.Lessons)
            {
                if (!string.IsNullOrEmpty(lesson.SubjectId))
                {
                    foreach (var lessonTime in lesson.Times.FindAll(x => x.SlotGroupFirstSlot == null))
                    {
                        ecfTableWriter.SetValue(EcfHeaders.Id, lessonTime.GetEcfId(lesson));
                        ecfTableWriter.SetValue(EcfHeaders.CourseId, lesson.Id);
                        ecfTableWriter.SetValue(EcfHeaders.SubjectId, lesson.SubjectId);
                        ecfTableWriter.SetValue(EcfHeaders.SchoolClassIdList, lesson.ClassIds);
                        ecfTableWriter.SetValue(EcfHeaders.TeacherIdList, lesson.GetEcfTeacherIdList());
                        ecfTableWriter.SetValue(EcfHeaders.RoomIdList, lessonTime.RoomIds);
                        ecfTableWriter.SetValue(EcfHeaders.TemporalExpressions, lesson.GetEcfTemporalExpressions(lessonTime, untisDocument.GeneralSettings));

                        await ecfTableWriter.WriteAsync();

                        _consoleWriter.ContinueProgress(++ecfRecordCounter);
                    }
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSchoolClassAbsences(CsvReader csvReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.SchoolClassId,
                EcfHeaders.StartTimepoint,
                EcfHeaders.EndTimepoint,
                EcfHeaders.ReasonId,
                EcfHeaders.Description);

            var gpuReader = new GpuReader<GpuAbsence>(csvReader);

            await foreach (var absence in gpuReader.ReadAsync())
            {
                if (absence.Type == GpuAbsenceType.Class)
                {
                    if (absence.IsInsideTerm(_untisDocument.GeneralSettings) && absence.GetEcfSchoolClassId(_untisDocument.Classes) != null)
                    {
                        ecfTableWriter.SetValue(EcfHeaders.Id, absence.Id);
                        ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, absence.GetUntisSchoolClassId());
                        ecfTableWriter.SetValue(EcfHeaders.StartTimepoint, absence.StartDate);
                        ecfTableWriter.SetValue(EcfHeaders.EndTimepoint, absence.EndDate);
                        ecfTableWriter.SetValue(EcfHeaders.ReasonId, absence.Reason);
                        ecfTableWriter.SetValue(EcfHeaders.Description, absence.Text);

                        await ecfTableWriter.WriteAsync();

                        _untisAbsencesCache.Add(absence.Id);

                        _consoleWriter.ContinueProgress(++ecfRecordCounter);
                    }
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSchoolClasses(UntisDocument untisDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.Name1,
                EcfHeaders.Description,
                EcfHeaders.DepartmentId,
                EcfHeaders.Color,
                EcfHeaders.ValidFrom,
                EcfHeaders.ValidTo);

            foreach (var schoolClass in untisDocument.Classes)
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, schoolClass.Id);
                ecfTableWriter.SetValue(EcfHeaders.Code, schoolClass.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.Name1, schoolClass.LongName);
                ecfTableWriter.TrySetValue(EcfHeaders.Description, schoolClass.GetEcfDescription(untisDocument.Descriptions));
                ecfTableWriter.TrySetValue(EcfHeaders.DepartmentId, schoolClass.DepartmentId);
                ecfTableWriter.TrySetValue(EcfHeaders.Color, schoolClass.BackgroundColor);
                ecfTableWriter.TrySetValue(EcfHeaders.ValidFrom, schoolClass.ValidFrom);
                ecfTableWriter.TrySetValue(EcfHeaders.ValidTo, schoolClass.ValidTo);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudents(UntisDocument untisDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.LastName,
                EcfHeaders.FirstName,
                EcfHeaders.Gender,
                EcfHeaders.Birthdate,
                EcfHeaders.EmailAddress);

            foreach (var student in untisDocument.Students)
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, student.Id);
                ecfTableWriter.SetValue(EcfHeaders.LastName, student.LastName);
                ecfTableWriter.SetValue(EcfHeaders.FirstName, student.FirstName);
                ecfTableWriter.TrySetValue(EcfHeaders.Gender, student.GetEcfGender());
                ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, student.Birthdate);
                ecfTableWriter.TrySetValue(EcfHeaders.EmailAddress, student.Email);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSchoolClassAttendances(UntisDocument untisDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.StudentId,
                EcfHeaders.SchoolClassId);

            foreach (var student in untisDocument.Students)
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, IdFactory.CreateIdFromValues(student.Id, student.ClassId));
                ecfTableWriter.SetValue(EcfHeaders.StudentId, student.Id);
                ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, student.ClassId);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSubjects(UntisDocument untisDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.Name,
                EcfHeaders.Description,
                EcfHeaders.Color);

            foreach (var subject in untisDocument.Subjects)
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, subject.Id);
                ecfTableWriter.SetValue(EcfHeaders.Code, subject.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.Name, subject.LongName);
                ecfTableWriter.TrySetValue(EcfHeaders.Description, subject.GetEcfDescription(untisDocument.Descriptions));
                ecfTableWriter.TrySetValue(EcfHeaders.Color, subject.BackgroundColor);

                await ecfTableWriter.WriteAsync();

                ecfRecordCounter++;
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSubstituteLessons(CsvReader csvReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.CourseId,
                EcfHeaders.RoomIdList,
                EcfHeaders.SchoolClassIdList,
                EcfHeaders.TeacherIdList,
                EcfHeaders.TemporalExpressions);

            var gpuReader = new GpuReader<GpuSubstitution>(csvReader);

            await foreach (var substitution in gpuReader.ReadAsync())
            {
                if (substitution.Date >= _untisDocument.GeneralSettings.TermBeginDate &&
                    substitution.Date <= _untisDocument.GeneralSettings.TermEndDate &&
                    substitution.Type != GpuSubstitutionType.Cancellation &&
                    substitution.Type != GpuSubstitutionType.Exemption &&
                    substitution.GetEcfCourseId(_untisDocument.Lessons) != null)
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, substitution.GetEcfId());
                    ecfTableWriter.SetValue(EcfHeaders.CourseId, substitution.GetEcfCourseId(_untisDocument.Lessons));
                    ecfTableWriter.SetValue(EcfHeaders.RoomIdList, substitution.GetEcfRoomIdList());
                    ecfTableWriter.SetValue(EcfHeaders.SchoolClassIdList, substitution.GetEcfSchoolClassIdList());
                    ecfTableWriter.SetValue(EcfHeaders.TeacherIdList, substitution.GetEcfTeacherIdList());
                    ecfTableWriter.SetValue(EcfHeaders.TemporalExpressions, substitution.GetEcfTemporalExpressions(_untisDocument.TimeGrids, _untisDocument.Lessons));

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportTeacherAbsences(CsvReader csvReader, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.TeacherId,
                EcfHeaders.StartTimepoint,
                EcfHeaders.EndTimepoint,
                EcfHeaders.ReasonId,
                EcfHeaders.Description);

            var gpuReader = new GpuReader<GpuAbsence>(csvReader);

            await foreach (var absence in gpuReader.ReadAsync())
            {
                if (absence.Type == GpuAbsenceType.Teacher)
                {
                    if (absence.IsInsideTerm(_untisDocument.GeneralSettings) && absence.GetEcfTeacherId(_untisDocument.Teachers) != null)
                    {
                        ecfTableWriter.SetValue(EcfHeaders.Id, absence.Id);
                        ecfTableWriter.SetValue(EcfHeaders.TeacherId, absence.GetUntisTeacherId());
                        ecfTableWriter.SetValue(EcfHeaders.StartTimepoint, absence.StartDate);
                        ecfTableWriter.SetValue(EcfHeaders.EndTimepoint, absence.EndDate);
                        ecfTableWriter.SetValue(EcfHeaders.ReasonId, absence.Reason);
                        ecfTableWriter.SetValue(EcfHeaders.Description, absence.Text);

                        await ecfTableWriter.WriteAsync();

                        _untisAbsencesCache.Add(absence.Id);

                        _consoleWriter.ContinueProgress(++ecfRecordCounter);
                    }
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportTeacherCourseAttendances(UntisDocument untisDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.CourseId,
                EcfHeaders.TeacherId);

            foreach (var lesson in untisDocument.Lessons)
            {
                if (!string.IsNullOrEmpty(lesson.SubjectId) && !string.IsNullOrEmpty(lesson.TeacherId))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, IdFactory.CreateIdFromValues(lesson.Id, lesson.TeacherId));
                    ecfTableWriter.SetValue(EcfHeaders.CourseId, lesson.Id);
                    ecfTableWriter.SetValue(EcfHeaders.TeacherId, lesson.TeacherId);

                    await ecfTableWriter.WriteAsync();

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }
        private async Task<int> ExportTeachers(UntisDocument untisDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.LastName,
                EcfHeaders.FirstName,
                EcfHeaders.Gender,
                EcfHeaders.EmailAddress,
                EcfHeaders.Color);

            foreach (var teacher in untisDocument.Teachers)
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, teacher.Id);
                ecfTableWriter.SetValue(EcfHeaders.Code, teacher.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.LastName, teacher.LastName);
                ecfTableWriter.TrySetValue(EcfHeaders.FirstName, teacher.FirstName);
                ecfTableWriter.TrySetValue(EcfHeaders.Gender, teacher.GetEcfGender());
                ecfTableWriter.TrySetValue(EcfHeaders.EmailAddress, teacher.Email);
                ecfTableWriter.TrySetValue(EcfHeaders.Color, teacher.BackgroundColor);

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportTimeframes(UntisDocument untisDocument, EcfTableWriter ecfTableWriter)
        {
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.Name,
                EcfHeaders.TimeSlots);

            foreach (var timeGrid in untisDocument.TimeGrids)
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, timeGrid.GetEcfCode());
                ecfTableWriter.SetValue(EcfHeaders.Code, timeGrid.GetEcfCode());
                ecfTableWriter.SetValue(EcfHeaders.Name, timeGrid.GetEcfCode());
                ecfTableWriter.SetValue(EcfHeaders.TimeSlots, timeGrid.GetEcfTimeSlots());

                await ecfTableWriter.WriteAsync();

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }
    }
}
