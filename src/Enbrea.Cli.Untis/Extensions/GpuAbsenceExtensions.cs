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

using Enbrea.Untis.Gpu;
using Enbrea.Untis.Xml;
using System.Collections.Generic;

namespace Enbrea.Cli.Untis
{
    /// <summary>
    /// Extensions for <see cref="GpuAbsence"/>
    /// </summary>
    public static class GpuAbsenceExtensions
    {
        public static string GetEcfRoomId(this GpuAbsence absence, List<UntisRoom> rooms)
        {
            var room = rooms.Find(x => x.Id == absence.GetUntisRoomId());
            if (room != null)
            {
                return room.Id;
            }
            else
            {
                return default;
            }
        }

        public static string GetEcfSchoolClassId(this GpuAbsence absence, List<UntisClass> schoolClasses)
        {
            var schoolClass = schoolClasses.Find(x => x.Id == absence.GetUntisSchoolClassId());
            if (schoolClass != null)
            {
                return schoolClass.Id;
            }
            else
            {
                return default;
            }
        }

        public static string GetEcfTeacherId(this GpuAbsence absence, List<UntisTeacher> teachers)
        {
            var teacher = teachers.Find(x => x.Id == absence.GetUntisTeacherId());
            if (teacher != null)
            {
                return teacher.Id;
            }
            else
            {
                return default;
            }
        }

        public static string GetUntisRoomId(this GpuAbsence absence)
        {
            if (string.IsNullOrEmpty(absence.ShortName))
            {
                return null;
            }
            else
            {
                return "RM_" + absence.ShortName;
            }
        }

        public static string GetUntisSchoolClassId(this GpuAbsence absence)
        {
            if (string.IsNullOrEmpty(absence.ShortName))
            {
                return null;
            }
            else
            {
                return "CL_" + absence.ShortName;
            }
        }

        public static string GetUntisTeacherId(this GpuAbsence absence)
        {
            if (string.IsNullOrEmpty(absence.ShortName))
            {
                return null;
            }
            else
            {
                return "TR_" + absence.ShortName;
            }
        }

        public static bool IsInsideTerm(this GpuAbsence absence, UntisGeneralSettings generalSettings)
        {
            return
                (absence.StartDate >= generalSettings.TermBeginDate && absence.StartDate <= generalSettings.TermEndDate) ||
                (absence.EndDate >= generalSettings.TermBeginDate && absence.EndDate <= generalSettings.TermEndDate);
        }
    }
}