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

namespace Enbrea.Cli.Excel
{
    /// <summary>
    /// Configuration for Excel/CSV
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Mapping of CSV file headers to ECF file headers
        /// </summary>
        [JsonPropertyOrder(8)]
        public ICollection<CsvMapping> CsvMappings { get; set; } = new List<CsvMapping>()
        {
            new() { FromHeader = "VNAME", ToHeader = "Vorname" },
            new() { FromHeader = "NNAME", ToHeader = "Nachname" },
            new() { FromHeader = "KLASSE", ToHeader = "Klasse" },
        };

        /// <summary>
        /// Quote char for export from CSV file
        /// </summary>
        [JsonPropertyOrder(6)]
        public char CsvQuote { get; set; } = '"';

        /// <summary>
        /// Separator char for export from CSV file
        /// </summary>
        [JsonPropertyOrder(7)]
        public char CsvSeparator { get; set; } = ';';

        /// <summary>
        /// Name of the Excel file (XLSX or CSV)
        /// </summary>
        [JsonPropertyOrder(1)]
        public string DataFile { get; set; } = ".\\excel\\beispiel.xlsx";

        /// <summary>
        /// Data provider for Excel (either XLSX or CSV)
        /// </summary>
        [JsonPropertyOrder(0)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DataProvider DataProvider { get; set; } = DataProvider.Xlsx;

        /// <summary>
        /// Target folder for ECF file generation
        /// </summary>
        [JsonPropertyOrder(9)]
        public string TargetFolder { get; set; } = ".\\excel\\export";

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
        public ICollection<XlsxMapping> XlsxMappings { get; set; } = new List<XlsxMapping>() 
        {
            new() { FromHeader = "A", ToHeader = "Vorname" },
            new() { FromHeader = "B", ToHeader = "Nachname" },
            new() { FromHeader = "C", ToHeader = "Klasse" },
        };

        /// <summary>
        /// Name of the Excel sheet for export from XLSX file
        /// </summary>
        [JsonPropertyOrder(2)]
        public string XlsxSheetName { get; set; }

        public string GetCsvHeaderName(string ecfHeaderName)
        {
            var mapping = CsvMappings.FirstOrDefault(x => x.ToHeader == ecfHeaderName);
            if (mapping != null)
            {
                return mapping.FromHeader;
            }
            else
            {
                return ecfHeaderName;
            }
        }

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
