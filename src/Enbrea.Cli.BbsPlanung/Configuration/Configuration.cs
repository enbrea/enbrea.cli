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

using System.Text.Json.Serialization;

namespace Enbrea.Cli.BbsPlanung
{
    /// <summary>
    /// Configuration for BBS-Planung
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// MS access database connection for direct BBS-Planung access
        /// </summary>
        [JsonPropertyOrder(0)]
        public string DatabaseConnection { get; set; } = "Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=c:\\Access\\s_daten.mdb;SystemDB=c:\\Access\\system.mdw;Uid=myUsername;Pwd=myPassword";

        /// <summary>
        /// School number (Schulnummer)
        /// </summary>
        [JsonPropertyOrder(1)]
        public int SchoolNo { get; set; } = 12345;

        /// <summary>
        /// Target folder for ECF files, log files etc.
        /// </summary>
        [JsonPropertyOrder(2)]
        public string TargetFolder { get; set; } = ".\\bbsplanung\\export";
    }
}
