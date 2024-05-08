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

using Enbrea.Cli.Common;
using System.Text.Json.Serialization;

namespace Enbrea.Cli.Edoosys
{
    /// <summary>
    /// Configuration for edoo.sys
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Folder for CSV export from edoo.sys
        /// </summary>
        [JsonPropertyOrder(2)]
        public string CsvExportFile { get; set; } = ".\\edoosys\\export\\csv\\data.csv";

        /// <summary>
        /// Quote char for CSV export from edoo.sys
        /// </summary>
        [JsonPropertyOrder(3)]
        public char CsvExportQuote { get; set; } = '"';

        /// <summary>
        /// Separator char for CSV export from edoo.sys
        /// </summary>
        [JsonPropertyOrder(4)]
        public char CsvExportSeparator { get; set; } = ';';

        /// <summary>
        /// PostgreSQL database connection for direct edoo.sys access
        /// </summary>
        [JsonPropertyOrder(1)]
        public string DatabaseConnection { get; set; } = "Server=127.0.0.1;Port=5432;Database=asv;User Id=myUsername;Password=myPassword;";

        /// <summary>
        /// Data provider for edoo.sys (either CSV export or direct database access)
        /// </summary>
        [JsonPropertyOrder(0)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DataProvider DataProvider { get; set; } = DataProvider.Postgres;

        /// <summary>
        /// Mapping for export to ECF
        /// </summary>
        [JsonPropertyOrder(9)]
        public ProviderEcfMapping EcfMapping { get; set; }

        /// <summary>
        /// Do not process edoo.sys school class groups (Klassengruppen)
        /// </summary>
        [JsonPropertyOrder(7)]
        public bool NoSchoolClassGroups { get; set; } = true;

        /// <summary>
        /// School number (Schulnummer)
        /// </summary>
        [JsonPropertyOrder(5)]
        public string SchoolNo { get; set; } = "12345";

        /// <summary>
        /// School year code (Kürzel des Schuljahres)
        /// </summary>
        [JsonPropertyOrder(6)]
        public string SchoolYearCode { get; set; } = "2023/24";

        /// <summary>
        /// Target folder for ECF file generation
        /// </summary>
        [JsonPropertyOrder(8)]
        public string TargetFolder { get; set; } = ".\\edoosys\\export";
    }
}
