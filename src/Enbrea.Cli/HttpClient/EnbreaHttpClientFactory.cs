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

using Microsoft.Extensions.DependencyInjection;
using Polly;
using System;
using System.Net;   
using System.Net.Http;

namespace Enbrea.Cli
{
    public static class EnbreaHttpClientFactory
    {
        public static IEnbreaHttpClient CreateClient()
        {
            // Create dependency injection container
            var serviceCollection = new ServiceCollection();

            // Register Enbrea Http Client
            serviceCollection.AddHttpClient<IEnbreaHttpClient, EnbreaHttpClient>(c =>
            {
                c.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue(AssemblyInfo.GetAgentName(), AssemblyInfo.GetVersion()));
            })

            // Configure HTTP client for automatic retry
            .AddPolicyHandler(Policy<HttpResponseMessage>.Handle<HttpRequestException>()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.RequestTimeout)
                .OrResult(msg => msg.StatusCode == HttpStatusCode.ServiceUnavailable)
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            // Create IEnbreaClient implementation
            var services = serviceCollection.BuildServiceProvider();

            // Return back IEnbreaClient implementation
            return services.GetRequiredService<IEnbreaHttpClient>();
        }
    }
}
