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

namespace Enbrea.Cli.Common
{
    /// <summary>
    /// Mapping of ECF files to ENBREA.
    /// </summary>
    /// <remarks>
    /// The mapping defines which ECF files should be imported to ENBREA or exported from ENBREA and what
    /// is the unique key of each ECF file.
    /// </remarks>
    public class EcfMapping
    {
        /// <summary>
        /// The list of mappings
        /// </summary>
        public ICollection<EcfFileMapping> Files { get; set; } = new List<EcfFileMapping>()
        { 
            new EcfFileMapping() { Name = "Announcements", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "Countries", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "CourseCategories", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "CourseFlags", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "Courses", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "CourseTypes", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "Departments", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "EducationalAreas", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "EducationalContents", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "EducationalMaterials", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "EducationalPrograms", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "EventTypes", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "ExamTypes", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "FormsOfTeaching", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "Holidays", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "Languages", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "LessonGaps", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "LessonProfiles", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "LevelOfQualifications", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "MaritalStatuses", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "Nationalities", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "Regions", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "Religions", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "ResourceCategories", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "ResourceFlags", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "RoomAbsenceReasons", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "RoomAbsences", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "Rooms", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "RoomTypes", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "ScheduledLessons", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "SchoolClassAbsenceReasons", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "SchoolClassAbsences", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "SchoolClasses", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "SchoolClassFlags", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "SchoolClassLevels", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "SchoolClassProfils", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "SchoolClassTypes", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "SchoolForms", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "SchoolOrganisations", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "SchoolTypes", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "StudentAbsences", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "StudentCourseAttendances", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "Students", KeyHeaders = ["LastName", "FirstName", "Birthdate"] },
            new EcfFileMapping() { Name = "StudentSchoolClassAttendances", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "StudentSubjects", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "SubjectFocuses", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "SubjectLearningField", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "Subjects", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "SubjectTypes", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "SubstituteLessons", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "SubstitutionQualities", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "TeacherAbsenceReasonDifferentiations", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "TeacherAbsenceReasons", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "TeacherAbsences", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "TeacherCourseAttendances", KeyHeaders = ["Id"] },
            new EcfFileMapping() { Name = "Teachers", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "TeacherTypes", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "TimeAccountEntryReasons", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "TimeAccountEntryReportings", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "TimeAccountEntryTypes", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "TimeFrames", KeyHeaders = ["Code"] },
            new EcfFileMapping() { Name = "VocationalFields", KeyHeaders = ["Code" ] }
       };
    }
}
