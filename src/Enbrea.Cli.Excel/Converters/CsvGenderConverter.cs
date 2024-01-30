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

using Enbrea.Csv;
using Enbrea.Ecf;
using System.Globalization;

namespace Enbrea.Cli.Excel
{
    /// <summary>
    /// Implementation of a <see cref="EcfGender"> converter from CSV
    /// </summary>
    public class CsvGenderConverter : CsvDefaultEnumConverter
    {
        public CsvGenderConverter() : 
            base(typeof(EcfGender), CultureInfo.InvariantCulture, true)
        {
        }

        public override object FromString(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                switch (value)
                {
                    case "w":
                    case "W":
                        return EcfGender.Female;
                    case "m":
                    case "M":
                        return EcfGender.Male;
                    case "d":
                    case "D":
                        return EcfGender.Diverse;
                    default:
                        return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}