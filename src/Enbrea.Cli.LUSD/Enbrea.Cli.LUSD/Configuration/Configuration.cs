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

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Enbrea.Cli.LUSD
{
    /// <summary>
    /// Configuration for LUSD
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Name of the Excel file (XLSX)
        /// </summary>
        [JsonPropertyOrder(1)]
        public string DataFile { get; set; } = ".\\lusd\\beispiel.xlsx";

        /// <summary>
        /// Target folder for ECF file generation
        /// </summary>
        [JsonPropertyOrder(9)]
        public string TargetFolder { get; set; } = ".\\lusd\\export";

        /// <summary>
        /// Number of the first Excel sheet row for export from XLSX file
        /// </summary>
        [JsonPropertyOrder(3)]
        public int? XlsxFirstRowNumber { get; set; } = 2;

        /// <summary>
        /// Number of the last Excel sheet row for export from XLSX file
        /// </summary>
        [JsonPropertyOrder(4)]
        public int? XlsxLastRowNumber { get; set; }

        /// <summary>
        /// Mapping of XLSX file headers to ECF file headers
        /// </summary>
        [JsonPropertyOrder(5)]
        public ICollection<XlsxMapping> XlsxMappings { get; set; } =
        [
            new() { FromHeader = "A", ToHeader = "Vorname" },
            new() { FromHeader = "B", ToHeader = "Nachname" },
            new() { FromHeader = "C", ToHeader = "Geburtsdatum" },
            new() { FromHeader = "D", ToHeader = "Klasse" },
            new() { FromHeader = "E", ToHeader = "Fach" },
            new() { FromHeader = "F", ToHeader = "Lehrer" }
        ];

        /// <summary>
        /// Name of the Excel sheet for export from XLSX file
        /// </summary>
        [JsonPropertyOrder(2)]
        public string XlsxSheetName { get; set; }

        public string GetXlsxColumnName(string ecfHeaderName)
        {
            var mapping = XlsxMappings.FirstOrDefault(x => x.ToHeader == ecfHeaderName);
            if (mapping != null)
            {
                return mapping.FromHeader;
            }
            else
            {
                return null;
            }
        }
    }
}
