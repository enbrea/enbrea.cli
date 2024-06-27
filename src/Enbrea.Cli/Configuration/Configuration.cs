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
using System;
using System.Text.Json.Serialization;

namespace Enbrea.Cli
{
    /// <summary>
    /// Enbrea Cli configuration
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Personal Access Token to be used instead of Enbrea identity call with UserName/Password
        /// </summary>
        [JsonPropertyOrder(1)]
        public string AccessToken { get; set; } = "MyAccessToken";

        /// <summary>
        /// Code of the Enbrea application process (for exporting)
        /// </summary>
        [JsonPropertyOrder(3)]
        public string ApplicationProcess { get; set; } 

        /// <summary>
        /// Sub configuration for BBS-Planung
        /// </summary>
        [JsonPropertyOrder(10)]
        public BbsPlanung.Configuration BbsPlanung { get; set; } = new();

        /// <summary>
        /// Sub configuration for DaNiS
        /// </summary>
        [JsonPropertyOrder(13)]
        public Danis.Configuration Danis { get; set; } = new();

        /// <summary>
        /// Sub configuration for DAVINCI
        /// </summary>
        [JsonPropertyOrder(7)]
        public DaVinci.Configuration DaVinci { get; set; } = new();

        /// <summary>
        /// Sub configuration for BBS-Planung
        /// </summary>
        [JsonPropertyOrder(99)]
        public EcfMapping EcfMapping { get; set; } = new();

        /// <summary>
        /// Sub configuration for edoo.sys
        /// </summary>
        [JsonPropertyOrder(11)]
        public Edoosys.Configuration Edoosys { get; set; } = new();

        /// <summary>
        /// Sub configuration for Excel/CSV
        /// </summary>
        [JsonPropertyOrder(14)]
        public Excel.Configuration Excel { get; set; } = new();

        /// <summary>
        /// Sub configuration for MAGELLAN
        /// </summary>
        [JsonPropertyOrder(9)]
        public Magellan.Configuration Magellan { get; set; } = new();

        /// <summary>
        /// Sub configuration for SchildNRW
        /// </summary>
        [JsonPropertyOrder(12)]
        public SchildNRW.Configuration SchildNRW { get; set; } = new();

        /// <summary>
        /// Code of the Enbrea school term (for importing or exporting)
        /// </summary>
        [JsonPropertyOrder(2)]
        public string SchoolTerm { get; set; } = "MySchoolTerm";

        /// <summary>
        /// Sub configuration for Untis
        /// </summary>
        [JsonPropertyOrder(8)]
        public Untis.Configuration Untis { get; set; } = new();

        /// <summary>
        /// URL to Enbrea
        /// </summary>
        [JsonPropertyOrder(0)]
        public Uri Url { get; set; } = new Uri("https://enbrea.cloud/myschool");

        /// <summary>
        /// Calculates the correct URL for the Enbrea SyncHub
        /// </summary>
        /// <returns>A URL</returns>
        public Uri GetSyncHubUrlWithTrailingSlash()
        {
            if (Url.Port != 443)
            {
                return Url.AddTrailingSlash();
            }
            else
            {
                return new Uri(Url.AddTrailingSlash(), "SyncHub").AddTrailingSlash();
            }
        }
    }
}
