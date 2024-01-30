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
using Enbrea.GuidFactory;
using System;

namespace Enbrea.Cli.Common
{
    public static class IdFactory
    {
        public static Guid CreateIdFromValue(string value)
        {
            if (Guid.TryParse(value, out var id))
            {
                return id;
            }
            else
            {
                return GuidGenerator.Create(GuidGenerator.IsoOidNamespace, value);
            }
        }

        public static Guid CreateIdFromValues(params string[] values)
        {
            var csvLineBuilder = new CsvLineBuilder(new CsvConfiguration() { Separator = ';' });
            foreach (var value in values)
            {
                csvLineBuilder.Append(value);

           }
            return GuidGenerator.Create(GuidGenerator.IsoOidNamespace, csvLineBuilder.ToString());
        }
    }
}