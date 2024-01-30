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
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli
{
    public class SchoolTermManager
    {
        private protected readonly Configuration _config;
        private protected readonly IEnbreaHttpClient _httpClient;
        protected readonly ConsoleWriter _consoleWriter;
        private readonly CancellationToken _cancellationToken;

        public SchoolTermManager(Configuration config, CancellationToken cancellationToken)
        {
            _config = config;
            _cancellationToken = cancellationToken;
            _consoleWriter = ConsoleWriterFactory.CreateConsoleWriter(ProgressUnit.Count);
            _httpClient = EnbreaHttpClientFactory.CreateClient();
            _httpClient.PrepareAuthentication(_config);
        }

        public async Task ListSchoolTerms()
        {
            _consoleWriter.StartProgress("List school terms...");
            try
            {
                var response = await _httpClient.GetAsync("schoolTerms",
                    HttpCompletionOption.ResponseHeadersRead,
                    _config,
                    _cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var shoolTermCounter = 0;
                    var schoolTermList = await JsonSerializer.DeserializeAsync<List<SchoolTermDto>>(response.Content.ReadAsStream());

                    _consoleWriter.FinishProgress();
                   
                    foreach (var schoolTerm in schoolTermList)
                    {
                        _consoleWriter.Message($"{++shoolTermCounter}. [{schoolTerm.ValidFrom:yyyy-mm-dd} to {schoolTerm.ValidTo:yyyy-mm-dd}] {schoolTerm.Code}");
                    }

                    _consoleWriter.Success($"{shoolTermCounter} school term(s) found");
                }
                else
                { 
                    await ThrowSnapshotException("List school terms failed", response);
                }
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }

        private async Task ThrowSnapshotException(string message, HttpResponseMessage serverResponse)
        {
            throw new SchoolTermException(message, serverResponse.StatusCode, await serverResponse.Content.ReadAsStringAsync());
        }
    }
}
