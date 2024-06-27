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
using System;
using System.Data.Common;

namespace Enbrea.Cli.Magellan
{
    /// <summary>
    /// Extensions for <see cref="DbDataReader"/>
    /// </summary>
    public static class DbDataReaderExtensions
    {
        public static DateOnly? GetDateOrDefault(this DbDataReader dbDataReader, string name)
        {
            var value = dbDataReader[name];
            if (value != null)
            {
                if (value.GetType() == typeof(DateTime))
                {
                    return DateOnly.FromDateTime((DateTime)value);
                }
            }
            return null;
        }

        public static EcfGender? GetGenderOrDefault(this DbDataReader dbDataReader, string name)
        {
            var value = dbDataReader[name];
            if (value != null)
            {
                if (value.GetType() == typeof(string))
                {
                    return ((string)value) switch
                    {
                        "M" => EcfGender.Male,
                        "W" => EcfGender.Female,
                        _ => null,
                    };
                }
            }
            return null;
        }

        public static DateOnly? GetOldestDateOrDefault(this DbDataReader dbDataReader, string name1, string name2)
        {
            var value1 = dbDataReader.GetDateOrDefault(name1);
            var value2 = dbDataReader.GetDateOrDefault(name2);

            if (value1 == null)
            {
                return value2;
            }
            else if (value2 == null)
            {
                return value1;
            }
            else if (value1 < value2)
            {
                return value1;
            }
            else
            {
                return value2;
            }
        }

        public static string GetSalutationOrDefault(this DbDataReader dbDataReader, string name)
        {
            var value = dbDataReader[name];
            if (value != null)
            {
                if (value.GetType() == typeof(string))
                {
                    return ((string)value) switch
                    {
                        "0" => "Frau",
                        "1" => "Herr",
                        "2" => "Frau Dr.",
                        "3" => "Herr Dr.",
                        "4" => "Frau Prof.",
                        "5" => "Herr Prof.",
                        "6" => "Frau Prof.Dr.",
                        "7" => "Herr Prof.Dr.",
                        ":" => "Ms.",
                        ";" => "Mrs.",
                        "<" => "Mr.",
                        _ => null,
                    };
                }
            }
            return null;
        }

        public static short GetShortOrDefault(this DbDataReader dbDataReader, string name, short defaultValue)
        {
            var value = dbDataReader[name];
            if (value != null)
            {
                if (value.GetType() == typeof(short))
                {
                    return (short)value;
                }
            }
            return defaultValue;
        }

        public static DateOnly? GetYoungestDateOrDefault(this DbDataReader dbDataReader, string name1, string name2)
        {
            var value1 = dbDataReader.GetDateOrDefault(name1);
            var value2 = dbDataReader.GetDateOrDefault(name2);

            if (value1 == null)
            {
                return value2;
            }
            else if (value2 == null)
            {
                return value1;
            }
            else if (value1 > value2)
            {
                return value1;
            }
            else
            {
                return value2;
            }
        }

        public static bool IsNullOrEmpty(this DbDataReader dbDataReader, string name)
        {
            var value = dbDataReader[name];
            return (value == null) || (value is DBNull);
        }
    }
}