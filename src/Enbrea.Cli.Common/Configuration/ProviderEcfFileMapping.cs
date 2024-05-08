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

namespace Enbrea.Cli.Common
{
    /// <summary>
    /// File mapping for an ECF file exported by an ECF provider
    /// </summary>
    public class ProviderEcfFileMapping
    {
        /// <summary>
        /// List of columns to be removed from export by the provider to the specified ECF file
        /// </summary>
        public string[] RemoveExportHeaders { get; set; }

        /// <summary>
        /// ECF file name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// No export of the specified ECF file
        /// </summary>
        public bool NoExport { get; set; } = false;
    }
}
