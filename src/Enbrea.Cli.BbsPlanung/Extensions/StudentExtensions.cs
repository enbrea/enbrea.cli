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

using Enbrea.BbsPlanung.Db;
using Enbrea.Cli.Common;
using Enbrea.Csv;
using Enbrea.Ecf;
using System;

namespace Enbrea.Cli.BbsPlanung
{
    /// <summary>
    /// Extensions for <see cref="Student"/>
    /// </summary>
    public static class StudentExtensions
    {
        public static DateOnly? GetBirthdateOrDefault(this Student student)
        {
            if (student.Birthdate != null)
            {
                return DateOnly.FromDateTime((DateTime)student.Birthdate);
            }
            else
            {
                return null;
            };
        }

        public static EcfGender? GetGenderOrDefault(this Student student)
        {
            return student.Gender switch
            {
                Gender.Female => EcfGender.Female,
                Gender.Male => EcfGender.Male,
                _ => null,
            };
        }
    }
}