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

using System.Collections.Generic;
using System.Data.Common;

namespace Enbrea.Cli.Magellan
{
    /// <summary>
    /// Extensions for <see cref="DbCommand"/>
    /// </summary>
    public static class DbCommandExtensions
    {
        public static void AddParameters(this DbCommand dbCommand, IEnumerable<SqlAssigment> sqlAssigments)
        {
            foreach (var sqlAssigment in sqlAssigments)
            {
                var dbParameter = dbCommand.CreateParameter();
                dbParameter.ParameterName = sqlAssigment.ParamName;
                dbParameter.Value = sqlAssigment.Value;
                dbCommand.Parameters.Add(dbParameter);
            }
        }
    }
}
