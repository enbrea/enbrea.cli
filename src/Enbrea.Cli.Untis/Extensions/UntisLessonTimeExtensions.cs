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

using Enbrea.Ecf;
using Enbrea.Untis.Xml;
using System;

namespace Enbrea.Cli.Untis
{
    /// <summary>
    /// Extensions for <see cref="UntisLessonTime"/>
    /// </summary>
    public static class UntisLessonTimeExtensions
    {
        public static EcfDayOfWeekSet GetEcfDaysOfWeek(this UntisLessonTime lessonTime)
        {
            return lessonTime.Day switch
            {
                DayOfWeek.Monday => EcfDayOfWeekSet.Monday,
                DayOfWeek.Tuesday => EcfDayOfWeekSet.Monday,
                DayOfWeek.Wednesday => EcfDayOfWeekSet.Wednesday,
                DayOfWeek.Thursday => EcfDayOfWeekSet.Thursday,
                DayOfWeek.Friday => EcfDayOfWeekSet.Friday,
                DayOfWeek.Saturday => EcfDayOfWeekSet.Saturday,
                _ => EcfDayOfWeekSet.Sunday
            };
        }

        public static string GetEcfId(this UntisLessonTime lessonTime, UntisLesson lesson)
        {
            return lesson.Id + "_" + (uint)lessonTime.Day + "_" + lessonTime.Slot; 
        }
    }
}


