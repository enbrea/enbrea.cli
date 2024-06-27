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
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli
{
    public class SnapshotManager
    {
        private protected readonly Configuration _config;
        private protected readonly IEnbreaHttpClient _httpClient;
        protected readonly ConsoleWriter _consoleWriter;
        private readonly CancellationToken _cancellationToken;

        public SnapshotManager(Configuration config, CancellationToken cancellationToken)
        {
            _config = config;
            _cancellationToken = cancellationToken;
            _consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
            _httpClient = EnbreaHttpClientFactory.CreateClient();
            _httpClient.PrepareAuthentication(_config);
        }

        public async Task BackupOffline(FileInfo outFile)
        {
            _consoleWriter.StartProgress("Create offline database backup...");
            try
            {
                outFile = outFile.AddDefaultExtension("backup");

                if (!File.Exists(outFile.FullName))
                {
                    var response = await _httpClient.PostAsync("snapshots/download",
                        _config, 
                        _cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        await ThrowSnapshotException("Create database backup failed", response);
                    }

                    Directory.CreateDirectory(outFile.Directory.FullName);

                    using var outFileStream = new FileStream(outFile.FullName, FileMode.Create);

                    await response.Content.CopyToAsync(outFileStream, _cancellationToken);

                    _consoleWriter.FinishProgress();
                    _consoleWriter.Success($"Database backup successfully downloaded to {outFile.FullName}");
                }
                else
                {
                    throw new FileNotFoundException($"File \"{outFile.FullName}\" already exists.");
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public async Task CreateSnapshot()
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

                var s = response.Content.ReadAsStream();

                var responseBody = await JsonSerializer.DeserializeAsync<SnapshotsListDto>(s);

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

        public async Task DeleteSnapshot(Guid uid)
        {
            _consoleWriter.StartProgress("Delete database snapshot...");
            try
            {

                var response = await _httpClient.DeleteAsync($"snapshots/{uid}",
                    _config, 
                    _cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    await ThrowSnapshotException("Delete database snapshot failed", response);
                }

                _consoleWriter.FinishProgress();
                _consoleWriter.Success($"Database snapshot with uid {uid} successfully deleted");
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }
        public async Task ListSnapshots()
        {
            _consoleWriter.StartProgress("List database snapshots...");
            try
            {
                var response = await _httpClient.GetAsync("snapshots",
                    HttpCompletionOption.ResponseHeadersRead,
                    _config,
                    _cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    await ThrowSnapshotException("List database snapshots failed", response);
                }

                var responseBody = await JsonSerializer.DeserializeAsync<SnapshotsListDto>(response.Content.ReadAsStream());

                _consoleWriter.FinishProgress();

                var snapshotCounter = 0;
                foreach (var e in responseBody.items)
                {
                    _consoleWriter.Message($"Snapshot ID {e.uid}, Timestamp {e.timestamp.ToString("yyyy-MM-dd hh:mm:ss")}");
                    snapshotCounter++;
                }

                _consoleWriter.Success($"{snapshotCounter} database snapshot(s) found");
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        public async Task RestoreSnapshot(Guid uid)
        {
            _consoleWriter.StartProgress("Restore database snapshot...");
            try
            {
                var response = await _httpClient.PostAsync($"snapshots/restore({uid})",
                    _config, 
                    _cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    await ThrowSnapshotException("Restore database snapshot failed", response);
                }

                _consoleWriter.FinishProgress();
                _consoleWriter.Success($"Database snapshot with uid {uid} successfully restored");
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        private async Task ThrowSnapshotException(string message, HttpResponseMessage serverResponse)
        {
            throw new SnapshotException(message, serverResponse.StatusCode, await serverResponse.Content.ReadAsStringAsync());
        }
    }
}
