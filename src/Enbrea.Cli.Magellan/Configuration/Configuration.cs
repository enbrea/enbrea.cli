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

namespace Enbrea.Cli.Magellan
{
    /// <summary>
    /// Configuraton for MAGELLAN 
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Firebird database connection for MAGELLAN access
        /// </summary>
        [JsonPropertyOrder(0)]
        public string DatabaseConnection { get; set; } = "DataSource=localhost;Database='C:\\Users\\Public\\Documents\\Stueber Systems\\MAGELLAN 11\\Datenbank\\Magellan11.fdb';Charset=UTF8;User=myUsername;Password=myPassword";

        /// <summary>
        /// ID of the current MAGELLAN school term (Zeitraum)
        /// </summary>
        [JsonPropertyOrder(2)]
        public int SchoolTermId { get; set; } = 42;

        /// <summary>
        /// Source folder for ecf files, log files etc.
        /// </summary>
        [JsonPropertyOrder(4)]
        public string SourceFolder { get; set; } = ".\\magellan\\import";

        /// <summary>
        /// Table mapping for export and import
        /// </summary>
        [JsonPropertyOrder(5)]
        public ProviderEcfMapping EcfMapping { get; set; }

        /// <summary>
        /// Target folder for ecf files, log files etc.
        /// </summary>
        [JsonPropertyOrder(3)]
        public string TargetFolder { get; set; } = ".\\magellan\\export";

        /// <summary>
        /// ID of the current MAGELLAN tenant (Mandant)
        /// </summary>
        [JsonPropertyOrder(1)]
        public int TenantId { get; set; } = 1;
    }
}
