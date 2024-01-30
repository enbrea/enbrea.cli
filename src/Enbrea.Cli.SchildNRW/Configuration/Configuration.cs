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

using Enbrea.SchildNRW.Db;
using System.Text.Json.Serialization;

namespace Enbrea.Cli.SchildNRW
{
    /// <summary>
    /// Configuration for SchildNRW
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// MS SQL Server or MySQL database connection for direct SchildNRW access
        /// </summary>
        [JsonPropertyOrder(1)]
        public string DatabaseConnection { get; set; } = "server=localhost;port=3306;database=schild;uid=myUsername;pwd=myPassword";

        /// <summary>
        /// Data provider for SchildNRW (either MS SQL Server or MySQL)
        /// </summary>
        [JsonPropertyOrder(0)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SchildNRWDbProvider DataProvider { get; set; } = SchildNRWDbProvider.MySql;

        /// <summary>
        /// School term (Schulhalbjahr)
        /// </summary>
        [JsonPropertyOrder(3)]
        public short SchoolTerm { get; set; } = 1;

        /// <summary>
        /// School year (Schuljahr)
        /// </summary>
        [JsonPropertyOrder(2)]
        public short SchoolYear { get; set; } = 2023;

        /// <summary>
        /// Target folder for ECF files, log files etc.
        /// </summary>
        [JsonPropertyOrder(4)]
        public string TargetFolder { get; set; } = ".\\schildnrw\\export";
    }
}
