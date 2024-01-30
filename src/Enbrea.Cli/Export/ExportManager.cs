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
using Microsoft.AspNetCore.SignalR.Client;
using System;
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
                await CleanUp();

                var jobId = await CreateJob(_config);

                await Prepare(jobId);
                await Extract(jobId);
                await Download(jobId, jobId);
            }
            else
            {
                throw new ImportException($"No export tables in configuration found.");
            }
        }

        private async Task CleanUp()
        {
            Console.WriteLine();
            Console.WriteLine($"[CleanUp] Delete successfull and failed jobs...");

            var response = await _httpClient.DeleteAsync("exports/jobs", _config, _cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ImportException($"Delete jobs failed. Server responded with status code {response.StatusCode}");
            }

            Console.WriteLine($"[CleanUp] Jobs deleted");
        }

        private async Task<Guid> CreateJob(Configuration config)
        {
            Console.WriteLine();

            Console.WriteLine($"[Job] Create...");

            var response = await _httpClient.PostAsync("exports/jobs", _config, 
                new CreateExportJobOptions(config.SchoolTerm, config.ApplicationProcess, _provider), 
                _cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ExportException($"Create job failed. Server responded with status code {response.StatusCode}");
            }

            //var responseBody = await response.Content.ReadAsJsonAsync<Reference>();
            var responseBody = await response.Content.ReadFromJsonAsync<Reference>();
            var jobId = responseBody.Id;

            Console.WriteLine($"[Job] Storage {jobId} created");

            return jobId;
        }

        private async Task<Guid> CreateTable(Guid jobId, EcfFileMapping file)
        {
            Console.WriteLine();
            Console.WriteLine($"[Table] Create {file.Name}...");

            var response = await _httpClient.PostAsync($"exports/jobs/{jobId}/tables", _config, 
                new CreateExportTableOptions(file.Name, file.KeyHeaders), 
                _cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ImportException($"Create table failed. Server responded with status code {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadFromJsonAsync<Reference>();
            var tableId = responseBody.Id;

            Console.WriteLine($"[Table] Table {file.Name} created");

            return tableId;
        }

        private async Task Prepare(Guid jobId)
        {
            Console.WriteLine();
            Console.WriteLine($"[Preparing] Create tables for extraction...");

            var tableCount = 0;

            foreach (var file in _config.EcfMapping.Files)
            {
                var tableId = await CreateTable(jobId, file);

                tableCount++;
            }

            Console.WriteLine($"[Preparing] {tableCount} tables for extraction created");
        }

        private async Task Download(Guid jobId, Guid tableId)
        {
            Console.WriteLine();
            Console.WriteLine($"[Downloading] Download files...");

            Directory.CreateDirectory(_dataFolderName);

            var fileCount = 0;

            foreach (var file in _config.EcfMapping.Files)
            {
                Console.WriteLine($"[Downloading] File {file.Name}...");

                await DownloadFile(jobId, tableId, file);

                fileCount++;
            }

            Console.WriteLine($"[Downloading] {fileCount} files downloaded");
        }

        private async Task DownloadFile(Guid jobId, Guid tableId, EcfFileMapping file)
        {
            using var response = await _httpClient.GetAsync(
                $"exports/jobs/{jobId}/tables/{tableId}",
                HttpCompletionOption.ResponseHeadersRead,
                _config,
                _cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ExportException($"Create job failed. Server responded with status code {response.StatusCode}");
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return;
            }

            using var ecfFile = await response.Content.ReadAsStreamAsync();

            using var ecfFileStream = new FileStream(Path.Combine(_dataFolderName, file.GetNameWithExtension()),
                FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            await ecfFile.CopyToAsync(ecfFileStream, _cancellationToken);
        }

        private async Task Extract(Guid jobId)
        {
            using var finishEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

            var connection = new HubConnectionBuilder()
                .WithUrl(new Uri(_config.GetSyncHubUrlWithTrailingSlash(), "exportHub"))
                .Build();

            connection.Closed += (error) =>
            {
                if (error != null)
                {
                    Console.WriteLine();
                    Console.WriteLine($"[Server lost] {error.Message}");
                    _cancellationEvent.Set();
                }
                return Task.CompletedTask;
            };

            await connection.StartAsync(_cancellationToken);

            connection.On<Guid, string>("ExtractTableBegin", (ecfJobId, ecfTableName) =>
            {
                if (ecfJobId == jobId)
                {
                    Console.WriteLine($"[Extracting] [{ecfTableName}] Start...");
                }
            });

            connection.On<Guid, string, int>("ExtractTableEnd", (ecfJobId, ecfTableName, ecfRecordCounter) =>
            {
                if (ecfJobId == jobId)
                {
                    Console.WriteLine($"[Extracting] [{ecfTableName}] {ecfRecordCounter} record(s) extracted");
                }
            });

            connection.On<Guid>("ExtractBegin", (ecfJobId) =>
            {
                if (ecfJobId == jobId)
                {
                    Console.WriteLine();
                    Console.WriteLine("[Extracting] Start...");
                }
            });

            connection.On<Guid, int, int>("ExtractSuccessfull", (ecfJobId, ecfTableCounter, ecfRecordCounter) =>
            {
                if (ecfJobId == jobId)
                {
                    Console.WriteLine($"[Extracting] {ecfTableCounter} table(s) and {ecfRecordCounter} record(s) extracted");
                    finishEvent.Set();
                }
            });

            connection.On<Guid, int, int, string>("ExtractFailed", (ecfJobId, ecfTableCounter, ecfRecordCounter, errorMessage) =>
            {
                if (ecfJobId == jobId)
                {
                    Console.WriteLine();
                    Console.WriteLine($"[Error] Extracting failed. Only {ecfTableCounter} table(s) and {ecfRecordCounter} record(s) extracted");
                    Console.WriteLine($"[Error] Reason: {errorMessage}");
                    finishEvent.Set();
                }
            });

            var response = await _httpClient.PostAsync($"exports/jobQueue/jobs/{jobId}",
                _config,  
                _cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ExportException($"Start extracting failed. Server responded with status code {response.StatusCode}.");
            }

            WaitHandle.WaitAny(new[]
            {
                    finishEvent, _cancellationEvent
                });

            await connection.StopAsync(_cancellationToken);
        }
    }
}
