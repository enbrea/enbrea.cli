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

using Enbrea.Ecf;
using Enbrea.Untis.Xml;
using System;
using System.Collections.Generic;

namespace Enbrea.Cli.Untis
{
    /// <summary>
    /// Extensions for <see cref="UntisHoliday"/>
    /// </summary>
    public static class UntisHolidayExtensions
    {
        public static List<EcfTemporalExpression> GetEcfTemporalExpressions(this UntisHoliday holiday)
        {
            var temporalExpressions = new List<EcfTemporalExpression>
            {
                new EcfOneTimeExpression()
                {
                    Operation = EcfTemporalExpressionOperation.Include,
                    StartTimepoint = new DateTimeOffset(holiday.StartTime.ToDateTime(new TimeOnly(0, 0))),
                    EndTimepoint = new DateTimeOffset(holiday.EndTime.ToDateTime(new TimeOnly(0, 0)))
                }
            };

            return temporalExpressions;
        }
    }
}