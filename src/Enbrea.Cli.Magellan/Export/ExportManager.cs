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
using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.Magellan
{
    public class ExportManager : EcfCustomManager
    {
        private readonly Configuration _config;
        private int _tableCounter = 0;
        private int _version = 0;

        public ExportManager(Configuration config, ConsoleWriter consoleWriter, CancellationToken cancellationToken)
            : base(config.TargetFolder, consoleWriter, cancellationToken)
        {
            _config = config;
        }

        public async override Task Execute()
        {
            using var connection = new FbConnection(_config.DatabaseConnection);

            connection.Open();
            try
            {
                _tableCounter = 0;
                _version = await ReadMagellanVersion(connection);

                if (_version < 7)
                {
                    throw new Exception("ENBREA CLI only supports MAGELLAN version 7 and higher.");
                }

                // Report status
                _consoleWriter.Caption("Export from MAGELLAN");

                // Preperation
                PrepareEcfFolder();

                // Catalogs
                await Execute(EcfTables.Countries, connection, async (c, w) => await ExportCatalog("Staaten", c, w));
                await Execute(EcfTables.CourseCategories, connection, async (c, w) => await ExportCatalog("Fachstati", c, w));
                await Execute(EcfTables.CourseTypes, connection, async (c, w) => await ExportCatalog("Unterrichtsarten", c, w));
                await Execute(EcfTables.FormsOfTeaching, connection, async (c, w) => await ExportCatalog("Unterrichtsformen", c, w));
                await Execute(EcfTables.Languages, connection, async (c, w) => await ExportCatalog("Muttersprachen", c, w));
                await Execute(EcfTables.Nationalities, connection, async (c, w) => await ExportCatalog("Staatsangehoerigkeiten", c, w));
                await Execute(EcfTables.Religions, connection, async (c, w) => await ExportCatalog("Konfessionen", c, w));
                await Execute(EcfTables.SchoolCategories, connection, async (c, w) => await ExportCatalog("Schulformen", c, w));
                await Execute(EcfTables.SchoolClassFlags, connection, async (c, w) => await ExportCatalog("KlassenMerkmale", c, w));
                await Execute(EcfTables.SchoolClassLevels, connection, async (c, w) => await ExportCatalog("Klassenstufen", c, w));
                await Execute(EcfTables.SchoolOrganisations, connection, async (c, w) => await ExportCatalog("Organisationen", c, w));
                await Execute(EcfTables.SchoolTypes, connection, async (c, w) => await ExportCatalog("Schularten", c, w));
                await Execute(EcfTables.SubjectLevels, connection, async (c, w) => await ExportCatalog("FachNiveaus", c, w));
                await Execute(EcfTables.SubjectFocuses, connection, async (c, w) => await ExportCatalog("Fachschwerpunkte", c, w));
                await Execute(EcfTables.SubjectGroups, connection, async (c, w) => await ExportCatalog("Fachgruppen", c, w));

                // Special catalogs
                await Execute(EcfTables.EducationalPrograms, connection, async (c, w) => await ExportEducationalPrograms(c, w));
                await Execute(EcfTables.MaritalStatuses, connection, async (c, w) => await ExportMaritalStatuses(w));
                await Execute(EcfTables.SchoolClassTypes, connection, async (c, w) => await ExportSchoolClassTypes(w));
                await Execute(EcfTables.SubjectCategories, connection, async (c, w) => await ExportSubjectCategories(w));
                await Execute(EcfTables.SubjectTypes, connection, async (c, w) => await ExportSubjectTypes(w));

                // Education
                await Execute(EcfTables.Departments, connection, async (c, w) => await ExportDepartments(c, w));
                await Execute(EcfTables.Subjects, connection, async (c, w) => await ExportSubjects(c, w));
                await Execute(EcfTables.Teachers, connection, async (c, w) => await ExportTeachers(c, w));
                await Execute(EcfTables.SchoolClasses, connection, async (c, w) => await ExportSchoolClasses(c, w));
                await Execute(EcfTables.Students, connection, async (c, w) => await ExportStudents(c, w));
                await Execute(EcfTables.StudentSchoolAttendances, connection, async (c, w) => await ExportStudentSchoolAttendances(c, w));
                await Execute(EcfTables.StudentSchoolClassAttendances, connection, async (c, w) => await ExportStudentSchoolClassAttendances(c, w));
                await Execute(EcfTables.StudentSubjects, connection, async (c, w) => await ExportStudentSubjects(c, w));

                _consoleWriter.Success($"{_tableCounter} table(s) extracted").NewLine();
            }
            finally
            {
                connection.Close();
            }
        }

        private async Task Execute(string ecfTableName, FbConnection fbConnection, Func<FbConnection, EcfTableWriter, Task<int>> action)
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
                var ecfRecordCounter = await action(fbConnection, ecfTablefWriter);

                // Inc table counter
                _tableCounter++;

                // Report status
                _consoleWriter.FinishProgress(ecfRecordCounter);
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        private async Task<int> ExportCatalog(string fbTableName, FbConnection fbConnection, EcfTableWriter ecfTableWriter)
        {
            string sql =
                $"""
                select 
                  * 
                from 
                  "{fbTableName}"
                """;

            using var fbTransaction = await fbConnection.BeginTransactionAsync(_cancellationToken);
            using var fbCommand = new FbCommand(sql, fbConnection, fbTransaction);

            using var reader = await fbCommand.ExecuteReaderAsync(_cancellationToken);

            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.StatisticalCode,
                EcfHeaders.InternalCode,
                EcfHeaders.Name);

            while (await reader.ReadAsync(_cancellationToken))
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, reader["Kuerzel"]);
                ecfTableWriter.SetValue(EcfHeaders.Code, reader["Kuerzel"]);
                ecfTableWriter.SetValue(EcfHeaders.StatisticalCode, reader["StatistikID"]);
                ecfTableWriter.SetValue(EcfHeaders.InternalCode, reader["Schluessel"]);
                ecfTableWriter.SetValue(EcfHeaders.Name, reader["Bezeichnung"]);

                await ecfTableWriter.WriteAsync(_cancellationToken);

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportDepartments(FbConnection fbConnection, EcfTableWriter ecfTableWriter)
        {
            string sql =
                """
                select 
                  *
                from 
                  "Abteilungen" 
                where 
                  "Mandant" = @tenantId
                """;

            using var fbTransaction = await fbConnection.BeginTransactionAsync(_cancellationToken);
            using var fbCommand = new FbCommand(sql, fbConnection, fbTransaction);

            fbCommand.Parameters.Add("@tenantId", _config.TenantId);

            using var reader = await fbCommand.ExecuteReaderAsync(_cancellationToken);

            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.Name);

            while (await reader.ReadAsync(_cancellationToken))
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, reader["Kuerzel"]);
                ecfTableWriter.SetValue(EcfHeaders.Code, reader["Kuerzel"]);
                ecfTableWriter.SetValue(EcfHeaders.Name, reader["Bezeichnung"]);

                await ecfTableWriter.WriteAsync(_cancellationToken);

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportEducationalPrograms(FbConnection fbConnection, EcfTableWriter ecfTableWriter)
        {
            string sql = 
                """
                select * from "Bildungsgaenge"
                """;

            using var fbTransaction = await fbConnection.BeginTransactionAsync(_cancellationToken);
            using var fbCommand = new FbCommand(sql, fbConnection, fbTransaction);

            using var reader = await fbCommand.ExecuteReaderAsync(_cancellationToken);

            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.StatisticalCode,
                EcfHeaders.InternalCode,
                EcfHeaders.Name);

            while (await reader.ReadAsync(_cancellationToken))
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, reader["Kuerzel"]);
                ecfTableWriter.SetValue(EcfHeaders.Code, reader["Kuerzel"]);
                ecfTableWriter.SetValue(EcfHeaders.StatisticalCode, reader["StatistikID"]);
                ecfTableWriter.SetValue(EcfHeaders.InternalCode, reader["Schluessel"]);
                ecfTableWriter.SetValue(EcfHeaders.Name, reader["Bezeichnung"]);

                await ecfTableWriter.WriteAsync(_cancellationToken);

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportMaritalStatuses(EcfTableWriter ecfTableWriter)
        {
            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.StatisticalCode,
                EcfHeaders.InternalCode,
                EcfHeaders.Name);

            await ecfTableWriter.WriteAsync("0", "VH", "00", "00", "Verheiratet");
            await ecfTableWriter.WriteAsync("1", "NV", "01", "01", "Nicht Verheiratet");
            await ecfTableWriter.WriteAsync("2", "LE", "02", "02", "Ledig");
            await ecfTableWriter.WriteAsync("3", "GE", "03", "03", "Geschieden");
            await ecfTableWriter.WriteAsync("4", "VW", "04", "04", "Verwitwet");

            _consoleWriter.ContinueProgress(5);

            return await Task.FromResult(5);
        }

        private async Task<int> ExportSchoolClasses(FbConnection fbConnection, EcfTableWriter ecfTableWriter)
        {
            string sql =
                """
                select 
                  K.*, 
                  L1."ID" as "Lehrer1", 
                  L2."ID" as "Lehrer2" 
                from 
                  "KlassenAnsicht" as K
                left join 
                  "Lehrer" as L1 on  K."Mandant" = L1."Mandant" and K."Klassenleiter1" = L1."ID" and L1."Status" = 1
                left join 
                  "Lehrer" as L2 on  K."Mandant" = L2."Mandant" and K."Klassenleiter2" = L2."ID" and L2."Status" = 1
                where 
                  K."Mandant" = @tenantId and K."Zeitraum" = @schoolTermId
                """;

            using var fbTransaction = await fbConnection.BeginTransactionAsync(_cancellationToken);
            using var fbCommand = new FbCommand(sql, fbConnection, fbTransaction);

            fbCommand.Parameters.Add("@tenantId", _config.TenantId);
            fbCommand.Parameters.Add("@schoolTermId", _config.SchoolTermId);

            using var reader = await fbCommand.ExecuteReaderAsync(_cancellationToken);

            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.StatisticalCode,
                EcfHeaders.Name1,
                EcfHeaders.Name2,
                EcfHeaders.SchoolClassTypeId,
                EcfHeaders.SchoolClassLevelId,
                EcfHeaders.DepartmentId,
                EcfHeaders.SchoolTypeId,
                EcfHeaders.SchoolCategoryId,
                EcfHeaders.SchoolOrganisationId,
                EcfHeaders.Teacher1Id,
                EcfHeaders.Teacher2Id,
                EcfHeaders.FormOfTeachingId);

            while (await reader.ReadAsync(_cancellationToken))
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, reader["Id"]);
                ecfTableWriter.SetValue(EcfHeaders.Code, reader["Kuerzel"]);
                ecfTableWriter.SetValue(EcfHeaders.StatisticalCode, reader["KuerzelStatistik"]);
                ecfTableWriter.SetValue(EcfHeaders.Name1, reader["Langname1"]);
                ecfTableWriter.SetValue(EcfHeaders.Name2, reader["Langname2"]);
                ecfTableWriter.SetValue(EcfHeaders.SchoolClassTypeId, reader["Klassenart"]);
                ecfTableWriter.SetValue(EcfHeaders.SchoolClassLevelId, reader["Klassenstufe"]);
                ecfTableWriter.SetValue(EcfHeaders.DepartmentId, reader["Abteilung"]);
                ecfTableWriter.SetValue(EcfHeaders.SchoolTypeId, reader["Schulart"]);
                ecfTableWriter.SetValue(EcfHeaders.SchoolCategoryId, reader["Schulform"]);
                ecfTableWriter.SetValue(EcfHeaders.SchoolOrganisationId, reader["Organisation"]);
                ecfTableWriter.SetValue(EcfHeaders.Teacher1Id, reader["Lehrer1"]);
                ecfTableWriter.SetValue(EcfHeaders.Teacher2Id, reader["Lehrer2"]);
                ecfTableWriter.SetValue(EcfHeaders.FormOfTeachingId, reader["Unterrichtsform"]);

                await ecfTableWriter.WriteAsync(_cancellationToken);

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSchoolClassTypes(EcfTableWriter ecfTableWriter)
        {
            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.StatisticalCode,
                EcfHeaders.InternalCode,
                EcfHeaders.Name);

            await ecfTableWriter.WriteAsync("0", "STANDARD", "00", "00", "Standardklasse");
            await ecfTableWriter.WriteAsync("1", "GANZTAGS", "01", "01", "Ganztagsklasse");
            await ecfTableWriter.WriteAsync("2", "KURSE", "02", "02", "Oberstufenjahrgang (Nur Kurse)");
            await ecfTableWriter.WriteAsync("3", "LK+GK", "03", "03", "Oberstufenjahrgang (LK und GK)");
            await ecfTableWriter.WriteAsync("4", "ABSCHLUSS", "04", "04", "Abschlussklasse");
            await ecfTableWriter.WriteAsync("5", "KOMBI", "05", "05", "Kombinationsklasse");
            await ecfTableWriter.WriteAsync("6", "KINDER", "06", "06", "Schulkindergarten");
            await ecfTableWriter.WriteAsync("7", "STANDARD+O", "07", "07", "Standardklasse mit Oberstufensynchronisation");

            _consoleWriter.ContinueProgress(8);

            return await Task.FromResult(8);
        }

        private async Task<int> ExportStudents(FbConnection fbConnection, EcfTableWriter ecfTableWriter)
        {
            string sql =
                """
                select 
                  S."ID",
                  S."Nachname",
                  S."Vorname",
                  S."Vorname2",
                  S."Geburtsname",
                  S."Anrede",
                  S."Geschlecht",
                  S."Geburtsdatum",
                  S."Geburtsname",
                  S."Geburtsort",
                  S."Geburtsland",
                  S."Strasse",
                  S."PLZ",
                  S."Ort",
                  S."Land",
                  S."Telefon",
                  S."EMail",
                  S."Mobil",
                  S."Staatsangeh1",
                  S."Staatsangeh2",
                  S."Muttersprache",
                  S."Verkehrssprache",
                  S."Konfession",
                  SZ."Tutor"
                from 
                  "Schueler" S
                join 
                  "SchuelerZeitraeume" SZ on S."ID" = SZ."Schueler" and S."Mandant" = SZ."Mandant"
                where 
                  S."Mandant" = @tenantId and SZ."Zeitraum" = @schoolTermId and S."Status" in (2, 3, 4) and (S."IDIntern" is NULL)
                """;

            using var fbTransaction = await fbConnection.BeginTransactionAsync(_cancellationToken);
            using var fbCommand = new FbCommand(sql, fbConnection, fbTransaction);

            fbCommand.Parameters.Add("@tenantId", _config.TenantId);
            fbCommand.Parameters.Add("@schoolTermId", _config.SchoolTermId);

            using var reader = await fbCommand.ExecuteReaderAsync(_cancellationToken);

            var ecfCache = new HashSet<int>();
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.LastName,
                EcfHeaders.FirstName,
                EcfHeaders.MiddleName,
                EcfHeaders.Birthname,
                EcfHeaders.Salutation,
                EcfHeaders.Gender,
                EcfHeaders.Birthdate,
                EcfHeaders.Birthname,
                EcfHeaders.PlaceOfBirth,
                //EcfHeaders.CountryOfBirthId,
                EcfHeaders.AddressLines,
                EcfHeaders.PostalCode,
                EcfHeaders.Locality,
                //EcfHeaders.CountryId,
                EcfHeaders.HomePhoneNumber,
                EcfHeaders.EmailAddress,
                EcfHeaders.MobileNumber,
                //EcfHeaders.Nationality1Id,
                //EcfHeaders.Nationality2Id,
                EcfHeaders.NativeLanguageId,
                EcfHeaders.CorrespondenceLanguageId,
                //EcfHeaders.ReligionId,
                EcfHeaders.TutorId);

            while (await reader.ReadAsync(_cancellationToken))
            {
                var studentId = (int)reader["ID"];

                if (!ecfCache.Contains(studentId))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, studentId);
                    ecfTableWriter.SetValue(EcfHeaders.LastName, reader["Nachname"]);
                    ecfTableWriter.SetValue(EcfHeaders.FirstName, reader["Vorname"]);
                    ecfTableWriter.SetValue(EcfHeaders.MiddleName, reader["Vorname2"]);
                    ecfTableWriter.SetValue(EcfHeaders.Birthname, reader["Geburtsname"]);
                    ecfTableWriter.SetValue(EcfHeaders.Salutation, reader.GetSalutationOrDefault("Anrede"));
                    ecfTableWriter.SetValue(EcfHeaders.Gender, reader.GetGenderOrDefault("Geschlecht"));
                    ecfTableWriter.SetValue(EcfHeaders.Birthdate, reader.GetDateOrDefault("Geburtsdatum"));
                    ecfTableWriter.SetValue(EcfHeaders.Birthname, reader["Geburtsname"]);
                    ecfTableWriter.SetValue(EcfHeaders.PlaceOfBirth, reader["Geburtsort"]);
                    //ecfTableWriter.SetValue(EcfHeaders.CountryOfBirthId, reader["Geburtsland"]);
                    ecfTableWriter.SetValue(EcfHeaders.AddressLines, reader["Strasse"]);
                    ecfTableWriter.SetValue(EcfHeaders.PostalCode, reader["PLZ"]);
                    ecfTableWriter.SetValue(EcfHeaders.Locality, reader["Ort"]);
                    //ecfTableWriter.SetValue(EcfHeaders.CountryId, reader["Land"]);
                    ecfTableWriter.SetValue(EcfHeaders.HomePhoneNumber, reader["Telefon"]);
                    ecfTableWriter.SetValue(EcfHeaders.EmailAddress, reader["EMail"]);
                    ecfTableWriter.SetValue(EcfHeaders.MobileNumber, reader["Mobil"]);
                    //ecfTableWriter.SetValue(EcfHeaders.Nationality1Id, reader["Staatsangeh1"]);
                    //ecfTableWriter.SetValue(EcfHeaders.Nationality2Id, reader["Staatsangeh2"]);
                    ecfTableWriter.SetValue(EcfHeaders.NativeLanguageId, reader["Muttersprache"]);
                    ecfTableWriter.SetValue(EcfHeaders.CorrespondenceLanguageId, reader["Verkehrssprache"]);
                    //ecfTableWriter.SetValue(EcfHeaders.ReligionId, reader["Konfession"]);
                    ecfTableWriter.SetValue(EcfHeaders.TutorId, reader["Tutor"]);

                    await ecfTableWriter.WriteAsync(_cancellationToken);

                    ecfCache.Add(studentId);

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSchoolAttendances(FbConnection fbConnection, EcfTableWriter ecfTableWriter)
        {
            string sql =
                """
                select 
                  S."ID" as "Schueler",
                  S."ZugangAm", 
                  S."Zugang2Am", 
                  S."AbgangAm",
                  S."Abgang2Am"
                from 
                  "Schueler" S
                join 
                  "SchuelerZeitraeume" SZ
                on 
                  S."ID" = SZ."Schueler" and S."Mandant" = SZ."Mandant"
                where 
                  S."Mandant" = @tenantId and SZ."Zeitraum" = @schoolTermId and S."Status" in (2, 3, 4) and (S."IDIntern" is NULL)
                """;

            using var fbTransaction = await fbConnection.BeginTransactionAsync(_cancellationToken);
            using var fbCommand = new FbCommand(sql, fbConnection, fbTransaction);

            fbCommand.Parameters.Add("@tenantId", _config.TenantId);
            fbCommand.Parameters.Add("@schoolTermId", _config.SchoolTermId);

            using var reader = await fbCommand.ExecuteReaderAsync(_cancellationToken);

            var ecfCache = new HashSet<int>();
            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.StudentId,
                EcfHeaders.EntryDate,
                EcfHeaders.ExitDate);

            while (await reader.ReadAsync(_cancellationToken))
            {
                var studentId = (int)reader["Schueler"];

                if (!reader.IsNullOrEmpty("ZugangAm"))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, IdFactory.CreateIdFromValue($"{studentId}+1"));
                    ecfTableWriter.SetValue(EcfHeaders.StudentId, studentId);
                    ecfTableWriter.SetValue(EcfHeaders.EntryDate, reader.GetDateOrDefault("ZugangAm"));
                    ecfTableWriter.SetValue(EcfHeaders.ExitDate, reader.GetDateOrDefault("AbgangAm"));

                    await ecfTableWriter.WriteAsync(_cancellationToken);

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }

                if (!reader.IsNullOrEmpty("Zugang2Am"))
                {
                    ecfTableWriter.SetValue(EcfHeaders.Id, IdFactory.CreateIdFromValue($"{studentId}+2"));
                    ecfTableWriter.SetValue(EcfHeaders.StudentId, studentId);
                    ecfTableWriter.SetValue(EcfHeaders.EntryDate, reader.GetDateOrDefault("Zugang2Am"));
                    ecfTableWriter.SetValue(EcfHeaders.ExitDate, reader.GetDateOrDefault("Abgang2Am"));

                    await ecfTableWriter.WriteAsync(_cancellationToken);

                    _consoleWriter.ContinueProgress(++ecfRecordCounter);
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSchoolClassAttendances(FbConnection fbConnection, EcfTableWriter ecfTableWriter)
        {
            string sql =
                """
                select 
                  Z.ID, 
                  Z."Klasse",
                  S."ID" as "Schueler",
                  K."Zugang" as "KlasseZugangAm", 
                  K."Abgang" as "KlasseAbgangAm", 
                  S."ZugangAm" as "SchuelerZugangAm", 
                  S."AbgangAm" as "SchuelerAbgangAm"
                from 
                  "SchuelerZeitraeume" as Z
                join 
                  "SchuelerKlassen" as K on Z."ID" = K."SchuelerZeitraumID"
                join 
                  "Schueler" as S on Z."Schueler" = S."ID"
                where 
                  Z."Mandant" = @tenantId and Z."Zeitraum" = @schoolTermId and S."Status" in (2, 3, 4) and (S."IDIntern" is NULL)
                
                union all
                
                select 
                  Z.ID, 
                  Z."Klasse",
                  S."IDIntern" as "Schueler",
                  K."Zugang" as "KlasseZugangAm", 
                  K."Abgang" as "KlasseAbgangAm", 
                  S."ZugangAm" as "SchuelerZugangAm", 
                  S."AbgangAm" as "SchuelerAbgangAm"
                from 
                  "SchuelerZeitraeume" as Z
                join 
                  "SchuelerKlassen" as K on Z."ID" = K."SchuelerZeitraumID"
                join 
                  "Schueler" as S on Z."Schueler" = S."ID"
                where 
                  Z."Mandant" = @tenantId and Z."Zeitraum" = @schoolTermId and S."Status" in (2, 3, 4) and not (S."IDIntern" is NULL)
                """;

            using var fbTransaction = await fbConnection.BeginTransactionAsync(_cancellationToken);
            using var fbCommand = new FbCommand(sql, fbConnection, fbTransaction);

            fbCommand.Parameters.Add("@tenantId", _config.TenantId);
            fbCommand.Parameters.Add("@schoolTermId", _config.SchoolTermId);

            using var reader = await fbCommand.ExecuteReaderAsync(_cancellationToken);

            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.SchoolClassId,
                EcfHeaders.StudentId,
                EcfHeaders.EntryDate,
                EcfHeaders.ExitDate);

            while (await reader.ReadAsync(_cancellationToken))
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, reader["ID"]);
                ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, reader["Klasse"]);
                ecfTableWriter.SetValue(EcfHeaders.StudentId, reader["Schueler"]);
                ecfTableWriter.SetValue(EcfHeaders.EntryDate, reader.GetYoungestDateOrDefault("KlasseZugangAm", "SchuelerZugangAm"));
                ecfTableWriter.SetValue(EcfHeaders.ExitDate, reader.GetOldestDateOrDefault("KlasseAbgangAm", "SchuelerAbgangAm"));

                await ecfTableWriter.WriteAsync(_cancellationToken);

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSubjects(FbConnection fbConnection, EcfTableWriter ecfTableWriter)
        {
            string sql =
                """
                select 
                  F."ID", 
                  F."Klasse", 
                  S."ID" as "Schueler", 
                  K."Zugang" as "KlasseZugangAm", 
                  K."Abgang" as "KlasseAbgangAm", 
                  S."ZugangAm" as "SchuelerZugangAm", 
                  S."AbgangAm" as "SchuelerAbgangAm",
                  F."KursNr", 
                  F."Unterrichtsart", 
                  F."Fachstatus", 
                  F."Fach",
                  F."Niveau", 
                  F."Schwerpunkt", 
                  L."ID" as "Lehrer" 
                from 
                  "SchuelerFachdaten" as F
                join 
                  "SchuelerZeitraeume" as Z on Z."ID" = F."SchuelerZeitraumID"
                join 
                  "SchuelerKlassen" as K on Z."ID" = K."SchuelerZeitraumID"
                join 
                  "Schueler" as S on Z."Schueler" = S."ID"
                left join 
                  "Lehrer" as L on F."Lehrer" = L."ID"
                where 
                  Z."Mandant" = @tenantId and Z."Zeitraum" = @schoolTermId and S."Status" in (2, 3, 4) and (S."IDIntern" is NULL)
                
                union all
                
                select 
                  F."ID", 
                  F."Klasse", 
                  S."IDIntern" as "Schueler", 
                  K."Zugang" as "KlasseZugangAm", 
                  K."Abgang" as "KlasseAbgangAm", 
                  S."ZugangAm" as "SchuelerZugangAm", 
                  S."AbgangAm" as "SchuelerAbgangAm",
                  F."KursNr", 
                  F."Unterrichtsart", 
                  F."Fachstatus", 
                  F."Fach",
                  F."Niveau", 
                  F."Schwerpunkt", 
                  L."ID" as "Lehrer" 
                from 
                  "SchuelerFachdaten" as F
                join 
                  "SchuelerZeitraeume" as Z on Z."ID" = F."SchuelerZeitraumID"
                join 
                  "SchuelerKlassen" as K on Z."ID" = K."SchuelerZeitraumID"
                join 
                  "Schueler" as S on Z."Schueler" = S."ID"
                left join 
                  "Lehrer" as L on F."Lehrer" = L."ID" and L."Status" = 1
                where 
                  Z."Mandant" = @tenantId and Z."Zeitraum" = @schoolTermId and S."Status" in (2, 3, 4) and not (S."IDIntern" is NULL)
                
                union all

                select 
                  F."ID", 
                  F."Klasse", 
                  S."ID" as "Schueler", 
                  K."Zugang" as "KlasseZugangAm", 
                  K."Abgang" as "KlasseAbgangAm", 
                  S."ZugangAm" as "SchuelerZugangAm", 
                  S."AbgangAm" as "SchuelerAbgangAm",
                  F."KursNr", 
                  F."Unterrichtsart", 
                  F."Fachstatus", 
                  F."Fach",
                  F."Niveau", 
                  F."Schwerpunkt", 
                  L."ID" as "Lehrer" 
                from 
                  "SchuelerFachdaten" as F
                join 
                  "SchuelerZeitraeume" as Z on Z."ID" = F."SchuelerZeitraumID"
                join 
                  "SchuelerKlassen" as K on Z."ID" = K."SchuelerZeitraumID"
                join 
                  "Schueler" as S on Z."Schueler" = S."ID"
                left join 
                  "Lehrer" as L on F."Lehrer" = L."ID"
                where 
                  Z."Mandant" = @tenantId and Z."Zeitraum" = @schoolTermId and S."Status" in (2, 3, 4) and (S."IDIntern" is NULL)
                
                union all
                
                select 
                  F."ID", 
                  F."Klasse", 
                  S."IDIntern" as "Schueler", 
                  K."Zugang" as "KlasseZugangAm", 
                  K."Abgang" as "KlasseAbgangAm", 
                  S."ZugangAm" as "SchuelerZugangAm", 
                  S."AbgangAm" as "SchuelerAbgangAm",
                  F."KursNr", 
                  F."Unterrichtsart", 
                  F."Fachstatus", 
                  F."Fach",
                  F."Niveau", 
                  F."Schwerpunkt", 
                  L."ID" as "Lehrer" 
                from 
                  "SchuelerFachdaten" as F
                join 
                  "SchuelerZeitraeume" as Z on Z."ID" = F."SchuelerZeitraumID"
                join 
                  "SchuelerKlassen" as K on Z."ID" = K."SchuelerZeitraumID"
                join 
                  "Schueler" as S on Z."Schueler" = S."ID"
                left join 
                 "Lehrer" as L on F."Lehrer" = L."ID" and L."Status" = 1
                where 
                  Z."Mandant" = @tenantId and Z."Zeitraum" = @schoolTermId and S."Status" in (2, 3, 4) and not (S."IDIntern" is NULL)
                """;

            using var fbTransaction = await fbConnection.BeginTransactionAsync(_cancellationToken);
            using var fbCommand = new FbCommand(sql, fbConnection, fbTransaction);

            fbCommand.Parameters.Add("@tenantId", _config.TenantId);
            fbCommand.Parameters.Add("@schoolTermId", _config.SchoolTermId);

            using var reader = await fbCommand.ExecuteReaderAsync(_cancellationToken);

            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.SchoolClassId,
                EcfHeaders.StudentId,
                EcfHeaders.CourseNo,
                EcfHeaders.CourseTypeId,
                EcfHeaders.CourseCategoryId,
                EcfHeaders.SubjectId,
                EcfHeaders.SubjectLevelId,
                EcfHeaders.SubjectFocusId,
                EcfHeaders.TeacherId,
                EcfHeaders.EntryDate,
                EcfHeaders.ExitDate);

            while (await reader.ReadAsync(_cancellationToken))
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, reader["ID"]);
                ecfTableWriter.SetValue(EcfHeaders.SchoolClassId, reader["Klasse"]);
                ecfTableWriter.SetValue(EcfHeaders.StudentId, reader["Schueler"]);
                ecfTableWriter.SetValue(EcfHeaders.CourseNo, reader.GetShortOrDefault("KursNr", 0));
                ecfTableWriter.SetValue(EcfHeaders.CourseTypeId, reader["Unterrichtsart"]);
                ecfTableWriter.SetValue(EcfHeaders.CourseCategoryId, reader["Fachstatus"]);
                ecfTableWriter.SetValue(EcfHeaders.SubjectId, reader["Fach"]);
                ecfTableWriter.SetValue(EcfHeaders.SubjectLevelId, reader["Niveau"]);
                ecfTableWriter.SetValue(EcfHeaders.SubjectFocusId, reader["Schwerpunkt"]);
                ecfTableWriter.SetValue(EcfHeaders.TeacherId, reader["Lehrer"]);
                ecfTableWriter.SetValue(EcfHeaders.EntryDate, reader.GetYoungestDateOrDefault("KlasseZugangAm", "SchuelerZugangAm"));
                ecfTableWriter.SetValue(EcfHeaders.ExitDate, reader.GetOldestDateOrDefault("KlasseAbgangAm", "SchuelerAbgangAm"));

                await ecfTableWriter.WriteAsync(_cancellationToken);

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSubjectCategories(EcfTableWriter ecfTableWriter)
        {
            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.StatisticalCode,
                EcfHeaders.InternalCode,
                EcfHeaders.Name);

            await ecfTableWriter.WriteAsync("0", "SLK", "00", "00", "sprachl.-lit.-künstlerisch");
            await ecfTableWriter.WriteAsync("1", "GES", "01", "01", "gesellschaftswiss.");
            await ecfTableWriter.WriteAsync("2", "MNT", "02", "02", "mathem.-nat.-technisch");
            await ecfTableWriter.WriteAsync("3", "REL", "03", "03", "Religion");
            await ecfTableWriter.WriteAsync("4", "SP", "04", "04", "Sport");

            _consoleWriter.ContinueProgress(5);

            return await Task.FromResult(5);
        }

        private async Task<int> ExportSubjects(FbConnection fbConnection, EcfTableWriter ecfTableWriter)
        {
            string sql =
                """
                select * from "Faecher" where "Mandant" = @tenantId
                """;

            using var fbTransaction = await fbConnection.BeginTransactionAsync(_cancellationToken);
            using var fbCommand = new FbCommand(sql, fbConnection, fbTransaction);

            fbCommand.Parameters.Add("@tenantId", _config.TenantId);

            using var reader = await fbCommand.ExecuteReaderAsync(_cancellationToken);

            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.StatisticalCode,
                EcfHeaders.InternalCode,
                EcfHeaders.Name,
                EcfHeaders.SubjectTypeId,
                EcfHeaders.SubjectCategoryId,
                EcfHeaders.SubjectGroupId);

            while (await reader.ReadAsync(_cancellationToken))
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, reader["ID"]);
                ecfTableWriter.SetValue(EcfHeaders.Code, reader["Kuerzel"]);
                ecfTableWriter.SetValue(EcfHeaders.StatisticalCode, reader["StatistikID"]);
                ecfTableWriter.SetValue(EcfHeaders.InternalCode, reader["Schluessel"]);
                ecfTableWriter.SetValue(EcfHeaders.Name, reader["Bezeichnung"]);
                ecfTableWriter.SetValue(EcfHeaders.SubjectTypeId, reader["Kategorie"]);
                ecfTableWriter.SetValue(EcfHeaders.SubjectCategoryId, reader["Aufgabenbereich"]);
                ecfTableWriter.SetValue(EcfHeaders.SubjectGroupId, reader["Gruppe"]);

                await ecfTableWriter.WriteAsync(_cancellationToken);

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSubjectTypes(EcfTableWriter ecfTableWriter)
        {
            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.StatisticalCode,
                EcfHeaders.InternalCode,
                EcfHeaders.Name);

            await ecfTableWriter.WriteAsync("0", "FS", "00", "00", "Fremdsprache");
            await ecfTableWriter.WriteAsync("1", "REL", "01", "01", "Religion / Ethik");
            await ecfTableWriter.WriteAsync("2", "DEU", "02", "02", "Deutsch");
            await ecfTableWriter.WriteAsync("3", "MAT", "03", "03", "Mathematik");
            await ecfTableWriter.WriteAsync("4", "KUN", "04", "04", "Kunst");
            await ecfTableWriter.WriteAsync("5", "MUS", "05", "05", "Musik");
            await ecfTableWriter.WriteAsync("6", "SP", "06", "06", "Sport");
            await ecfTableWriter.WriteAsync("9", "INF", "09", "09", "Informatik");
            await ecfTableWriter.WriteAsync("10", "PHI", "10", "10", "Philosophie");
            await ecfTableWriter.WriteAsync("11", "GES", "11", "11", "Geschichte");
            await ecfTableWriter.WriteAsync("12", "PHY", "12", "12", "Physik");
            await ecfTableWriter.WriteAsync("13", "CHE", "13", "13", "Chemie");
            await ecfTableWriter.WriteAsync("14", "BIO", "14", "14", "Biologie");
            await ecfTableWriter.WriteAsync("15", "ERD", "15", "15", "Erdkunde");
            await ecfTableWriter.WriteAsync("16", "SOZ", "16", "16", "Sozialkunde");
            await ecfTableWriter.WriteAsync("17", "WIR", "17", "17", "Wirtschaft");
            await ecfTableWriter.WriteAsync("18", "POL", "18", "18", "Politik");
            await ecfTableWriter.WriteAsync("19", "DSP", "19", "19", "Darstellendes Spiel");
            await ecfTableWriter.WriteAsync("20", "EREL", "20", "20", "Evangelische Religion");
            await ecfTableWriter.WriteAsync("21", "KREL", "21", "21", "Katholische Religion");
            await ecfTableWriter.WriteAsync("26", "TECH", "26", "26", "Technik");
            await ecfTableWriter.WriteAsync("27", "PÄD", "27", "27", "Pädagogik");
            await ecfTableWriter.WriteAsync("28", "SPT", "28", "28", "Sport - Theorie");
            await ecfTableWriter.WriteAsync("29", "BWL/RW", "29", "29", "BWL / RW");
            await ecfTableWriter.WriteAsync("30", "BWL/VWL", "30", "30", "BWL / VWL");
            await ecfTableWriter.WriteAsync("31", "VWL", "31", "31", "VWL");
            await ecfTableWriter.WriteAsync("32", "SEM", "32", "32", "Seminar");
            await ecfTableWriter.WriteAsync("33", "GSU", "33", "33", "Gesundheit");
            await ecfTableWriter.WriteAsync("34", "PSY", "34", "34", "Psychologie");
            await ecfTableWriter.WriteAsync("35", "RECH", "35", "35", "Recht");

            _consoleWriter.ContinueProgress(30);

            return await Task.FromResult(30);
        }

        private async Task<int> ExportTeachers(FbConnection fbConnection, EcfTableWriter ecfTableWriter)
        {
            string sql =
                """
                select * from "Lehrer" where "Mandant" = @tenantId and "Status" = 1
                """;

            using var fbTransaction = await fbConnection.BeginTransactionAsync(_cancellationToken);
            using var fbCommand = new FbCommand(sql, fbConnection, fbTransaction);

            fbCommand.Parameters.Add("@tenantId", _config.TenantId);

            using var reader = await fbCommand.ExecuteReaderAsync(_cancellationToken);

            var ecfRecordCounter = 0;

            await ecfTableWriter.WriteHeadersAsync(
                EcfHeaders.Id,
                EcfHeaders.Code,
                EcfHeaders.LastName,
                EcfHeaders.FirstName,
                EcfHeaders.MiddleName,
                EcfHeaders.Salutation,
                EcfHeaders.Gender,
                EcfHeaders.Birthdate,
                EcfHeaders.Birthname,
                EcfHeaders.PlaceOfBirth,
                EcfHeaders.MaritalStatusId,
                EcfHeaders.AddressLines,
                EcfHeaders.PostalCode,
                EcfHeaders.Locality,
                EcfHeaders.CountryId,
                EcfHeaders.HomePhoneNumber,
                EcfHeaders.OfficePhoneNumber,
                EcfHeaders.EmailAddress,
                EcfHeaders.MobileNumber,
                EcfHeaders.Nationality1Id,
                EcfHeaders.Nationality2Id,
                EcfHeaders.NativeLanguageId,
                EcfHeaders.CorrespondenceLanguageId,
                EcfHeaders.ReligionId); ;

            while (await reader.ReadAsync(_cancellationToken))
            {
                ecfTableWriter.SetValue(EcfHeaders.Id, reader["ID"]);
                ecfTableWriter.SetValue(EcfHeaders.Code, reader["Kuerzel"]);
                ecfTableWriter.SetValue(EcfHeaders.LastName, reader["Nachname"]);
                ecfTableWriter.SetValue(EcfHeaders.FirstName, reader["Vorname"]);
                ecfTableWriter.SetValue(EcfHeaders.MiddleName, reader["Vorname2"]);
                ecfTableWriter.SetValue(EcfHeaders.Salutation, reader.GetSalutationOrDefault("Anrede"));
                ecfTableWriter.SetValue(EcfHeaders.Gender, reader.GetGenderOrDefault("Geschlecht"));
                ecfTableWriter.SetValue(EcfHeaders.Birthdate, reader.GetDateOrDefault("Geburtsdatum"));
                ecfTableWriter.SetValue(EcfHeaders.Birthname, reader["Geburtsname"]);
                ecfTableWriter.SetValue(EcfHeaders.PlaceOfBirth, reader["Geburtsort"]);
                ecfTableWriter.SetValue(EcfHeaders.MaritalStatusId, reader["Ehestand"]);
                ecfTableWriter.SetValue(EcfHeaders.AddressLines, reader["Strasse"]);
                ecfTableWriter.SetValue(EcfHeaders.PostalCode, reader["PLZ"]);
                ecfTableWriter.SetValue(EcfHeaders.Locality, reader["Ort"]);
                ecfTableWriter.SetValue(EcfHeaders.CountryId, reader["Land"]);
                ecfTableWriter.SetValue(EcfHeaders.HomePhoneNumber, reader["Telefon"]);
                ecfTableWriter.SetValue(EcfHeaders.OfficePhoneNumber, reader["TelefonDienst"]);
                ecfTableWriter.SetValue(EcfHeaders.EmailAddress, reader["Email"]);
                ecfTableWriter.SetValue(EcfHeaders.MobileNumber, reader["Mobil"]);
                ecfTableWriter.SetValue(EcfHeaders.Nationality1Id, reader["Staatsangeh"]);
                ecfTableWriter.SetValue(EcfHeaders.Nationality2Id, reader["Staatsangeh2"]);
                ecfTableWriter.SetValue(EcfHeaders.NativeLanguageId, reader["Muttersprache"]);
                ecfTableWriter.SetValue(EcfHeaders.CorrespondenceLanguageId, reader["Verkehrssprache"]);
                ecfTableWriter.SetValue(EcfHeaders.ReligionId, reader["Konfession"]);

                await ecfTableWriter.WriteAsync(_cancellationToken);

                _consoleWriter.ContinueProgress(++ecfRecordCounter);
            }

            return ecfRecordCounter;
        }

        private async Task<int> ReadMagellanVersion(FbConnection fbConnection)
        {
            var sql =
                """
                select first 1 
                  "Release"
                from 
                  "Version";
                """;
            
            using var fbTransaction = await fbConnection.BeginTransactionAsync(_cancellationToken);
            using var fbCommand = new FbCommand(sql, fbConnection, fbTransaction);

            using var reader = await fbCommand.ExecuteReaderAsync(_cancellationToken);

            int result;

            if (await reader.ReadAsync(_cancellationToken))
            {
                var version = reader.GetInt32(reader.GetOrdinal("Release"));

                if (version >= 800)
                    result = 8;
                else if (version >= 700)
                    result = 7;
                else
                    result = 6;
            }
            else
            {
                result = 6;
            }

            await fbTransaction.CommitAsync(_cancellationToken);

            return result;
        }    
    }
}
