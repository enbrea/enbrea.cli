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

using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli
{
    public interface IEnbreaHttpClient
    {
        Task<HttpResponseMessage> DeleteAsync(string requestUri, Configuration config, CancellationToken cancellationToken);

        Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption, Configuration config, CancellationToken cancellationToken);

        Task<Stream> GetStreamAsync(string requestUri, Configuration config);

        Task<HttpResponseMessage> PostAsync(string requestUri, Configuration config, CancellationToken cancellationToken);

        Task<HttpResponseMessage> PostAsync(string requestUri, Configuration config, object content, CancellationToken cancellationToken);

        Task<HttpResponseMessage> PostAsync(string requestUri, Configuration config, StreamContent content, CancellationToken cancellationToken);

        void PrepareAuthentication(Configuration config);
    }
}
