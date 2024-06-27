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

using Enbrea.Cli.Common;
using Enbrea.Ecf;
using Enbrea.Untis.Gpu;
using Enbrea.Untis.Xml;
using System.Collections.Generic;
using System.Linq;

namespace Enbrea.Cli.Untis
{
    /// <summary>
    /// Extensions for <see cref="GpuSubstitution"/>
    /// </summary>
    public static class GpuSubstitutionExtensions
    {
        public static string GetEcfCourseId(this GpuSubstitution substitution, List<UntisLesson> lessons)
        {
            var lesson = lessons.Find(x => x.GetGroupId() == substitution.LessonId && x.SubjectId != null && x.TeacherId == "TR_" + substitution.AbsentTeacher);

            if (lesson != null)
            {
                return lesson.Id;
            }
            else
            {
                return default;
            }
        }

        public static string GetEcfId(this GpuSubstitution substitution)
        {
            return $"SB_{substitution.Id}";
        }

        public static string GetEcfLessonGapId(this GpuSubstitution substitution)
        {
            return $"LG_{substitution.Id}";
        }

        public static string GetEcfLessonId(this GpuSubstitution substitution, List<UntisLesson> lessons)
        {
            var lesson = lessons.Find(x => x.GetGroupId() == substitution.LessonId && x.SubjectId != null && x.TeacherId == "TR_"+ substitution.AbsentTeacher);

            if (lesson != null)
            {
                var lessonTime = lesson.Times.Find(x => (x.Day == substitution.Date.DayOfWeek) && (x.Slot == substitution.TimeSlot));
                if (lessonTime != null)
                {
                    return lesson.Id + "_" + (uint)substitution.Date.DayOfWeek + "_" + ((lessonTime.SlotGroupFirstSlot != null) ? lessonTime.SlotGroupFirstSlot : lessonTime.Slot); 
                }
                else
                {
                    return default; 
                }
            }
            else 
            {
                return default;
            }
        }

        public static List<EcfGapReason> GetEcfReasons(this GpuSubstitution substitution, HashSet<uint> absencesCache)
        {
            var gapReasons = new List<EcfGapReason>();

            if (substitution.AbsenceId != null)
            {
                if (absencesCache.Contains((uint)substitution.AbsenceId))
                {
                    gapReasons.Add(new EcfAbsenceGapReason() { AbsenceId = substitution.AbsenceId?.ToString() });
                }
            }

            return gapReasons;
        }

        public static List<EcfGapResolution> GetEcfResolutions(this GpuSubstitution substitution)
        {
            var gapResolutions = new List<EcfGapResolution>();

            if ((substitution.Type == GpuSubstitutionType.Cancellation) || (substitution.Type == GpuSubstitutionType.Exemption))
            {
                gapResolutions.Add(new EcfLessonGapCancellation() { Behaviour = EcfLessonGapCancellationBehaviour.None });
            }
            else
            {
                gapResolutions.Add(new EcfLessonGapSubstitution() { SubstituteLessonId = substitution.GetEcfId() });
            }

            return gapResolutions;
        }

        public static List<string> GetEcfRoomIdList(this GpuSubstitution substitution)
        {
            if (substitution.StandInRooms.Count > 0)
            {
                return substitution.StandInRooms.Select(x => "RM_" + x).ToList();
            }
            else
            {
                return substitution.Rooms.Select(x => "RM_" + x).ToList();
            }
        }

        public static List<string> GetEcfSchoolClassIdList(this GpuSubstitution substitution)
        {
            if (substitution.SchoolClasses.Count > 0)
            {
                return substitution.StandInSchoolClasses.Select(x => "CL_" + x).ToList();
            }
            else
            {
                return substitution.SchoolClasses.Select(x => "CL_" + x).ToList();
            }
        }

        public static List<string> GetEcfTeacherIdList(this GpuSubstitution substitution)
        {
            var idList = new List<string>();
            
            if (!string.IsNullOrEmpty(substitution.StandInTeacher))
            {
                idList.Add("TR_" + substitution.StandInTeacher);
            }
            else if (!string.IsNullOrEmpty(substitution.AbsentTeacher))
            {
                idList.Add("TR_" + substitution.AbsentTeacher);
            }
            
            return idList;
        }

        public static List<EcfTemporalExpression> GetEcfTemporalExpressions(this GpuSubstitution substitution, List<UntisTimeGrid> timeGrids, List<UntisLesson> lessons)
        {
            var temporalExpressions = new List<EcfTemporalExpression>();

            var lesson = lessons.Find(x => x.GetGroupId() == substitution.LessonId);

            if (lesson != null)
            {
                var timeGrid = timeGrids.Find(x => x.Name == lesson.TimeGridId);

                if (timeGrid != null)
                {
                    var timeGridSlot = timeGrid.Slots.Find(x => x.Period == substitution.TimeSlot);

                    if (timeGridSlot != null)
                    {
                        temporalExpressions.Add(new EcfOneTimeExpression()
                        {
                            Operation = EcfTemporalExpressionOperation.Include,
                            StartTimepoint = DateOnlyUtils.ToDateTimeOffset(substitution.Date).AddMinutes(timeGridSlot.StartTime.TotalMinutes),
                            EndTimepoint = DateOnlyUtils.ToDateTimeOffset(substitution.Date).AddMinutes(timeGridSlot.EndTime.TotalMinutes)
                        });
                    }
                }
            }

            return temporalExpressions;
        }
    }
}