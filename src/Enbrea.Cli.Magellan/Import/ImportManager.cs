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
using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.Magellan
{
    public class ImportManager : EcfCustomManager
    {
        private readonly Dictionary<string, string> _applicationAssessments = new();

        private readonly Dictionary<string, string> _applicationEnrollmentSupportMap = new();

        private readonly Dictionary<string, string> _applicationStudentMap = new();

        private readonly List<ApplicationTarget> _applicationTargets = new();

        private readonly List<ApplicationTargetTrack> _applicationTargetTracks = new();

        private readonly Configuration _config;

        private readonly Dictionary<string, string> _custodianRelationshipTypeMap = new();

        private readonly Dictionary<int, int> _studentForeignLanguageCount = new();

        private int _tableCounter = 0;

        private int _version = 0;

        public ImportManager(Configuration config, ConsoleWriter consoleWriter, CancellationToken cancellationToken)
            : base(config.SourceFolder, consoleWriter, cancellationToken)
        {
            _config = config;
        }

        public override async Task Execute()
        {
            using var connection = new FbConnection(_config.DatabaseConnection);

            connection.Open();
            try
            {
                using FbTransaction transaction = await connection.BeginTransactionAsync();
                try
                {
                    _tableCounter = 0;
                    _version = await ReadMagellanVersion(connection, transaction);

                    if (_version < 7)
                    {
                        throw new Exception("Enbrea Cli only supports MAGELLAN version 7 and higher.");
                    }

                    // Report status
                    _consoleWriter.Caption("Import to MAGELLAN");

                    // processes every found import file in given dependency order
                    await Execute(connection, transaction, EcfTables.Applications, async (c, t, ic) => await ImportApplicationStudentMapping(ic));
                    await Execute(connection, transaction, EcfTables.CustodianRelationshipTypes, async (c, t, ic) => await ImportCustodianRelationshipTypeMapping(ic));

                    // SchoolTerms
                    await Execute(connection, transaction, EcfTables.SchoolTerms, async (c, t, ic) => await ImportSchoolTerm(c, t, ic));

                    // Basic Catalogs
                    await Execute(connection, transaction, EcfTables.CourseTypes, async (c, t, ic) => await ImportBasicCatalog("Unterrichtsarten", c, t, ic));
                    await Execute(connection, transaction, EcfTables.EnrollmentTargets, async (c, t, ic) => await ImportBasicCatalog("Einschulmerkmale", c, t, ic));
                    await Execute(connection, transaction, EcfTables.Languages, async (c, t, ic) => await ImportBasicCatalog("Muttersprachen", c, t, ic));
                    await Execute(connection, transaction, EcfTables.Nationalities, async (c, t, ic) => await ImportBasicCatalog("Staatsangehoerigkeiten", c, t, ic));
                    await Execute(connection, transaction, EcfTables.Countries, async (c, t, ic) => await ImportBasicCatalog("Staatsangehoerigkeiten", c, t, ic));
                    await Execute(connection, transaction, EcfTables.Religions, async (c, t, ic) => await ImportBasicCatalog("Konfessionen", c, t, ic));
                    await Execute(connection, transaction, EcfTables.ReligionParticipations, async (c, t, ic) => await ImportBasicCatalog("RelTeilnahmen", c, t, ic));

                    // Special Catalogs
                    await Execute(connection, transaction, EcfTables.AchievementTypes, async (c, t, ic) => await ImportAchievementType(c, t, ic));
                    await Execute(connection, transaction, EcfTables.ForeignLanguages, async (c, t, ic) => await ImportForeignLanguage(c, t, ic));
                    await Execute(connection, transaction, EcfTables.Subjects, async (c, t, ic) => await ImportSubject(c, t, ic));
                    await Execute(connection, transaction, EcfTables.GradeValues, async (c, t, ic) => await ImportGradeValue(c, t, ic));

                    // School
                    await Execute(connection, transaction, EcfTables.Workforces, async (c, t, ic) => await ImporWorkforce(c, t, ic));
                    await Execute(connection, transaction, EcfTables.Teachers, async (c, t, ic) => await ImportTeacher(c, t, ic));
                    await Execute(connection, transaction, EcfTables.Custodians, async (c, t, ic) => await ImportCustodian(c, t, ic));
                    await Execute(connection, transaction, EcfTables.SchoolClasses, async (c, t, ic) => await ImportSchoolClass(c, t, ic));
                    await Execute(connection, transaction, EcfTables.Students, async (c, t, ic) => await ImportStudent(c, t, ic));
                    await Execute(connection, transaction, EcfTables.StudentForeignLanguages, async (c, t, ic) => await ImportStudentForeignLanguage(c, t, ic));
                    await Execute(connection, transaction, EcfTables.StudentCustodians, async (c, t, ic) => await ImportStudentCustodian(c, t, ic));
                    await Execute(connection, transaction, EcfTables.StudentRemarks, async (c, t, ic) => await ImportStudentRemark(c, t, ic));
                    await Execute(connection, transaction, EcfTables.StudentSchoolClassAttendances, async (c, t, ic) => await ImportStudentSchoolClassAttendance(c, t, ic));

                    // Applications
                    await Execute(connection, transaction, EcfTables.ApplicationAssessments, async (c, t, ic) => await ImportApplicationAssessments(ic));
                    await Execute(connection, transaction, EcfTables.ApplicationEnrollmentSupports, async (c, t, ic) => await ImportApplicationEnrollmentSupports(ic));
                    await Execute(connection, transaction, EcfTables.ApplicationLevels, async (c, t, ic) => await ImportBasicCatalog("Einschulmerkmale", c, t, ic));
                    await Execute(connection, transaction, EcfTables.ApplicationTargetTracks, async (c, t, ic) => await ImportApplicationTargetTracks(ic));
                    await Execute(connection, transaction, EcfTables.ApplicationTargets, async (c, t, ic) => await ImportApplicationTargets(c, t, ic));

                    await Execute(connection, transaction, EcfTables.Applications, async (c, t, ic) => await ImportApplicationLevelIds(c, t, ic));
                    await Execute(connection, transaction, EcfTables.Applications, async (c, t, ic) => await ImportApplicationAssesments(c, t, ic));
                    await Execute(connection, transaction, EcfTables.ApplicationTargetSelections, async (c, t, ic) => await ImportApplicationTargetSelections(c, t, ic));
                    await Execute(connection, transaction, EcfTables.ApplicationEnrollmentSupportSelections, async (c, t, ic) => await ImportApplicationEnrollmentSupportSelections(c, t, ic));

                    await Execute(connection, transaction, EcfTables.StudentSubjects, async (c, t, ic) => await ImportStudentSubject(c, t, ic));

                    await transaction.CommitAsync();

                    _consoleWriter.Success($"{_tableCounter} table(s) extracted").NewLine();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            finally
            {
                connection.Close();
            }
        }

        private async Task Execute(FbConnection fbConnection, FbTransaction fbTransaction, string ecfTableName, Func<FbConnection, FbTransaction, ImportContext, Task> action)
        {
            var sourceFile = Path.ChangeExtension(Path.Combine(GetEcfFolderName(), ecfTableName), "csv");

            if (File.Exists(sourceFile))
            {
                // Report status
                _consoleWriter.StartProgress($"Importing {ecfTableName}...");
                try
                {
                    // Init local counter
                    var ecfRecordCounter = 0;

                    // Init CSV file stream
                    using var ecfStreamReader = File.OpenText(sourceFile);

                    // Init ECF Table Reader
                    var ecfTableReader = new EcfTableReader(ecfStreamReader);

                    // Read ECF header line
                    await ecfTableReader.ReadHeadersAsync();

                    // Iterate through the ECF records
                    while (ecfTableReader.ReadAsync().Result > 0)
                    {
                        await action(fbConnection, fbTransaction, new ImportContext(fbConnection, fbTransaction, ecfTableReader));
                        ecfRecordCounter++;
                    }

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
        }

        private async Task ImportAchievementType(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupOrCreateEntityByCodeColumn("Leistungsarten", 20, async (insertOrUpdate, code) =>
            {
                var sqlBuilder = new SqlBuilder("Leistungsarten");

                sqlBuilder.SetValue("Kuerzel", code);
                sqlBuilder.SetValue("Art", 0);

                importContext.LookupValue<string>(EcfHeaders.Name, (value) => sqlBuilder.SetValue("Kuerzel", value));
                importContext.LookupValue<string>(EcfHeaders.StatisticalCode, value => sqlBuilder.SetValue("Schluessel", value));

                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Kuerzel\" = @code"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);
                fbCommand.Parameters.Add("@code", code);

                await fbCommand.ExecuteNonQueryAsync();

                return code;
            }); ;
        }

        private async Task ImportApplicationAssesments(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupEntityByIdColumn(EcfHeaders.StudentId, "Schueler", _config.TenantId, async (id) =>
            {
                bool mustUpdate = false; string assessmentByTeacherId = null, assessmentByStudentId = null;

                importContext.LookupValue<string>(EcfHeaders.AssessmentByTeacherId, (value) => assessmentByTeacherId = value);
                importContext.LookupValue<string>(EcfHeaders.AssessmentByStudentId, (value) => assessmentByStudentId = value);

                var sqlBuilder = new SqlBuilder("BewerberVerfahren");

                if (_applicationAssessments.TryGetValue(assessmentByTeacherId, out var assessmentByTeacherCode))
                {
                    sqlBuilder.SetValue("EinschaetzungLehrer", ValueConverter.ApplicationAssessment(assessmentByTeacherCode));
                    mustUpdate = true;
                }
                if (_applicationAssessments.TryGetValue(assessmentByStudentId, out var assessmentByStudentCode))
                {
                    sqlBuilder.SetValue("EinschaetzungBewerber", ValueConverter.ApplicationAssessment(assessmentByStudentCode));
                    mustUpdate = true;
                }

                if (mustUpdate)
                {
                    var sql = sqlBuilder.AsUpdate("\"Mandant\" = @tenantId and \"Schueler\" = @studentId");

                    using var fbCommand = new FbCommand(sql, fbConnection, fbTransaction);

                    fbCommand.AddParameters(sqlBuilder.Assignments);
                    fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                    fbCommand.Parameters.Add("@studentId", id);

                    await fbCommand.ExecuteNonQueryAsync();
                }
            }); ;
        }

        private async Task ImportApplicationAssessments(ImportContext importContext)
        {
            string id = null, code = null;

            importContext.LookupValue<string>(EcfHeaders.Id, (value) => id = value);
            importContext.LookupValue<string>(EcfHeaders.Code, (value) => code = value);

            _applicationAssessments.Add(id, code);

            await Task.CompletedTask;
        }

        private async Task ImportApplicationEnrollmentSupports(ImportContext importContext)
        {
            string id = null, code = null;

            importContext.LookupValue<string>(EcfHeaders.Id, (value) => id = value);
            importContext.LookupValue<string>(EcfHeaders.Code, (value) => code = value);

            _applicationEnrollmentSupportMap.Add(id, code);

            await Task.CompletedTask;
        }

        private async Task ImportApplicationEnrollmentSupportSelections(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupValue<string>(EcfHeaders.ApplicationId, async (applicationId) =>
            {
                if (_applicationStudentMap.TryGetValue(applicationId, out var studentId))
                {
                    await importContext.LookupEntityById("Schueler", studentId, _config.TenantId, async (studentId) =>
                    {
                        await importContext.LookupOrCreateEntity("BewerberVerfahren",
                            "\"Mandant\" = @tenantId and \"Schueler\" = @studentId",
                            fbCommand =>
                        {
                            fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                            fbCommand.Parameters.Add("@studentId", studentId);
                        },
                            async (insertOrUpdate) =>
                        {
                            var enrollmentSupportCode = string.Empty;
                            var enrollmentSupportWeeklyUnit = default(int?);

                            importContext.LookupValue<string>("EnrollmentSupportId", (value) => enrollmentSupportCode = _applicationEnrollmentSupportMap.GetValueOrDefault(value));
                            importContext.LookupValue<int>("WeeklyUnits", (value) => enrollmentSupportWeeklyUnit = value);

                            var sqlBuilder = new SqlBuilder("BewerberVerfahren");

                            sqlBuilder.SetValue("Mandant", _config.TenantId);
                            sqlBuilder.SetValue("Schueler", studentId);

                            if (enrollmentSupportCode == "BüDeu")
                            {
                                sqlBuilder.SetValue("BrueckenKurseDE", Convert.ToUInt16(enrollmentSupportWeeklyUnit));
                            }
                            else if (enrollmentSupportCode == "BüMat")
                            {
                                sqlBuilder.SetValue("BrueckenKurseMA", Convert.ToUInt16(enrollmentSupportWeeklyUnit));
                            }
                            else if (enrollmentSupportCode == "BüEng")
                            {
                                sqlBuilder.SetValue("BrueckenKurseEN", Convert.ToUInt16(enrollmentSupportWeeklyUnit));
                            }
                            else
                            {
                                return;
                            }

                            using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"Schueler\" = @studentId"), fbConnection, fbTransaction);

                            fbCommand.AddParameters(sqlBuilder.Assignments);
                            fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                            fbCommand.Parameters.Add("@studentId", studentId);

                            await fbCommand.ExecuteNonQueryAsync();
                        });
                    });
                };
            });
        }

        private async Task ImportApplicationLevelIds(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupEntityByIdColumn(EcfHeaders.StudentId, "Schueler", _config.TenantId, async (id) =>
            {
                var sqlBuilder = new SqlBuilder("Schueler");

                await importContext.TryLookupEntityByCode(EcfHeaders.LevelId, "Einschulmerkmale", (value) => sqlBuilder.SetValue("Einschulmerkmal3", value));

                using var fbCommand = new FbCommand(sqlBuilder.AsUpdate("\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);

                fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                fbCommand.Parameters.Add("@id", id);

                await fbCommand.ExecuteNonQueryAsync();
            }); ;
        }

        private async Task ImportApplicationStudentMapping(ImportContext importContext)
        {
            string applicationId = null, studentId = null;

            importContext.LookupValue<string>(EcfHeaders.Id, (value) => applicationId = value);
            importContext.LookupValue<string>(EcfHeaders.StudentId, (value) => studentId = value);

            _applicationStudentMap.TryAdd(applicationId, studentId);

            await Task.CompletedTask;
        }

        private async Task ImportApplicationTargets(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupOrCreateEntityByCodeColumn("Einschulmerkmale", 20, async (insertOrUpdate, code) =>
            {
                string id = null;

                importContext.LookupValue<string>(EcfHeaders.Id, (value) => id = value);

                if (code == null) importContext.LookupValue<string>(EcfHeaders.Code, (value) => code = value);

                var sqlBuilder = new SqlBuilder("Einschulmerkmale");

                sqlBuilder.SetValue("Kuerzel", code);

                importContext.LookupValue<string>(EcfHeaders.Name, (value) => sqlBuilder.SetValue("Bezeichnung", value));
                importContext.LookupValue<string>(EcfHeaders.TrackId, (value) => _applicationTargets.Add(new ApplicationTarget() { Id = id, Code = code, Track = _applicationTargetTracks.Where(x => x.Id == value).SingleOrDefault() }));

                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Kuerzel\" = @code"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);
                fbCommand.Parameters.Add("@code", code);

                await fbCommand.ExecuteNonQueryAsync();

                return code;
            });
        }

        private async Task ImportApplicationTargetSelections(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupValue<string>(EcfHeaders.ApplicationId, async (applicationId) =>
            {
                if (_applicationStudentMap.TryGetValue(applicationId, out var studentId))
                {
                    await importContext.LookupEntityById("Schueler", studentId, _config.TenantId, async (id) =>
                    {
                        var targetId = string.Empty;
                        var targetOrder = default(int?);

                        importContext.LookupValue<string>(EcfHeaders.TargetId, (value) => targetId = value);
                        importContext.LookupValue<int?>(EcfHeaders.Order, (value) => targetOrder = value);

                        var sqlBuilder = new SqlBuilder("Schueler");

                        var target = _applicationTargets.Where(x => x.Id == targetId).SingleOrDefault();
                        if (target != null)
                        {
                            if ((targetOrder != null) && (targetOrder == 0) && (target.Track.Order == null || target.Track.Order == 1))
                            {
                                sqlBuilder.SetValue("Einschulmerkmal", target.Code);
                            }
                            else if ((targetOrder != null) && (targetOrder == 0) && (target.Track.Order == 2))
                            {
                                sqlBuilder.SetValue("Einschulmerkmal2", target.Code);
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }

                        using var fbCommand = new FbCommand(sqlBuilder.AsUpdate("\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                        fbCommand.AddParameters(sqlBuilder.Assignments);
                        fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                        fbCommand.Parameters.Add("@id", id);

                        await fbCommand.ExecuteNonQueryAsync();
                    });
                }
            });
        }

        private async Task ImportApplicationTargetTracks(ImportContext importContext)
        {
            string id = null, code = null;
            int? order = default;

            importContext.LookupValue<string>(EcfHeaders.Id, (value) => id = value);
            importContext.LookupValue<string>(EcfHeaders.Code, (value) => code = value);
            importContext.LookupValue<int?>(EcfHeaders.Order, (value) => order = value);

            _applicationTargetTracks.Add(new ApplicationTargetTrack() { Id = id, Code = code, Order = order });

            await Task.CompletedTask;
        }

        private async Task ImportBasicCatalog(string tableName, FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupOrCreateEntityByCodeColumn(tableName, 20, async (insertOrUpdate, code) =>
            {
                var sqlBuilder = new SqlBuilder(tableName);

                sqlBuilder.SetValue("Kuerzel", code);

                importContext.LookupValue<string>(EcfHeaders.Name, (value) => sqlBuilder.SetValue("Bezeichnung", value));
                importContext.LookupValue<string>(EcfHeaders.StatisticalCode, value => sqlBuilder.SetValue("Schluessel", value));

                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Kuerzel\" = @code"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);
                fbCommand.Parameters.Add("@code", code);

                await fbCommand.ExecuteNonQueryAsync();

                return code;
            });
        }

        private async Task ImportCustodian(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupOrCreateEntityByIdColumn("Sorgeberechtigte", _config.TenantId, async (insertOrUpdate, id) =>
            {
                var sqlBuilder = new SqlBuilder("Sorgeberechtigte");

                sqlBuilder.SetValue("Mandant", _config.TenantId);
                sqlBuilder.SetValue("ID", id);
                sqlBuilder.SetValue("Status2", 1);

                importContext.LookupValue<string>(EcfHeaders.Id, (value) => sqlBuilder.SetValue("EnbreaID", IdFactory.CreateIdFromValue(value).ToEnbreaId()));
                importContext.LookupValue<string>(EcfHeaders.LastName, (value) => sqlBuilder.SetValue("Nachname", value));
                importContext.LookupValue<string>(EcfHeaders.FirstName, (value) => sqlBuilder.SetValue("Vorname", value));
                importContext.LookupValue<string>(EcfHeaders.Salutation, (value) => sqlBuilder.SetValue("Anrede", ValueConverter.Salutation(value)));
                importContext.LookupValue<string>(EcfHeaders.Gender, (value) => sqlBuilder.SetValue("Geschlecht", ValueConverter.Gender(value)));
                importContext.LookupValue<string>(EcfHeaders.AddressLines, (value) => sqlBuilder.SetValue("Strasse", value));
                importContext.LookupValue<string>(EcfHeaders.PostalCode, (value) => sqlBuilder.SetValue("PLZ", value));
                importContext.LookupValue<string>(EcfHeaders.Locality, (value) => sqlBuilder.SetValue("Ort", value));
                importContext.LookupValue<string>(EcfHeaders.EmailAddress, (value) => sqlBuilder.SetValue("Email", value));
                importContext.LookupValue<string>(EcfHeaders.MobileNumber, (value) => sqlBuilder.SetValue("Mobil", value));
                importContext.LookupValue<string>(EcfHeaders.HomePhoneNumber, (value) => sqlBuilder.SetValue("TelefonPrivat", value));
                importContext.LookupValue<string>(EcfHeaders.OfficePhoneNumber, (value) => sqlBuilder.SetValue("TelefonBeruf", value));
                importContext.LookupValue<string>(EcfHeaders.Notes, (value) => sqlBuilder.SetValue("Bemerkung", value));

                await importContext.TryLookupEntityByCode(EcfHeaders.CountryId, "Staatsangehoerigkeiten", (value) => sqlBuilder.SetValue("Land", value));

                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);
                fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                fbCommand.Parameters.Add("@id", id);

                return (int)await fbCommand.ExecuteScalarAsync();
            });
        }

        private async Task ImportCustodianRelationshipTypeMapping(ImportContext importContext)
        {
            string id = null, code = null;

            importContext.LookupValue<string>(EcfHeaders.Id, (value) => id = value);
            importContext.LookupValue<string>(EcfHeaders.Code, (value) => code = value);

            _custodianRelationshipTypeMap.Add(id, code);

            await Task.CompletedTask;
        }

        private async Task ImportForeignLanguage(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupOrCreateEntityByIdColumn("Faecher", _config.TenantId, async (insertOrUpdate, id) =>
            {
                var sqlBuilder = new SqlBuilder("Faecher");

                sqlBuilder.SetValue("Mandant", _config.TenantId);
                sqlBuilder.SetValue("ID", id);
                sqlBuilder.SetValue("Kategorie", 0);

                importContext.LookupValue<string>(EcfHeaders.Id, (value) => sqlBuilder.SetValue("EnbreaID", IdFactory.CreateIdFromValue(value).ToEnbreaId()));
                importContext.LookupValue<string>(EcfHeaders.Code, (value) => sqlBuilder.SetValue("Kuerzel", ValueConverter.Limit(value, 20)));
                importContext.LookupValue<string>(EcfHeaders.StatisticalCode, (value) => sqlBuilder.SetValue("StatistikID", ValueConverter.Limit(value, 20)));
                importContext.LookupValue<string>(EcfHeaders.InternalCode, (value) => sqlBuilder.SetValue("Schluessel", ValueConverter.Limit(value, 20)));
                importContext.LookupValue<string>(EcfHeaders.Name, (value) => sqlBuilder.SetValue("Bezeichnung", value));
                importContext.LookupValue<string>(EcfHeaders.SubjectGroupId, (value) => sqlBuilder.SetValue("Gruppe", value));

                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);
                fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                fbCommand.Parameters.Add("@id", id);

                return (int)await fbCommand.ExecuteScalarAsync();
            });
        }

        private async Task ImportGradeValue(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupOrCreateEntityByIdColumn("Noten", _config.TenantId, async (insertOrUpdate, id) =>
            {
                var sqlBuilder = new SqlBuilder("Noten");

                sqlBuilder.SetValue("Mandant", _config.TenantId);
                sqlBuilder.SetValue("ID", id);

                importContext.LookupValue<string>(EcfHeaders.Id, (value) => sqlBuilder.SetValue("EnbreaID", IdFactory.CreateIdFromValue(value).ToEnbreaId()));
                importContext.LookupValue<string>(EcfHeaders.Code, (value) => sqlBuilder.SetValue("Kuerzel", ValueConverter.Limit(value, 20)));
                importContext.LookupValue<string>(EcfHeaders.StatisticalCode, (value) => sqlBuilder.SetValue("Schluessel", ValueConverter.Limit(value, 20)));
                importContext.LookupValue<string>(EcfHeaders.Name, (value) => sqlBuilder.SetValue("Bezeichnung", value));
                importContext.LookupValue<double>(EcfHeaders.Value, (value) => sqlBuilder.SetValue("Notenwert", value));
                importContext.LookupValue<string>(EcfHeaders.GradeSystemId, (value) => sqlBuilder.SetValue("Notenart", ValueConverter.GradeSystem(value)));

                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);
                fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                fbCommand.Parameters.Add("@id", id);

                return (int)await fbCommand.ExecuteScalarAsync();
            });
        }

        private async Task ImportSchoolClass(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupOrCreateEntityByIdColumn("Klassen", _config.TenantId, async (insertOrUpdate, schoolClassId) =>
            {
                var sqlBuilder = new SqlBuilder("Klassen");

                sqlBuilder.SetValue("Mandant", _config.TenantId);
                sqlBuilder.SetValue("ID", schoolClassId);

                importContext.LookupValue<string>(EcfHeaders.Id, (value) => sqlBuilder.SetValue("EnbreaID", IdFactory.CreateIdFromValue(value).ToEnbreaId()));
                importContext.LookupValue<string>(EcfHeaders.Code, (value) => sqlBuilder.SetValue("Kuerzel", ValueConverter.Limit(value, 20)));
                importContext.LookupValue<string>(EcfHeaders.StatisticalCode, (value) => sqlBuilder.SetValue("Schluessel", value));
                importContext.LookupValue<string>(EcfHeaders.Name1, (value) => sqlBuilder.SetValue("Langname1", value));
                importContext.LookupValue<string>(EcfHeaders.Name2, (value) => sqlBuilder.SetValue("Langname2", value));
                importContext.LookupValue<string>(EcfHeaders.GradeSystemId, (value) => sqlBuilder.SetValue("Notenart", ValueConverter.GradeSystem(value)));

                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);
                fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                fbCommand.Parameters.Add("@id", schoolClassId);

                schoolClassId = (int)await fbCommand.ExecuteScalarAsync();

                await importContext.LookupEntityByIdColumn(EcfHeaders.SchoolTermId, "Zeitraeume", async (schoolTermId) =>
                {
                    await importContext.LookupOrCreateEntity("KlassenZeitraeume",
                        "\"Mandant\" = @tenantId and \"Zeitraum\" = @schoolTermId and \"Klasse\" = @schoolClassId",
                        fbCommand =>
                    {
                        fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                        fbCommand.Parameters.Add("@schoolTermId", schoolTermId);
                        fbCommand.Parameters.Add("@schoolClassId", schoolClassId);
                    },
                        async (insertOrUpdate, id) =>
                    {
                        var sqlBuilder = new SqlBuilder("KlassenZeitraeume");

                        sqlBuilder.SetValue("Mandant", _config.TenantId);
                        sqlBuilder.SetValue("ID", id);
                        sqlBuilder.SetValue("Klasse", schoolClassId);
                        sqlBuilder.SetValue("Zeitraum", schoolTermId);

                        importContext.LookupValue<byte?>(EcfHeaders.SchoolClassYear, (value) => sqlBuilder.SetValue("Jahrgang", value));

                        using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                        fbCommand.AddParameters(sqlBuilder.Assignments);
                        fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                        fbCommand.Parameters.Add("@id", id);

                        await fbCommand.ExecuteNonQueryAsync();
                    });
                });

                return (int)schoolClassId;
            }); ;
        }

        private async Task ImportSchoolTerm(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupOrCreateEntityByIdColumn("Zeitraeume", async (insertOrUpdate, id) =>
            {
                var sqlBuilder = new SqlBuilder("Zeitraeume");

                sqlBuilder.SetValue("ID", id);

                importContext.LookupValue<string>(EcfHeaders.Id, (value) => sqlBuilder.SetValue("EnbreaID", IdFactory.CreateIdFromValue(value).ToEnbreaId()));
                importContext.LookupValue<string>(EcfHeaders.Code, (value) => sqlBuilder.SetValue("Kuerzel", value));
                importContext.LookupValue<string>(EcfHeaders.Name1, (value) => sqlBuilder.SetValue("Ausdruck1", value));
                importContext.LookupValue<string>(EcfHeaders.Name2, (value) => sqlBuilder.SetValue("Ausdruck2", value));
                importContext.LookupValue<string>(EcfHeaders.ValidFrom, (value) => sqlBuilder.SetValue("Von", value));
                importContext.LookupValue<string>(EcfHeaders.ValidTo, (value) => sqlBuilder.SetValue("Bis", value));
                importContext.LookupValue<string>(EcfHeaders.Section, (value) => sqlBuilder.SetValue("Art", ValueConverter.TermSection(value)));

                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"ID\" = @id"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);
                fbCommand.Parameters.Add("@id", id);

                return (int)await fbCommand.ExecuteScalarAsync();
            }); ;
        }

        private async Task ImportStudent(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupOrCreateEntityByIdColumn("Schueler", _config.TenantId, async (insertOrUpdate, id) =>
            {
                var sqlBuilder = new SqlBuilder("Schueler");

                sqlBuilder.SetValue("Mandant", _config.TenantId);
                sqlBuilder.SetValue("ID", id);

                if (insertOrUpdate == SqlInsertOrUpdate.Insert)
                {
                    importContext.LookupValueOrDefault<EcfStudentStatus?>(EcfHeaders.Status, null, (value) =>
                    {
                        if (value == null)
                        {
                            sqlBuilder.SetValue("Status", 2);
                            sqlBuilder.SetValue("BewerberStatus", 0);
                        }
                        else if ((bool)(value?.HasFlag(EcfStudentStatus.Applicant)))
                        {
                            sqlBuilder.SetValue("Status", 1);
                            sqlBuilder.SetValue("BewerberStatus", 1);
                        }
                        else if ((bool)(value?.HasFlag(EcfStudentStatus.Enrolled)))
                        {
                            sqlBuilder.SetValue("Status", 3);
                            sqlBuilder.SetValue("BewerberStatus", 1);
                        }
                    });
                }

                importContext.LookupValue<string>(EcfHeaders.Id, (value) => sqlBuilder.SetValue("EnbreaID", IdFactory.CreateIdFromValue(value).ToEnbreaId()));
                importContext.LookupValue<string>(EcfHeaders.LastName, (value) => sqlBuilder.SetValue("Nachname", value));
                importContext.LookupValue<string>(EcfHeaders.FirstName, (value) => sqlBuilder.SetValue("Vorname", value));
                importContext.LookupValue<string>(EcfHeaders.Salutation, (value) => sqlBuilder.SetValue("Anrede", ValueConverter.Salutation(value)));
                importContext.LookupValue<string>(EcfHeaders.Gender, (value) => sqlBuilder.SetValue("Geschlecht", ValueConverter.Gender(value)));
                importContext.LookupValue<DateOnly?>(EcfHeaders.Birthdate, (value) => sqlBuilder.SetValue("Geburtsdatum", value));
                importContext.LookupValue<string>(EcfHeaders.PlaceOfBirth, (value) => sqlBuilder.SetValue("Geburtsort", value));
                importContext.LookupValue<string>(EcfHeaders.AddressLines, (value) => sqlBuilder.SetValue("Strasse", value));
                importContext.LookupValue<string>(EcfHeaders.PostalCode, (value) => sqlBuilder.SetValue("PLZ", value));
                importContext.LookupValue<string>(EcfHeaders.Locality, (value) => sqlBuilder.SetValue("Ort", value));
                importContext.LookupValue<string>(EcfHeaders.EmailAddress, (value) => sqlBuilder.SetValue("EMail", value));
                importContext.LookupValue<string>(EcfHeaders.MobileNumber, (value) => sqlBuilder.SetValue("Mobil", value));
                importContext.LookupValue<string>(EcfHeaders.HomePhoneNumber, (value) => sqlBuilder.SetValue("Telefon", value));
                importContext.LookupValue<string>(EcfHeaders.HealthInsuranceProvider, (value) => sqlBuilder.SetValue("Krankenkasse", value));
                importContext.LookupValue<DateOnly?>(EcfHeaders.EntryDate, (value) => sqlBuilder.SetValue("ZugangAm", value));
                importContext.LookupValue<DateOnly?>(EcfHeaders.ExitDate, (value) => sqlBuilder.SetValue("AbgangAm", value));
                importContext.LookupValue<DateOnly?>(EcfHeaders.FirstEntryDate, (value) => sqlBuilder.SetValue("Grundschuleintritt", value));
                importContext.LookupValue<string>(EcfHeaders.Notes, (value) => sqlBuilder.SetValue("Bemerkung", value));
                importContext.LookupValue<string>(EcfHeaders.TextAttribute1, (value) => sqlBuilder.SetValue("MerkmalB1", value));
                importContext.LookupValue<string>(EcfHeaders.TextAttribute2, (value) => sqlBuilder.SetValue("MerkmalB2", value));
                importContext.LookupValue<string>(EcfHeaders.TextAttribute3, (value) => sqlBuilder.SetValue("MerkmalB3", value));
                importContext.LookupValue<string>(EcfHeaders.TextAttribute4, (value) => sqlBuilder.SetValue("MerkmalB4", value));

                await importContext.TryLookupEntityByCode(EcfHeaders.CountryId, "Staatsangehoerigkeiten", (value) => sqlBuilder.SetValue("Land", value));
                await importContext.TryLookupEntityByCode(EcfHeaders.ReligionId, "Konfessionen", (value) => sqlBuilder.SetValue("Konfession", value));
                await importContext.TryLookupEntityByCode(EcfHeaders.ReligionParticipationId, "RelTeilnahmen", (value) => sqlBuilder.SetValue("RelTeilnahme", value));
                await importContext.TryLookupEntityByCode(EcfHeaders.Nationality1Id, "Staatsangehoerigkeiten", (value) => sqlBuilder.SetValue("Staatsangeh1", value));
                await importContext.TryLookupEntityByCode(EcfHeaders.Nationality2Id, "Staatsangehoerigkeiten", (value) => sqlBuilder.SetValue("Staatsangeh2", value));
                await importContext.TryLookupEntityByCode(EcfHeaders.NativeLanguageId, "Muttersprachen", (value) => sqlBuilder.SetValue("Muttersprache", value)); ;

                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);
                fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                fbCommand.Parameters.Add("@id", id);

                return (int)await fbCommand.ExecuteScalarAsync();
            });
        }

        private async Task ImportStudentCustodian(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupEntityByIdColumn(EcfHeaders.StudentId, "Schueler", _config.TenantId, async (studentId) =>
            {
                await importContext.LookupEntityByIdColumn(EcfHeaders.CustodianId, "Sorgeberechtigte", _config.TenantId, async (custodianId) =>
                {
                    await importContext.LookupOrCreateEntity("SchuelerSorgebe",
                        "\"Mandant\" = @tenantId and \"Schueler\" = @studentId and \"Sorgebe\" = @custodianId",
                        fbCommand =>
                    {
                        fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                        fbCommand.Parameters.Add("@studentId", studentId);
                        fbCommand.Parameters.Add("@custodianId", custodianId);
                    },
                        async (insertOrUpdate, id) =>
                    {
                        var sqlBuilder = new SqlBuilder("SchuelerSorgebe");

                        sqlBuilder.SetValue("Mandant", _config.TenantId);
                        sqlBuilder.SetValue("ID", id);

                        await importContext.TryLookupEntityById(EcfHeaders.StudentId, "Schueler", _config.TenantId, (value) => sqlBuilder.SetValue("Schueler", value));
                        await importContext.TryLookupEntityById(EcfHeaders.CustodianId, "Sorgeberechtigte", _config.TenantId, (value) => sqlBuilder.SetValue("Sorgebe", value));

                        importContext.LookupValue<string>(EcfHeaders.RelationshipTypeId, (value) => sqlBuilder.SetValue("Verhaeltnis", ValueConverter.RelationshipType(_custodianRelationshipTypeMap, value)));
                        importContext.LookupValue<string>(EcfHeaders.CustodianNotification, (value) => sqlBuilder.SetValue("Benachrichtigung", ValueConverter.Notification(value)));

                        using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                        fbCommand.AddParameters(sqlBuilder.Assignments);
                        fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                        fbCommand.Parameters.Add("@id", id);

                        await fbCommand.ExecuteNonQueryAsync();
                    });
                });
            });
        }

        private async Task ImportStudentForeignLanguage(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupEntityByIdColumn(EcfHeaders.StudentId, "Schueler", _config.TenantId, async (studentId) =>
            {
                await importContext.LookupEntityByIdColumn(EcfHeaders.ForeignLanguageId, "Faecher", _config.TenantId, async (languageId) =>
                {
                    if (_studentForeignLanguageCount.TryGetValue(studentId, out int languageCount))
                    {
                        languageCount++;
                        _studentForeignLanguageCount[studentId] = languageCount;
                    }
                    else
                    {
                        languageCount = 1;
                        _studentForeignLanguageCount.Add(studentId, languageCount);
                    }

                    var sqlBuilder = new SqlBuilder("Schueler");

                    var Supported = false;

                    if ((languageCount >= 1) && (languageCount <= 4))
                    {
                        switch (languageCount)
                        {
                            case 1:
                                sqlBuilder.SetValue("Fremdsprache1", languageId);
                                importContext.LookupValue<string>(EcfHeaders.StartSchoolClassYear, value => sqlBuilder.SetValue("Fremdsprache1Von", Regex.Match(value, "^[^\\d]*(\\d+)").ToString()));
                                importContext.LookupValue<string>(EcfHeaders.EndSchoolClassYear, value => sqlBuilder.SetValue("Fremdsprache1Bis", Regex.Match(value, "^[^\\d]*(\\d+)").ToString()));
                                break;
                            case 2:
                                sqlBuilder.SetValue("Fremdsprache2", languageId);
                                importContext.LookupValue<string>(EcfHeaders.StartSchoolClassYear, value => sqlBuilder.SetValue("Fremdsprache2Von", Regex.Match(value, "^[^\\d]*(\\d+)").ToString()));
                                importContext.LookupValue<string>(EcfHeaders.EndSchoolClassYear, value => sqlBuilder.SetValue("Fremdsprache2Bis", Regex.Match(value, "^[^\\d]*(\\d+)").ToString()));
                                break;
                            case 3:
                                sqlBuilder.SetValue("Fremdsprache3", languageId);
                                importContext.LookupValue<string>(EcfHeaders.StartSchoolClassYear, value => sqlBuilder.SetValue("Fremdsprache3Von", Regex.Match(value, "^[^\\d]*(\\d+)").ToString()));
                                importContext.LookupValue<string>(EcfHeaders.EndSchoolClassYear, value => sqlBuilder.SetValue("Fremdsprache3Bis", Regex.Match(value, "^[^\\d]*(\\d+)").ToString()));
                                break;
                            case 4:
                                sqlBuilder.SetValue("Fremdsprache4", languageId);
                                importContext.LookupValue<string>(EcfHeaders.StartSchoolClassYear, value => sqlBuilder.SetValue("Fremdsprache4Von", Regex.Match(value, "^[^\\d]*(\\d+)").ToString()));
                                importContext.LookupValue<string>(EcfHeaders.EndSchoolClassYear, value => sqlBuilder.SetValue("Fremdsprache4Bis", Regex.Match(value, "^[^\\d]*(\\d+)").ToString()));
                                break;
                        }
                        Supported = true;
                    }

                    if (Supported)
                    {
                        using var fbCommand = new FbCommand(sqlBuilder.AsUpdate("\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                        fbCommand.AddParameters(sqlBuilder.Assignments);
                        fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                        fbCommand.Parameters.Add("@id", studentId);

                        await fbCommand.ExecuteNonQueryAsync();
                    }
                });
            });
        }

        private async Task ImportStudentRemark(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupOrCreateEntityByIdColumn("SchuelerBesonderheiten", _config.TenantId, async (insertOrUpdate, id) =>
            {
                var sqlBuilder = new SqlBuilder("SchuelerBesonderheiten");

                sqlBuilder.SetValue("Mandant", _config.TenantId);
                sqlBuilder.SetValue("ID", id);

                importContext.LookupValue<string>(EcfHeaders.StudentId, (value) => sqlBuilder.SetValue("Schueler", value));
                importContext.LookupValue<string>(EcfHeaders.Notes, (value) => sqlBuilder.SetValue("Beschreibung", value));
                importContext.LookupValue<string>(EcfHeaders.ModifiedBy, (value) => sqlBuilder.SetValue("ErfasstVon", value));

                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);
                fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                fbCommand.Parameters.Add("@id", id);

                return (int)await fbCommand.ExecuteScalarAsync();
            });
        }

        private async Task ImportStudentSchoolClassAttendance(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupEntityByIdColumn(EcfHeaders.SchoolTermId, "Zeitraeume", async (schoolTermId) =>
            {
                await importContext.LookupEntityByIdColumn(EcfHeaders.StudentId, "Schueler", _config.TenantId, async (studentId) =>
                {
                    await importContext.LookupEntityByIdColumn(EcfHeaders.SchoolClassId, "Klassen", _config.TenantId, async (schoolClassId) =>
                    {
                        int studentSchoolTermId = 0;

                        await importContext.LookupOrCreateEntity("KlassenZeitraeume",
                            "\"Mandant\" = @tenantId and \"Zeitraum\" = @schoolTermId and \"Klasse\" = @schoolClassId",
                            fbCommand =>
                        {
                            fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                            fbCommand.Parameters.Add("@schoolTermId", schoolTermId);
                            fbCommand.Parameters.Add("@schoolClassId", schoolClassId);
                        },
                            async (insertOrUpdate, SchoolClassTermId) =>
                        {
                            await importContext.LookupOrCreateEntity("SchuelerZeitraeume",
                                "\"Mandant\" = @tenantId and \"Zeitraum\" = @schoolTermId and \"Klasse\" = @schoolClassId and \"Schueler\" = @studentId",
                                fbCommand =>
                            {
                                fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                                fbCommand.Parameters.Add("@schoolTermId", schoolTermId);
                                fbCommand.Parameters.Add("@schoolClassId", schoolClassId);
                                fbCommand.Parameters.Add("@studentId", studentId);
                            },
                                async (insertOrUpdate, id) =>
                            {
                                var sqlBuilder = new SqlBuilder("SchuelerZeitraeume");

                                sqlBuilder.SetValue("Mandant", _config.TenantId);
                                sqlBuilder.SetValue("ID", id);
                                sqlBuilder.SetValue("KlassenZeitraumID", SchoolClassTermId);
                                sqlBuilder.SetValue("Schueler", studentId);
                                sqlBuilder.SetValue("Klasse", schoolClassId);
                                sqlBuilder.SetValue("Zeitraum", schoolTermId);
                                sqlBuilder.SetValue("Status", schoolTermId != _config.SchoolTermId ? "+" : "N");
                                sqlBuilder.SetValue("TeilnahmeZusatzangebot", "N");
                                sqlBuilder.SetValue("SportBefreit", "N");

                                await importContext.TryLookupEntityById(EcfHeaders.TutorId, "tblLehrer", _config.TenantId, (value) => sqlBuilder.SetValue("Tutor", value));

                                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                                fbCommand.AddParameters(sqlBuilder.Assignments);
                                fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                                fbCommand.Parameters.Add("@id", id);

                                studentSchoolTermId = (int)await fbCommand.ExecuteScalarAsync();
                            });
                        });

                        await importContext.LookupOrCreateEntity("SchuelerKlassen",
                            "\"Mandant\" = @tenantId and \"SchuelerZeitraumID\" = @studentSchoolTermId",
                            fbCommand =>
                        {
                            fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                            fbCommand.Parameters.Add("@studentSchoolTermId", studentSchoolTermId);
                        },
                        async (insertOrUpdate) =>
                        {
                            var sqlBuilder = new SqlBuilder("SchuelerKlassen");

                            sqlBuilder.SetValue("Mandant", _config.TenantId);
                            sqlBuilder.SetValue("SchuelerZeitraumID", studentSchoolTermId);
                            sqlBuilder.SetValue("Hauptklasse", "J");
                            sqlBuilder.SetValue("Ueberspringer", "N");
                            sqlBuilder.SetValue("AufnahmepruefungBestanden", "N");
                            sqlBuilder.SetValue("NachpruefungBestanden", "N");

                            importContext.LookupValue<string>(EcfHeaders.Status, value => sqlBuilder.SetValue("Wiederholer", value == "W" ? "J" : string.Empty));

                            using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"SchuelerZeitraumID\" = @studentSchoolTermId"), fbConnection, fbTransaction);

                            fbCommand.AddParameters(sqlBuilder.Assignments);
                            fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                            fbCommand.Parameters.Add("@SchuelerZeitraumID", studentSchoolTermId);

                            await fbCommand.ExecuteNonQueryAsync();
                        });

                    });
                });
            });
        }

        private async Task ImportStudentSubject(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupEntityByIdColumn(EcfHeaders.SchoolTermId, "Zeitraeume", async (schoolTermId) =>
            {
                await importContext.LookupEntityByIdColumn(EcfHeaders.SchoolClassId, "Klassen", async (schoolClassId) =>
                {
                    await importContext.LookupEntityByIdColumn(EcfHeaders.StudentId, "Schueler", _config.TenantId, async (studentId) =>
                    {
                        await importContext.LookupEntity("SchuelerZeitraeume",
                            "\"Mandant\" = @tenantId and \"Zeitraum\" = @schoolTermId and \"Klasse\" = @schoolClassId and \"Schueler\" = studentId",
                            fbCommand =>
                        {
                            fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                            fbCommand.Parameters.Add("@schoolTermId", schoolTermId);
                            fbCommand.Parameters.Add("@schoolClassId", schoolClassId);
                            fbCommand.Parameters.Add("@studentId", studentId);
                        },
                            async (studentSchoolTermId) =>
                        {
                            await importContext.LookupEntityByIdColumn(EcfHeaders.SubjectId, "Faecher", _config.TenantId, async (subjectId) =>
                            {
                                int? courseNo = null;
                                string courseTypeCode = null;

                                importContext.LookupValue<int?>(EcfHeaders.CourseNo, (value) => courseNo = value);

                                await importContext.TryLookupEntityByCode(EcfHeaders.CourseTypeId, "Unterrichtsarten", (value) => courseTypeCode = value);

                                await importContext.LookupOrCreateEntity("SchuelerFachdaten",
                                    "\"Mandant\" = @tenantId and \"SchuelerZeitraumID\" = @studentSchoolTermId and \"Fach\" = @subjectId and \"KursNr\" = @courseNo and \"Unterrichtsart\" = @courseTypeCode",
                                    fbCommand =>
                                {
                                    fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                                    fbCommand.Parameters.Add("@studentSchoolTermId", studentSchoolTermId);
                                    fbCommand.Parameters.Add("@Fach", subjectId);
                                    fbCommand.Parameters.Add("@courseNo", courseNo);
                                    fbCommand.Parameters.Add("@courseTypeCode", courseTypeCode);
                                },
                                    async (insertOrUpdate, id) =>
                                {
                                    var sqlBuilder = new SqlBuilder("SchuelerFachdaten");

                                    sqlBuilder.SetValue("Mandant", _config.TenantId);
                                    sqlBuilder.SetValue("ID", id);
                                    sqlBuilder.SetValue("SchuelerZeitraumID", studentSchoolTermId);
                                    sqlBuilder.SetValue("Zeitraum", schoolTermId);
                                    sqlBuilder.SetValue("Klasse", schoolClassId);
                                    sqlBuilder.SetValue("Fach", subjectId);
                                    sqlBuilder.SetValue("KursNr", courseNo);
                                    sqlBuilder.SetValue("Unterrichtsart", courseTypeCode);

                                    importContext.LookupValue<string>(EcfHeaders.Passfail, value => sqlBuilder.SetValue("Bestanden", ValueConverter.Passfail(value)));

                                    await importContext.LookupEntityByIdColumn(EcfHeaders.StudentId, "Schueler", _config.TenantId, (value) => sqlBuilder.SetValue("Schueler", value));
                                    await importContext.TryLookupEntityById(EcfHeaders.TeacherId, "tblLehrer", _config.TenantId, (value) => sqlBuilder.SetValue("Lehrer", value));
                                    await importContext.TryLookupEntityById(EcfHeaders.Grade1ValueId, "Noten", _config.TenantId, (value) => sqlBuilder.SetValue("Endnote1", value)); ;
                                    await importContext.TryLookupEntityByCode(EcfHeaders.Grade1AchievementTypeId, "Leistungsarten", (value) => sqlBuilder.SetValue("Leistungsart", value));

                                    using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                                    fbCommand.AddParameters(sqlBuilder.Assignments);
                                    fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                                    fbCommand.Parameters.Add("@id", id);

                                    await fbCommand.ExecuteNonQueryAsync();
                                });
                            });
                        });
                    });
                });
            });
        }

        private async Task ImportSubject(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupOrCreateEntityByIdColumn("Faecher", _config.TenantId, async (insertOrUpdate, id) =>
            {
                var sqlBuilder = new SqlBuilder("Faecher");

                sqlBuilder.SetValue("Mandant", _config.TenantId);
                sqlBuilder.SetValue("ID", id);

                importContext.LookupValue<string>(EcfHeaders.Id, (value) => sqlBuilder.SetValue("EnbreaID", IdFactory.CreateIdFromValue(value).ToEnbreaId()));
                importContext.LookupValue<string>(EcfHeaders.Code, (value) => sqlBuilder.SetValue("Kuerzel", ValueConverter.Limit(value, 20)));
                importContext.LookupValue<string>(EcfHeaders.StatisticalCode, (value) => sqlBuilder.SetValue("StatistikID", ValueConverter.Limit(value, 20)));
                importContext.LookupValue<string>(EcfHeaders.InternalCode, (value) => sqlBuilder.SetValue("Schluessel", ValueConverter.Limit(value, 20)));
                importContext.LookupValue<string>(EcfHeaders.Name, (value) => sqlBuilder.SetValue("Bezeichnung", value));
                importContext.LookupValue<string>(EcfHeaders.SubjectGroupId, (value) => sqlBuilder.SetValue("Gruppe", value));

                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);
                fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                fbCommand.Parameters.Add("@id", id);

                return (int)await fbCommand.ExecuteScalarAsync();
            });
        }

        private async Task ImportTeacher(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupOrCreateEntityByIdColumn("tblLehrer", _config.TenantId, async (insertOrUpdate, id) =>
            {
                var sqlBuilder = new SqlBuilder("tblLehrer");

                sqlBuilder.SetValue("Mandant", _config.TenantId);
                sqlBuilder.SetValue("ID", id);

                importContext.LookupValue<string>(EcfHeaders.Id, (value) => sqlBuilder.SetValue("EnbreaID", IdFactory.CreateIdFromValue(value).ToEnbreaId()));
                importContext.LookupValue<string>(EcfHeaders.Code, (value) => sqlBuilder.SetValue("Kuerzel", ValueConverter.Limit(value, 20)));
                importContext.LookupValue<string>(EcfHeaders.LastName, (value) => sqlBuilder.SetValue("Nachname", value));
                importContext.LookupValue<string>(EcfHeaders.FirstName, (value) => sqlBuilder.SetValue("Vorname", value));
                importContext.LookupValue<string>(EcfHeaders.Salutation, (value) => sqlBuilder.SetValue("Anrede", ValueConverter.Salutation(value)));
                importContext.LookupValue<string>(EcfHeaders.Gender, (value) => sqlBuilder.SetValue("Geschlecht", ValueConverter.Gender(value)));
                importContext.LookupValue<DateTime?>(EcfHeaders.Birthdate, (value) => sqlBuilder.SetValue("Geburtsdatum", value));
                importContext.LookupValue<string>(EcfHeaders.AddressLines, (value) => sqlBuilder.SetValue("Strasse", value));
                importContext.LookupValue<string>(EcfHeaders.PostalCode, (value) => sqlBuilder.SetValue("PLZ", value));
                importContext.LookupValue<string>(EcfHeaders.Locality, (value) => sqlBuilder.SetValue("Ort", value));
                importContext.LookupValue<string>(EcfHeaders.EmailAddress, (value) => sqlBuilder.SetValue("Email", value));
                importContext.LookupValue<string>(EcfHeaders.MobileNumber, (value) => sqlBuilder.SetValue("Mobil", value));
                importContext.LookupValue<string>(EcfHeaders.HomePhoneNumber, (value) => sqlBuilder.SetValue("Telefon", value));
                importContext.LookupValue<string>(EcfHeaders.OfficePhoneNumber, (value) => sqlBuilder.SetValue("TelefonDienst", value));

                await importContext.TryLookupEntityByCode(EcfHeaders.CountryId, "Staatsangehoerigkeiten", (value) => sqlBuilder.SetValue("Land", value));
                await importContext.TryLookupEntityByCode(EcfHeaders.Nationality1Id, "Staatsangehoerigkeiten", (value) => sqlBuilder.SetValue("Staatsangeh", value));
                await importContext.TryLookupEntityByCode(EcfHeaders.Nationality2Id, "Staatsangehoerigkeiten", (value) => sqlBuilder.SetValue("Staatsangeh2", value));

                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);
                fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                fbCommand.Parameters.Add("@id", id);

                return (int)await fbCommand.ExecuteScalarAsync();
            });
        }

        private async Task ImporWorkforce(FbConnection fbConnection, FbTransaction fbTransaction, ImportContext importContext)
        {
            await importContext.LookupOrCreateEntityByIdColumn("Personen", _config.TenantId, async (insertOrUpdate, id) =>
            {
                var sqlBuilder = new SqlBuilder("Personen");

                sqlBuilder.SetValue("Mandant", _config.TenantId);
                sqlBuilder.SetValue("ID", id);

                importContext.LookupValue<string>(EcfHeaders.Id, (value) => sqlBuilder.SetValue("EnbreaID", IdFactory.CreateIdFromValue(value).ToEnbreaId()));

                importContext.LookupValue<string>(EcfHeaders.Code, (value) => sqlBuilder.SetValue("Kuerzel", ValueConverter.Limit(value, 20)));
                importContext.LookupValue<string>(EcfHeaders.LastName, (value) => sqlBuilder.SetValue("Nachname", value));
                importContext.LookupValue<string>(EcfHeaders.FirstName, (value) => sqlBuilder.SetValue("Vorname", value));
                importContext.LookupValue<string>(EcfHeaders.Salutation, (value) => sqlBuilder.SetValue("Anrede", ValueConverter.Salutation(value)));
                importContext.LookupValue<string>(EcfHeaders.Gender, (value) => sqlBuilder.SetValue("Geschlecht", ValueConverter.Gender(value)));
                importContext.LookupValue<DateTime?>(EcfHeaders.Birthdate, (value) => sqlBuilder.SetValue("Geburtsdatum", value));
                importContext.LookupValue<string>(EcfHeaders.AddressLines, (value) => sqlBuilder.SetValue("Strasse", value));
                importContext.LookupValue<string>(EcfHeaders.PostalCode, (value) => sqlBuilder.SetValue("PLZ", value));
                importContext.LookupValue<string>(EcfHeaders.Locality, (value) => sqlBuilder.SetValue("Ort", value));
                importContext.LookupValue<string>(EcfHeaders.EmailAddress, (value) => sqlBuilder.SetValue("Email", value));
                importContext.LookupValue<string>(EcfHeaders.MobileNumber, (value) => sqlBuilder.SetValue("Mobil", value));

                await importContext.TryLookupEntityByCode(EcfHeaders.CountryId, "Staatsangehoerigkeiten", (value) => sqlBuilder.SetValue("Land", value));
                await importContext.TryLookupEntityByCode(EcfHeaders.Nationality1Id, "Staatsangehoerigkeiten", (value) => sqlBuilder.SetValue("Staatsangeh1", value));
                await importContext.TryLookupEntityByCode(EcfHeaders.Nationality2Id, "Staatsangehoerigkeiten", (value) => sqlBuilder.SetValue("Staatsangeh2", value));

                using var fbCommand = new FbCommand(sqlBuilder.AsInsertOrUpdate(insertOrUpdate, "\"Mandant\" = @tenantId and \"ID\" = @id"), fbConnection, fbTransaction);

                fbCommand.AddParameters(sqlBuilder.Assignments);
                fbCommand.Parameters.Add("@tenantId", _config.TenantId);
                fbCommand.Parameters.Add("@id", id);

                return (int)await fbCommand.ExecuteScalarAsync();
            }); ;
        }

        private async Task<int> ReadMagellanVersion(FbConnection fbConnection, FbTransaction fbTransaction)
        {
            var sql =
                """
                select first 1 
                  "Release"
                from 
                  "Version";
                """;

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

            return result;
        }

        private class ApplicationTarget
        {
            public string Code;
            public string Id;
            public string Name;
            public ApplicationTargetTrack Track;
        }

        private class ApplicationTargetTrack
        {
            public string Code;
            public string Id;
            public int? Order;
        }
    }
}
