﻿#region Enbrea - Copyright (c) STÜBER SYSTEMS GmbH
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
using Enbrea.Danis.Db;
using System;

namespace Enbrea.Cli.Danis
{
    /// <summary>
    /// Data type converter
    /// </summary>
    public static class ValueConverter
    {
        public static DateOnly? GetDateOrDefault(DateTime? value)
        {
            if (value != null)
            {
                return DateOnly.FromDateTime((DateTime)value);
            }
            return null;
        }

        public static EcfGender? GetGenderOrDefault(Gender? value)
        {
            if (value != null)
            {
                return (value) switch
                {
                    Gender.Male => EcfGender.Male,
                    Gender.Female => EcfGender.Female,
                    Gender.Diverse => EcfGender.Diverse,
                    _ => null,
                };
            }
            return null;
        }
    }
}