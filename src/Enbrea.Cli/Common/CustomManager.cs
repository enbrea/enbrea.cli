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
using System.IO;
using System.Threading;

namespace Enbrea.Cli
{
    /// <summary>
    /// Abstract manager for dealing with ECF files from or to Enbrea
    /// </summary>
    public abstract class CustomManager : EcfCustomManager
    {
        private protected readonly Configuration _config;
        private protected readonly IEnbreaHttpClient _httpClient;
        private protected readonly EventWaitHandle _cancellationEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomManager"/> class.
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <param name="dataFolderName">Path to data folder for ecf files, log files etc.</param>
        /// <param name="consoleWriter">Console writer</param>
        /// <param name="cancellationEvent">Cancellation event for SignalR</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public CustomManager(Configuration config, string dataFolderName, ConsoleWriter consoleWriter, EventWaitHandle cancellationEvent, CancellationToken cancellationToken)
            : base(dataFolderName, consoleWriter, cancellationToken)
        {
            _config = config;
            _cancellationEvent = cancellationEvent;
            _httpClient = EnbreaHttpClientFactory.CreateClient();
            _httpClient.PrepareAuthentication(_config);
        }

        public string GetCtxFolderName()
        {
            return Path.Combine(_dataFolderName, "ctx");
        }

        public string GetCtxFileName()
        {
            return Path.Combine(GetCtxFolderName(), "previous-context.json");
        }
    }
}
