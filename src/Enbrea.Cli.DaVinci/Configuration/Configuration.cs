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

namespace Enbrea.Cli.DaVinci
{
    /// <summary>
    /// Configuration for DAVINCI
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// In case of file access, the file path
        /// </summary>
        [JsonPropertyOrder(1)]
        public string DataFile { get; set; } = ".\\davinci\\example.davinci";

        /// <summary>
        /// The DAVINCI data provider (file or server access)
        /// </summary>
        [JsonPropertyOrder(0)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DataProvider DataProvider { get; set; } = DataProvider.File;

        /// <summary>
        /// Mapping for export to ECF
        /// </summary>
        [JsonPropertyOrder(9)]
        public ProviderEcfMapping EcfMapping { get; set; }

        /// <summary>
        /// In case of server access, the server file id
        /// </summary>
        [JsonPropertyOrder(6)]
        public Guid? ServerFileId { get; set; } = Guid.Empty;

        /// <summary>
        /// In case of server access, the server name
        /// </summary>
        [JsonPropertyOrder(2)]
        public string ServerName { get; set; } = "localhost";

        /// <summary>
        /// In case of server access, the user password
        /// </summary>
        [JsonPropertyOrder(5)]
        public string ServerPassword { get; set; } = "myPassword";

        /// <summary>
        /// In case of server access, the TCP server port
        /// </summary>
        [JsonPropertyOrder(3)]
        public uint ServerPort { get; set; } = 8100;

        /// <summary>
        /// In case of server access, the user name
        /// </summary>
        [JsonPropertyOrder(4)]
        public string ServerUserName { get; set; } = "myUsername";

        /// <summary>
        /// Source folder for ECF files, log files etc.
        /// </summary>
        [JsonPropertyOrder(8)]
        public string SourceFolder { get; set; } = ".\\davinci\\import";

        /// <summary>
        /// Target folder for ECF files, log files etc.
        /// </summary>
        [JsonPropertyOrder(7)]
        public string TargetFolder { get; set; } = ".\\davinci\\export";
    }
}
