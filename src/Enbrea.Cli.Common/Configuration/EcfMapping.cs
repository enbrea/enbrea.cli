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

using System.Collections.Generic;

namespace Enbrea.Cli.Common
{
    /// <summary>
    /// Mapping of ECF files to Enbrea.
    /// </summary>
    /// <remarks>
    /// The mapping defines which ECF files should be imported to Enbrea or exported from Enbrea and what
    /// is the unique key of each ECF file.
    /// </remarks>
    public class EcfMapping
    {
        /// <summary>
        /// The list of mappings
        /// </summary>
        public ICollection<EcfFileMapping> Files { get; set; } =
        [
            new() { Name = "Announcements", KeyHeaders = ["Id"] },
            new() { Name = "ApplicationAssessments", KeyHeaders = ["Id"] },
            new() { Name = "ApplicationEnrollmentSupports", KeyHeaders = ["Id"] },
            new() { Name = "ApplicationEnrollmentSupportSelections", KeyHeaders = ["Id"] },
            new() { Name = "ApplicationEnrollmentTypes", KeyHeaders = ["Id"] },
            new() { Name = "ApplicationEnrollmentTypeSelections", KeyHeaders = ["Id"] },
            new() { Name = "ApplicationLevels", KeyHeaders = ["Id"] },
            new() { Name = "Applications", KeyHeaders = ["Id"] },
            new() { Name = "ApplicationTargets", KeyHeaders = ["Id"] },
            new() { Name = "ApplicationTargetSelections", KeyHeaders = ["Id"] },
            new() { Name = "ApplicationTargetTracks", KeyHeaders = ["Id"] },
            new() { Name = "Countries", KeyHeaders = ["Code"] },
            new() { Name = "CourseCategories", KeyHeaders = ["Code"] },
            new() { Name = "CourseFlags", KeyHeaders = ["Code"] },
            new() { Name = "Courses", KeyHeaders = ["Id"] },
            new() { Name = "CourseTypes", KeyHeaders = ["Code"] },
            new() { Name = "CustodianRelationshipTypes", KeyHeaders = ["Code"] },
            new() { Name = "Custodians", KeyHeaders = ["Id"] },
            new() { Name = "Departments", KeyHeaders = ["Code"] },
            new() { Name = "EducationalAreas", KeyHeaders = ["Code"] },
            new() { Name = "EducationalContents", KeyHeaders = ["Code"] },
            new() { Name = "EducationalMaterials", KeyHeaders = ["Code"] },
            new() { Name = "EducationalPrograms", KeyHeaders = ["Code"] },
            new() { Name = "Events", KeyHeaders = ["Id"] },
            new() { Name = "EventTypes", KeyHeaders = ["Code"] },
            new() { Name = "ExamTypes", KeyHeaders = ["Code"] },
            new() { Name = "ForeignLanguages", KeyHeaders = ["Code"] },
            new() { Name = "FormsOfTeaching", KeyHeaders = ["Code"] },
            new() { Name = "Holidays", KeyHeaders = ["Id"] },
            new() { Name = "Languages", KeyHeaders = ["Code"] },
            new() { Name = "LessonGaps", KeyHeaders = ["Id"] },
            new() { Name = "LessonProfiles", KeyHeaders = ["Code"] },
            new() { Name = "LevelOfQualifications", KeyHeaders = ["Code"] },
            new() { Name = "MaritalStatuses", KeyHeaders = ["Code"] },
            new() { Name = "Nationalities", KeyHeaders = ["Code"] },
            new() { Name = "Regions", KeyHeaders = ["Code"] },
            new() { Name = "Religions", KeyHeaders = ["Code"] },
            new() { Name = "ResourceCategories", KeyHeaders = ["Code"] },
            new() { Name = "ResourceFlags", KeyHeaders = ["Code"] },
            new() { Name = "RoomAbsenceReasons", KeyHeaders = ["Code"] },
            new() { Name = "RoomAbsences", KeyHeaders = ["Id"] },
            new() { Name = "Rooms", KeyHeaders = ["Code"] },
            new() { Name = "RoomTypes", KeyHeaders = ["Code"] },
            new() { Name = "ScheduledLessons", KeyHeaders = ["Id"] },
            new() { Name = "ScheduledSupervisions", KeyHeaders = ["Id"] },
            new() { Name = "SchoolClassAbsenceReasons", KeyHeaders = ["Code"] },
            new() { Name = "SchoolClassAbsences", KeyHeaders = ["Id"] },
            new() { Name = "SchoolClasses", KeyHeaders = ["Code"] },
            new() { Name = "SchoolClassFlags", KeyHeaders = ["Code"] },
            new() { Name = "SchoolClassLevels", KeyHeaders = ["Code"] },
            new() { Name = "SchoolClassProfils", KeyHeaders = ["Code"] },
            new() { Name = "SchoolClassTypes", KeyHeaders = ["Code"] },
            new() { Name = "SchoolForms", KeyHeaders = ["Code"] },
            new() { Name = "SchoolOrganisations", KeyHeaders = ["Code"] },
            new() { Name = "SchoolTypes", KeyHeaders = ["Code"] },
            new() { Name = "StudentAbsences", KeyHeaders = ["Id"] },
            new() { Name = "StudentCourseAttendances", KeyHeaders = ["Id"] },
            new() { Name = "StudentCustodians", KeyHeaders = ["Id"] },
            new() { Name = "StudentForeignLanguages", KeyHeaders = ["Id"] },
            new() { Name = "Students", KeyHeaders = ["LastName", "FirstName", "Birthdate"] },
            new() { Name = "StudentSchoolClassAttendances", KeyHeaders = ["Id"] },
            new() { Name = "StudentSubjects", KeyHeaders = ["Id"] },
            new() { Name = "SubjectFocuses", KeyHeaders = ["Code"] },
            new() { Name = "SubjectLearningField", KeyHeaders = ["Code"] },
            new() { Name = "Subjects", KeyHeaders = ["Code"] },
            new() { Name = "SubjectTypes", KeyHeaders = ["Code"] },
            new() { Name = "SubstituteLessons", KeyHeaders = ["Id"] },
            new() { Name = "SubstituteSupervisions", KeyHeaders = ["Id"] },
            new() { Name = "SubstitutionQualities", KeyHeaders = ["Code"] },
            new() { Name = "TeacherAbsenceReasonDifferentiations", KeyHeaders = ["Code"] },
            new() { Name = "TeacherAbsenceReasons", KeyHeaders = ["Code"] },
            new() { Name = "TeacherAbsences", KeyHeaders = ["Id"] },
            new() { Name = "TeacherCourseAttendances", KeyHeaders = ["Id"] },
            new() { Name = "Teachers", KeyHeaders = ["Code"] },
            new() { Name = "TeacherTypes", KeyHeaders = ["Code"] },
            new() { Name = "TimeAccountEntryReasons", KeyHeaders = ["Code"] },
            new() { Name = "TimeAccountEntryReportings", KeyHeaders = ["Code"] },
            new() { Name = "TimeAccountEntryTypes", KeyHeaders = ["Code"] },
            new() { Name = "TimeFrames", KeyHeaders = ["Code"] },
            new() { Name = "VocationalFields", KeyHeaders = ["Code" ] }
       ];
    }
}
