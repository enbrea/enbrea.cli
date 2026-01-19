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
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli
{
    /// <summary>
    /// Import workflow from ECF to Enbrea
    /// </summary>
    public class ImportManager : CustomManager
    {
        private readonly ImportBehaviour _behaviour;
        private readonly ImportProvider _provider;
        private readonly ProviderEcfMapping _providerEcfMapping;
        private readonly bool _skipSnapshot;
        private readonly bool _skipImport;

        public ImportManager(
            ImportProvider provider,
            ProviderEcfMapping providerEcfMapping,
            string dataFolderName,
            Configuration config,
            ImportBehaviour behaviour,
            bool skipSnapshot,
            bool skipImport,
            ConsoleWriter consoleWriter, 
            EventWaitHandle cancellationEvent, 
            CancellationToken cancellationToken)
            : base(config, dataFolderName, consoleWriter, cancellationEvent, cancellationToken)
        {
            _provider = provider;
            _providerEcfMapping = providerEcfMapping;
            _skipSnapshot = skipSnapshot;
            _skipImport = skipImport;
            _behaviour = behaviour;
        }

        public override async Task Execute()
        {
            if (_config.EcfMapping.Files.Count > 0)
            {
                var files = GetImportFiles();
                try
                {
                    // Provider ECF file mapping 
                    if (_providerEcfMapping?.Files != null)
                    {
                        await EcfUtils.ApplyProviderExportFileMappings(files, _providerEcfMapping.Files);
                    }

                    // Try load previous context data
                    var previousContext = await ImportContextManager.LoadFromFileAsync(GetCtxFileName(), _cancellationToken);

                    // Try load ECF manifest
                    var manifest = await EcfManifestManager.LoadFromFileAsync(GetEcfManifestFileName(), _cancellationToken);

                    // Initialize helper variable
                    var newImportDataAvailable = false;

                    // Has context changed?
                    if ((previousContext.SchoolTerm != _config.SchoolTerm) ||
                        (previousContext.ValidFrom != manifest.ValidFrom) ||
                        (previousContext.ValidTo != manifest.ValidTo) || _behaviour == ImportBehaviour.full)
                    {
                        // Console output
                        _consoleWriter.Caption("Delete previous ECF files");

                        // Delete previous ECF files, we will do an complete import
                        DeletePreviousFiles(files);

                        // We have something to import
                        newImportDataAvailable = true;
                    }
                    else
                    {
                        // Console output
                        _consoleWriter.Caption("Generate diff ECF files");

                        // Try to generate ECF diff files
                        var changedOnlyFileCount = await GenerateChangedOnlyFiles(files);
                        var deletedOnlyFileCount = await GenerateDeletedOnlyFiles(files);

                        // Do we have something to import?
                        newImportDataAvailable = changedOnlyFileCount > 0 || deletedOnlyFileCount > 0;
                    }

                    // Do we have data to import?
                    if (!_skipImport && newImportDataAvailable)
                    {
                        // Should we create a database snapshot?
                        if (!_skipSnapshot)
                        {
                            // Console output
                            _consoleWriter.NewLine().Caption("Connect and create database snapshot");

                            // Create a database snapshot
                            await CreateSnapshot();
                        }

                        // Console output
                        _consoleWriter.NewLine().Caption("Connect and create new import job");

                        // Create new import job for Enbrea
                        var jobId = await CreateJob(_config, manifest);

                        // Console output
                        _consoleWriter.NewLine().Caption("Upload ECF files");

                        // Upload ECF files
                        var fileCount = await Upload(jobId, files);

                        // Any files uploaded?
                        if (fileCount > 0)
                        {
                            // Console output
                            _consoleWriter.NewLine().Caption("Merge uploaded data");

                            // Merge ECF files
                            await Merge(jobId);
                        }
                    }

                    // Console output
                    _consoleWriter.NewLine().Caption("Clean up");

                    // Save current context data for future imports
                    await SaveContext(manifest);

                    // Delete and rename files
                    CleanUpFiles(files, true);
                }
                catch
                {
                    // Console output
                    _consoleWriter.NewLine().Caption("Clean up");

                    // Delete and rename files
                    CleanUpFiles(files, false);

                    throw;
                }
            }
            else
            {
                throw new ImportException($"No import tables in configuration found.");
            }
        }

        protected void CleanUpFiles(IEnumerable<ImportFile> files, bool successfullImport)
        {
            var fileCount = 0;

            _consoleWriter.StartProgress("Cleanup files", fileCount);
            try
            {
                foreach (var file in files)
                {
                    if (File.Exists(file.FullName))
                    {
                        if (successfullImport)
                        {
                            if (File.Exists(file.FullNameForPreviousRows))
                            {
                                File.Delete(file.FullNameForPreviousRows);
                            }
                            if (File.Exists(file.FullNameForChangedOnlyRows))
                            {
                                File.Delete(file.FullNameForChangedOnlyRows);
                            }
                            if (File.Exists(file.FullNameForDeletedOnlyRows))
                            {
                                File.Delete(file.FullNameForDeletedOnlyRows);
                            }

                            File.Move(file.FullName, file.FullNameForPreviousRows);
                        }
                        else
                        {
                            File.Delete(file.FullName);
                        }

                        _consoleWriter.ContinueProgress(++fileCount);
                    }
                }

                if (File.Exists(GetEcfManifestFileName()))
                {
                    File.Delete(GetEcfManifestFileName());

                    _consoleWriter.ContinueProgress(++fileCount);
                }

                _consoleWriter.FinishProgress();
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        protected async Task<Guid> CreateJob(Configuration config, EcfManifest manifest)
        {
            _consoleWriter.StartProgress("Create new import job...");
            try
            {
                var response = await _httpClient.PostAsync("imports/jobs",
                    _config,
                    new CreateImportJobOptions(
                        config.SchoolTerm,
                        _provider,
                        manifest),
                    _cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    await ThrowImportException("Create job failed", response);
                }

                var responseBody = await response.Content.ReadFromJsonAsync<Reference>();

                _consoleWriter.FinishProgress();

                return responseBody.Id;
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        protected async Task CreateSnapshot()
        {
            _consoleWriter.StartProgress("Create database snapshot...");
            try
            {
                var response = await _httpClient.PostAsync("snapshots",
                    _config,
                    _cancellationToken);

                if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
                {
                    await ThrowSnapshotException("Create database snapshot failed", response);
                }

                var responseBody = await JsonSerializer.DeserializeAsync<SnapshotsListDto>(response.Content.ReadAsStream());

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _consoleWriter.FinishProgress().Warning($"Latest snapshot with uid {responseBody.items.First().uid} from {responseBody.items.First().timestamp}");
                }
                else
                {
                    _consoleWriter.FinishProgress().Success($"Snapshot with uid {responseBody.items.First().uid} successfully created");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        protected async Task<Guid> CreateTable(Guid jobId, ImportFile file, ImportTableContentType contentType)
        {
            var response = await _httpClient.PostAsync($"imports/jobs/{jobId}/tables",
                _config,
                new CreateImportTableOptions(
                    file.TableName,
                    file.KeyHeaders,
                    contentType),
                _cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await ThrowImportException("Create table failed", response);
            }

            var responseBody = await response.Content.ReadFromJsonAsync<Reference>();

            return responseBody.Id;
        }

        protected void DeletePreviousFiles(IEnumerable<ImportFile> files)
        {
            var fileCount = 0;

            _consoleWriter.StartProgress("Cleanup files", fileCount);
            try
            {
                foreach (var file in files)
                {
                    if (File.Exists(file.FullNameForPreviousRows))
                    {
                        File.Delete(file.FullNameForPreviousRows);
                    }
                    if (File.Exists(file.FullNameForChangedOnlyRows))
                    {
                        File.Delete(file.FullNameForChangedOnlyRows);
                    }
                    if (File.Exists(file.FullNameForDeletedOnlyRows))
                    {
                        File.Delete(file.FullNameForDeletedOnlyRows);
                    }

                    _consoleWriter.ContinueProgress(++fileCount);
                }

                _consoleWriter.FinishProgress();

                Log.Information($"{fileCount} files cleaned up");
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        protected async Task<int> GenerateChangedOnlyFiles(IEnumerable<ImportFile> files)
        {
            var fileCount = 0;

            _consoleWriter.StartProgress("Generate files with changed only rows", fileCount);
            try
            {
                foreach (var file in files)
                {
                    if (File.Exists(file.FullName))
                    {
                        if (File.Exists(file.FullNameForPreviousRows))
                        {
                            if (_behaviour == ImportBehaviour.smartfull)
                            {
                                File.Copy(file.FullName, file.FullNameForChangedOnlyRows, overwrite: true);

                                _consoleWriter.ContinueProgress(++fileCount);
                            }
                            else
                            {
                                var numberOfAffectedRows = 0;

                                using var ecfTextReader = File.OpenText(file.FullName);
                                using var ecfTextReaderForPreviousRows = File.OpenText(file.FullNameForPreviousRows);
                                using (var ecfTextWriterForChangedOnlyRows = File.CreateText(file.FullNameForChangedOnlyRows))
                                {
                                    var ecfTableReader = new EcfTableReader(ecfTextReader);
                                    var ecfTableReaderForPreviousRows = new EcfTableReader(ecfTextReaderForPreviousRows);
                                    var ecfTableDiff = new CsvDiff(ecfTextWriterForChangedOnlyRows, new EcfConfiguration(), EcfHeaders.Id);

                                    numberOfAffectedRows = await ecfTableDiff.GenerateAsync(CsvDiffStrategy.AddedOrUpdatedOnly, ecfTableReaderForPreviousRows, ecfTableReader, _cancellationToken);
                                }

                                if (numberOfAffectedRows == 0)
                                {
                                    File.Delete(file.FullNameForChangedOnlyRows);
                                }
                                else
                                {
                                    _consoleWriter.ContinueProgress(++fileCount);
                                }
                            }
                        }
                        else
                        {
                            if (File.Exists(file.FullNameForChangedOnlyRows))
                            {
                                File.Delete(file.FullNameForChangedOnlyRows);
                            }
                            _consoleWriter.ContinueProgress(++fileCount);
                        }
                    }
                }
                _consoleWriter.FinishProgress(fileCount);

                return fileCount;
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        protected async Task<int> GenerateDeletedOnlyFiles(IEnumerable<ImportFile> files)
        {
            var fileCount = 0;

            _consoleWriter.StartProgress("Generate files with deleted only rows", fileCount);
            try
            {
                foreach (var file in files)
                {
                    if (File.Exists(file.FullNameForPreviousRows))
                    {
                        if (File.Exists(file.FullName))
                        {
                            var numberOfAffectedRows = 0;

                            using var ecfTextReader = File.OpenText(file.FullName);
                            using var ecfTextReaderForPreviousRows = File.OpenText(file.FullNameForPreviousRows);
                            using (var ecfTextWriterForDeletedOnlyRows = File.CreateText(file.FullNameForDeletedOnlyRows))
                            {
                                var ecfTableReader = new EcfTableReader(ecfTextReader);
                                var ecfTableReaderForPreviousRows = new EcfTableReader(ecfTextReaderForPreviousRows);
                                var ecfTableDiff = new CsvDiff(ecfTextWriterForDeletedOnlyRows, new EcfConfiguration(), EcfHeaders.Id);

                                numberOfAffectedRows = await ecfTableDiff.GenerateAsync(CsvDiffStrategy.DeletedOnly, ecfTableReaderForPreviousRows, ecfTableReader, _cancellationToken);
                            }

                            if (numberOfAffectedRows == 0)
                            {
                                File.Delete(file.FullNameForDeletedOnlyRows);
                            }
                            else
                            {
                                _consoleWriter.ContinueProgress(++fileCount);
                            }
                        }
                        else
                        {
                            File.Copy(file.FullNameForPreviousRows, file.FullNameForDeletedOnlyRows, true);

                            _consoleWriter.ContinueProgress(++fileCount);
                        }
                    }
                    else 
                    {
                        if (File.Exists(file.FullNameForDeletedOnlyRows))
                        {
                            File.Delete(file.FullNameForDeletedOnlyRows);
                        }
                    }
                }
                _consoleWriter.FinishProgress(fileCount);

                return fileCount;
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        protected List<ImportFile> GetImportFiles()
        {
            var files = new List<ImportFile>();

            foreach (var fileMapping in _config.EcfMapping.Files)
            {
                files.Add(new ImportFile(GetEcfFolderName(), fileMapping));
            }

            return files;
        }

        protected async Task Merge(Guid jobId)
        {
            var successfullMerge = false;

            using var finishEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

            var connection = new HubConnectionBuilder()
                .WithUrl(new Uri(_config.GetSyncHubUrlWithTrailingSlash(), "ImportHub"))
                .Build();

            connection.Closed += (error) =>
            {
                if (error != null)
                {
                    _consoleWriter.Error($"Server lost: {error.Message}");
                    _cancellationEvent.Set();
                }
                return Task.CompletedTask;
            };

            await connection.StartAsync(_cancellationToken);

            connection.On<Guid, string>("MergeTableBegin", (ecfJobId, ecfTableName) =>
            {
                if (ecfJobId == jobId)
                {
                    _consoleWriter.StartProgress($"Merging {ecfTableName}...");
                }
            });

            connection.On<Guid, string, int>("MergeTableEnd", (ecfJobId, ecfTableName, ecfRecordCounter) =>
            {
                if (ecfJobId == jobId)
                {
                    _consoleWriter.FinishProgress(ecfRecordCounter);
                }
            });

            connection.On<Guid>("MergeBegin", (ecfJobId) =>
            {
                if (ecfJobId == jobId)
                {
                    _consoleWriter.Message("Start merging...");
                }
            });

            connection.On<Guid, int, int>("MergeSuccessfull", (ecfJobId, ecfTableCounter, ecfRecordCounter) =>
            {
                if (ecfJobId == jobId)
                {
                    _consoleWriter.Success($"{ecfTableCounter} table(s) and {ecfRecordCounter} record(s) merged");
                    successfullMerge = true;
                    finishEvent.Set();
                }
            });

            connection.On<Guid, int, int, string>("MergeFailed", (ecfJobId, ecfTableCounter, ecfRecordCounter, errorMessage) =>
            {
                if (ecfJobId == jobId)
                {
                    _consoleWriter.CancelProgress();
                    _consoleWriter.Error($"Merging failed. Reason: {errorMessage}");
                    finishEvent.Set();
                }
            });

            connection.On<Guid, string, int>("RecordMerged", (ecfJobId, ecfTableName, ecfRecordCounter) =>
            {
                if (ecfJobId == jobId)
                {
                    _consoleWriter.ContinueProgress(ecfRecordCounter);
                }
            });

            var response = await _httpClient.PostAsync($"imports/jobQueue/jobs/{jobId}",
                _config,
                _cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await ThrowImportException("Start merging failed", response);
            }

            WaitHandle.WaitAny([finishEvent, _cancellationEvent]);

            if (!successfullMerge)
            {
                await ThrowImportException("Merging failed", response);
            }

            await connection.StopAsync(_cancellationToken);
        }

        protected async Task SaveContext(EcfManifest manifest)
        {
            _consoleWriter.StartProgress("Save context");
            try
            {
                var context = new ImportContext
                {
                    SchoolTerm = _config.SchoolTerm,
                    ValidFrom = manifest.ValidFrom,
                    ValidTo = manifest.ValidTo
                };

                await ImportContextManager.SaveToFileAsync(GetCtxFileName(), context, _cancellationToken);

                _consoleWriter.FinishProgress();
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        protected async Task<int> Upload(Guid jobId, IEnumerable<ImportFile> files)
        {
            var fileCount = 0;

            foreach (var file in files)
            {
                if (File.Exists(file.FullNameForPreviousRows))
                {
                    if (File.Exists(file.FullNameForChangedOnlyRows))
                    {
                        await UploadFile(jobId, file, file.FullNameForChangedOnlyRows, ImportTableContentType.ChangedOnly);
                        fileCount++;
                    }
                    if (File.Exists(file.FullNameForDeletedOnlyRows))
                    {
                        await UploadFile(jobId, file, file.FullNameForDeletedOnlyRows, ImportTableContentType.DeletedOnly);
                        fileCount++;
                    }
                }
                else
                {
                    if (File.Exists(file.FullName))
                    {
                        await UploadFile(jobId, file, file.FullName, ImportTableContentType.Complete);
                        fileCount++;
                    }
                }
            }

            _consoleWriter.Success($"{fileCount} files uploaded");

            return fileCount;
        }

        protected async Task UploadFile(Guid jobId, ImportFile file, string ecfFileName, ImportTableContentType contentType)
        {
            _consoleWriter.StartProgress($"Upload file {Path.GetFileName(ecfFileName)}...");
            try
            {
                var tableId = await CreateTable(jobId, file, contentType);
                await UploadTable(jobId, tableId, ecfFileName);

                _consoleWriter.FinishProgress();
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        protected async Task UploadTable(Guid jobId, Guid tableId, string ecfFileName)
        {
            using var fileStream = File.OpenRead(ecfFileName);
            {
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { FileName = Path.GetFileName(ecfFileName) };

                var response = await _httpClient.PostAsync($"imports/jobs/{jobId}/tables/{tableId}",
                    _config,
                    streamContent,
                    _cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    await ThrowImportException("Upload of table failed", response);
                }
            }
        }

        private static async Task ThrowImportException(string message, HttpResponseMessage serverResponse)
        {
            throw new ImportException(message, serverResponse.StatusCode, await serverResponse.Content.ReadAsStringAsync());
        }

        private async Task ThrowSnapshotException(string message, HttpResponseMessage serverResponse)
        {
            throw new SnapshotException(message, serverResponse.StatusCode, await serverResponse.Content.ReadAsStringAsync());
        }
    }
}
