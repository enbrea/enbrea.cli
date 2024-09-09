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
using Enbrea.Konsoli;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli
{
    public class ExportManager : CustomManager
    {
        private readonly ExportProvider _provider;

        public ExportManager(
            ExportProvider provider, 
            string ecfFolderName,
            Configuration config,
            ConsoleWriter consoleWriter, 
            EventWaitHandle cancellationEvent, 
            CancellationToken cancellationToken)
            : base(config, ecfFolderName, consoleWriter, cancellationEvent, cancellationToken)
        {
            _provider = provider;
        }

        public override async Task Execute()
        {
            if (_config.EcfMapping.Files.Count > 0)
            {
                // Create ouput directory
                Directory.CreateDirectory(GetEcfFolderName());

                // Console output
                _consoleWriter.Caption("Create new export job");

                // Delete old export jobs
                await DeleteOldJobs();

                // Create new export job
                var jobId = await CreateJob(_config);

                // Console output
                _consoleWriter.NewLine().Caption("Create export tables");

                // Create export tables
                var tableCount = 0;
                var tables = new Dictionary<Guid, EcfFileMapping>();

                foreach (var file in _config.EcfMapping.Files)
                {
                    tables.Add(await CreateTable(jobId, file), file);
                    tableCount++;
                }

                // Console output
                _consoleWriter.Success($"{tableCount} tables created");

                // Console output
                _consoleWriter.NewLine().Caption("Extract export data");

                // Extract data to export tables
                await Extract(jobId);

                // Console output
                _consoleWriter.NewLine().Caption("Download ECF files");

                // Download ECF files
                var fileCount = 0;

                foreach (var table in tables)
                {
                    await DownloadFile(jobId, table.Key, table.Value);

                    fileCount++;
                }

                // Console output
                _consoleWriter.Success($"{fileCount} files downloaded").NewLine(); 
            }
            else
            {
                throw new ExportException("No export tables in configuration found.");
            }
        }

        protected async Task<Guid> CreateJob(Configuration config)
        {
            _consoleWriter.StartProgress("Create job...");
            try
            {
                var response = await _httpClient.PostAsync("exports/jobs",
                    _config,
                    new CreateExportJobOptions(
                        config.SchoolTerm,
                        config.ApplicationProcess,
                        _provider),
                    _cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    await ThrowExportException("Create job failed", response);
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

        protected async Task<Guid> CreateTable(Guid jobId, EcfFileMapping file)
        {
            _consoleWriter.StartProgress($"Create table for {file.Name}...");
            try
            {
                var response = await _httpClient.PostAsync($"exports/jobs/{jobId}/tables", _config,
                    new CreateExportTableOptions(file.Name, file.KeyHeaders),
                    _cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    await ThrowExportException($"Create table failed.", response);
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

        protected async Task DeleteOldJobs()
        {
            _consoleWriter.StartProgress("Delete successfull and failed jobs...");
            try
            {
                var response = await _httpClient.DeleteAsync("exports/jobs",
                    _config,
                    _cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    await ThrowExportException("Delete jobs failed", response);
                }

                _consoleWriter.FinishProgress();
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }
        protected async Task DownloadFile(Guid jobId, Guid tableId, EcfFileMapping file)
        {
            _consoleWriter.StartProgress($"Download file {file.Name}");
            try
            {
                using var response = await _httpClient.GetAsync(
                    $"exports/jobs/{jobId}/tables/{tableId}",
                    HttpCompletionOption.ResponseHeadersRead,
                    _config,
                    _cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    await ThrowExportException($"Download file failed.", response);
                }

                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    using var ecfFile = await response.Content.ReadAsStreamAsync(_cancellationToken);

                    using var ecfFileStream = new FileStream(Path.Combine(GetEcfFolderName(), file.GetNameWithExtension()),
                        FileMode.Create, FileAccess.ReadWrite, FileShare.None);

                    await ecfFile.CopyToAsync(ecfFileStream, _cancellationToken);
                }

                _consoleWriter.FinishProgress();
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        protected async Task Extract(Guid jobId)
        {
            var successfullExtract = false;

            using var finishEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

            var connection = new HubConnectionBuilder()
                .WithUrl(new Uri(_config.GetSyncHubUrlWithTrailingSlash(), "exportHub"))
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

            connection.On<Guid, string>("ExtractTableBegin", (ecfJobId, ecfTableName) =>
            {
                if (ecfJobId == jobId)
                {
                    _consoleWriter.StartProgress($"Extracting {ecfTableName}...");
                }
            });

            connection.On<Guid, string, int>("ExtractTableEnd", (ecfJobId, ecfTableName, ecfRecordCounter) =>
            {
                if (ecfJobId == jobId)
                {
                    _consoleWriter.FinishProgress(ecfRecordCounter);
                }
            });

            connection.On<Guid>("ExtractBegin", (ecfJobId) =>
            {
                if (ecfJobId == jobId)
                {
                    _consoleWriter.Message("Start extracting...");
                }
            });

            connection.On<Guid, int, int>("ExtractSuccessfull", (ecfJobId, ecfTableCounter, ecfRecordCounter) =>
            {
                if (ecfJobId == jobId)
                {
                    _consoleWriter.Success($"{ecfTableCounter} table(s) extracted");
                    successfullExtract = true;
                    finishEvent.Set();
                }
            });

            connection.On<Guid, int, int, string>("ExtractFailed", (ecfJobId, ecfTableCounter, ecfRecordCounter, errorMessage) =>
            {
                if (ecfJobId == jobId)
                {
                    _consoleWriter.CancelProgress();
                    _consoleWriter.Error($"Extracting failed. Reason: {errorMessage}");
                    finishEvent.Set();
                }
            });

            connection.On<Guid, string, int>("RecordExtracted", (ecfJobId, ecfTableName, ecfRecordCounter) =>
            {
                if (ecfJobId == jobId)
                {
                    _consoleWriter.ContinueProgress(ecfRecordCounter);
                }
            });

            var response = await _httpClient.PostAsync($"exports/jobQueue/jobs/{jobId}",
                _config,
                _cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await ThrowExportException("Start extracting failed.", response);
            }

            WaitHandle.WaitAny(
            [
                finishEvent, _cancellationEvent
            ]);

            if (!successfullExtract)
            {
                await ThrowExportException("Extracting failed", response);
            }

            await connection.StopAsync(_cancellationToken);
        }

        private static async Task ThrowExportException(string message, HttpResponseMessage serverResponse)
        {
            throw new ExportException(message, serverResponse.StatusCode, await serverResponse.Content.ReadAsStringAsync());
        }
    }
}
