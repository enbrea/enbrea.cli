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
using System.Text.Json.Serialization;

namespace Enbrea.Cli.SaxSVS
{
    /// <summary>
    /// Configuration for SaxSVS
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Name of the exported SaxSVS XML file
        /// </summary>
        [JsonPropertyOrder(1)]
        public string DataFile { get; set; } = ".\\excel\\saxsvs.xml";

        /// <summary>
        /// Mapping for export to ECF
        /// </summary>
        [JsonPropertyOrder(4)]
        public ProviderEcfMapping EcfMapping { get; set; }

        /// <summary>
        /// School year (Schuljahr)
        /// </summary>
        [JsonPropertyOrder(2)]
        public string SchoolYear { get; set; } = "2025/2026";

        /// <summary>
        /// Target folder for ECF files, log files etc.
        /// </summary>
        [JsonPropertyOrder(3)]
        public string TargetFolder { get; set; } = ".\\saxsvs\\export";
    }
}
