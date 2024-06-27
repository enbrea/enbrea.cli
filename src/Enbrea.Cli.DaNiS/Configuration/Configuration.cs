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

namespace Enbrea.Cli.Danis
{
    /// <summary>
    /// Configuration for DaNiS
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// PostgreSQL database connection for direct DaNiS access
        /// </summary>
        [JsonPropertyOrder(1)]
        public string DatabaseConnection { get; set; } = "Server=127.0.0.1;Port=5432;Database=danis;User Id=myUsername;Password=myPassword;";

        /// <summary>
        /// Mapping for export to ECF
        /// </summary>
        [JsonPropertyOrder(4)]
        public ProviderEcfMapping EcfMapping { get; set; }

        /// <summary>
        /// Target folder for ECF file generation
        /// </summary>
        [JsonPropertyOrder(3)]
        public string TargetFolder { get; set; } = ".\\danis\\export";

        /// <summary>
        /// Year number (Jahr, in dem das Schuljahr beginnt)
        /// </summary>
        [JsonPropertyOrder(2)]
        public int Year { get; set; } = 2023;
    }
}
