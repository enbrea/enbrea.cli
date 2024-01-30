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

using System;
using System.Collections.Generic;

namespace Enbrea.Cli.Magellan
{
    public static class ValueConverter
    {
        public static byte? ApplicationAssessment(string value)
        {
            return value switch
            {
                "A" => 3,
                "B" => 2,
                "C" => 1,
                _ => null,
            };
        }

        public static string Gender(string value)
        {
            return value switch
            {
                "Female" => "W",
                "Male" => "M",
                "Diverse" => "D",
                _ => string.Empty,
            };
        }

        public static byte GradeSystem(string value)
        {
            /*
             Possible values in MAGELLAN:
                0 = Notenwerte
                1 = Punktwerte
                2 = Beurteilungen
            */
            return value switch
            {
                "PT" => 1, // Placeholder for "Beurteilungen", has to be refactored in case 
                "BU" => 2,
                _ => 0,
            };
        }

        public static string Limit(string value, int maxLength)
        {
            if (value.Length > maxLength)
                return value.Substring(0, maxLength);
            else
                return value;
        }

        public static string Notification(string value)
        {
            // Possible values in MAGELLAN: Immer, Nur im Notfall, Nie  
            return value switch
            {
                "Always" => "0",
                "UrgentCasesOnly" => "1",
                "Never" => "2",
                _ => string.Empty,
            };
        }

        public static string Passfail(string value)
        {
            /*
             Possible values in MAGELLAN:
                P = Bestanden
                F = Nicht bestanden
                N = Nicht belegt
             */
            return value switch
            {
                "Passed" => "P",
                "Failed" => "F",
                "NotUsed" => "N",
                _ => string.Empty,
            };
        }

        public static string Priority(string value)
        {
            // Possible values in MAGELLAN: Telefon privat, Telefon beruf, Mobil
            return value switch
            {
                "HomePhoneNumber" => "0",
                "OfficePhoneNumber" => "1",
                "MobileNumber" => "2",
                _ => string.Empty,
            };
        }

        public static byte? RelationshipType(Dictionary<string, string> idMap, string idValue)
        {
            /*
             Possible values in MAGELLAN:
                0 = Mutter
                1 = Vater
                2 = Eltern
                3 = Erziehungsberechtigte(r)
                4 = Sorgeberechtigte(r)
                5 = Ansprechpartner(in)
                6 = Vormund
                7 = Großmutter
                8 = Großvater
                9 =Pflegeeltern
                11 = Verhältnis1
                12 = Verhältnis2
                13 = Verhältnis3
                14 = Verhältnis4
                15 = Verhältnis5
                16 = Verhältnis6
                17 = Verhältnis7
                18 = Verhältnis8
                19 = Verhältnis9
                20 = Verhältnis10
                (Werte für die Verhältnis 11 … 20 können über „Bezeichnungen anpassen" ersetzt werden)
                21 = Onkel
                22 = Tante
                23 = Bruder
                24 = Schwester
                25 = Erzieher
                26 = Notfall
                27 = Gasteltern
             */

            if (idMap.TryGetValue(idValue, out var code))
            {
                return code switch
                {
                    "Mother" => 0,
                    "Father" => 1,
                    "Mutter" => 0,
                    "Vater" => 1,
                    _ => 4,
                };
            }
            else
            {
                return null;
            }
        }

        public static string Salutation(string value)
        {
            /*
             Possible values in MAGELLAN:
                0 = Frau
                1 = Herr
                2 = Frau Dr.
                3 = Herr Dr.
                4 = Frau Prof.
                5 = Herr Prof.
                6 = Frau Prof. Dr.
                7 = Herr Prof. Dr.
                : = Ms.
                ; = Mrs.
                < = Mr.
             */
            return value switch
            {
                "Fr" => "0",
                "Frau" => "0",
                "Hr" => "1",
                "Herr" => "1",
                "FrDr" => "2",
                "Frau Dr." => "2",
                "HrDr" => "3",
                "Herr Dr." => "3",
                "FrPr" => "4",
                "Frau Prof." => "4",
                "HrPr" => "5",
                "Herr Prof." => "5",
                "FrPrDr" => "6",
                "Frau Prof. Dr." => "6",
                "HrPrDr" => "7",
                "Herr Prof. Dr." => "7",
                "Famlie" => "8",
                "Herr und Frau" => "9",
                "Ms" => ":",
                "Ms." => ":",
                "Mrs" => ";",
                "Mrs." => ";",
                "Mr" => "<",
                "Mr." => "<",
                "Eheleute" => "?",
                _ => string.Empty,
            };
        }

        public static byte TermSection(string value)
        {
            return value switch
            {
                "1" => 0,
                "2" => 1,
                "3" => 2,
                _ => throw new NotImplementedException()
            };
        }
    }
}
