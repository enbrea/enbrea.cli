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

namespace Enbrea.Cli.Untis
{
    /// <summary>
    /// Configuration for Untis
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Untis file for file only access
        /// </summary>
        [JsonPropertyOrder(1)]
        public string DataFile { get; set; } = ".\\untis\\example.gpn";

        /// <summary>
        /// Data provider for Untis (file or server)
        /// </summary>
        [JsonPropertyOrder(0)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DataProvider DataProvider { get; set; } = DataProvider.File;

        /// <summary>
        /// Export folder for GPU files and XML file export from Untis
        /// </summary>
        [JsonPropertyOrder(7)]
        public string ExportFolder { get; set; } = ".\\untis\\export\\gpu";

        /// <summary>
        /// Exported Gpu files from Untis encoded as UTF8?
        /// </summary>
        [JsonPropertyOrder(10)]
        public bool ExportFilesAsUtf8 { get; set; } = false;

        /// <summary>
        /// Quote char for GPU files export from Untis
        /// </summary>
        [JsonPropertyOrder(8)]
        public char ExportQuote { get; set; } = '"';

        /// <summary>
        /// Separator char for GPU files export from Untis
        /// </summary>
        [JsonPropertyOrder(9)]
        public char ExportSeparator { get; set; } = ',';

        /// <summary>
        /// Password for Untis server access
        /// </summary>
        [JsonPropertyOrder(5)]
        public string ServerPassword { get; set; } = "myPassword";

        /// <summary>
        /// School number for Untis server access
        /// </summary>
        [JsonPropertyOrder(2)]
        public string ServerSchoolNo { get; set; } = "12345";

        /// <summary>
        /// School year for Untis server access
        /// </summary>
        [JsonPropertyOrder(3)]
        public string ServerSchoolYear { get; set; } = "2023-2024";

        /// <summary>
        /// User name for Untis server access
        /// </summary>
        [JsonPropertyOrder(4)]
        public string ServerUserName { get; set; } = "myUsername";

        /// <summary>
        /// Target folder for ECF files, log files etc.
        /// </summary>
        [JsonPropertyOrder(11)]
        public string TargetFolder { get; set; } = ".\\untis\\export";
    }
}
