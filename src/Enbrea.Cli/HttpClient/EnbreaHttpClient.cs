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

using IdentityModel.Client;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli
{
    public class EnbreaHttpClient : IEnbreaHttpClient
    {
        private readonly HttpClient _client;

        public EnbreaHttpClient(HttpClient client)
        {
            _client = client;
        }

        public Task<HttpResponseMessage> DeleteAsync(string relativeRequestUri, Configuration config, CancellationToken cancellationToken)
        {
            return _client.DeleteAsync(new Uri(config.GetSyncHubUrlWithTrailingSlash(), relativeRequestUri), cancellationToken);
        }

        public Task<HttpResponseMessage> GetAsync(string relativeRequestUri, HttpCompletionOption completionOption, Configuration config, CancellationToken cancellationToken)
        {
            return _client.GetAsync(new Uri(config.GetSyncHubUrlWithTrailingSlash(), relativeRequestUri), completionOption, cancellationToken);
        }

        public Task<Stream> GetStreamAsync(string relativeRequestUri, Configuration config)
        {
            return _client.GetStreamAsync(new Uri(config.GetSyncHubUrlWithTrailingSlash(), relativeRequestUri));
        }

        public Task<HttpResponseMessage> PostAsync(string relativeRequestUri, Configuration config, CancellationToken cancellationToken)
        {
            return _client.PostAsync(new Uri(config.GetSyncHubUrlWithTrailingSlash(), relativeRequestUri), null, cancellationToken);
        }

        public Task<HttpResponseMessage> PostAsync(string relativeRequestUri, Configuration config, object content, CancellationToken cancellationToken)
        {
            return _client.PostAsync(new Uri(config.GetSyncHubUrlWithTrailingSlash(), relativeRequestUri), new JsonContent(content), cancellationToken);
        }

        public Task<HttpResponseMessage> PostAsync(string relativeRequestUri, Configuration config, StreamContent content, CancellationToken cancellationToken)
        {
            return _client.PostAsync(new Uri(config.GetSyncHubUrlWithTrailingSlash(), relativeRequestUri), content, cancellationToken);
        }

        public void PrepareAuthentication(Configuration config)
        {
            if (!string.IsNullOrWhiteSpace(config.AccessToken))
            {
                _client.SetToken("PAT", config.AccessToken);
            }
            else
            {
                throw new Exception("No Access Token provided.");
            }
        }
    }
}