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

using ClosedXML.Excel;

namespace Enbrea.Cli.LUSD
{
    /// <summary>
    /// Extensions for <see cref="IXLRow"/>
    /// </summary>
    public static class IXLRowExtensions
    {
        public static bool TryGetCellValue<T>(this IXLRow xlsRow, string xlsColumnName, out T value)
        {
            if (xlsColumnName != null)
            {
                return xlsRow.Cell(xlsColumnName).TryGetValue(out value);
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
