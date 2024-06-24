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

using Enbrea.Csv;
using Enbrea.Ecf;
using Enbrea.GuidFactory;
using System;

namespace Enbrea.Cli.Excel
{
    public class ExportStudent
    {
        public readonly DateOnly? BirthDate = null;
        public readonly string FirstName = null;
        public readonly EcfGender? Gender = null;
        public readonly string Id;
        public readonly string LastName = null;
        public readonly string MiddleName = null;
        public readonly string NickName = null;
        public readonly string Salutation = null;
        public readonly string Email = null;

        public ExportStudent(Configuration config, CsvTableReader csvTableReader)
        {
            csvTableReader.TryGetValue(config.GetCsvHeaderName("Vorname"), out FirstName);
            csvTableReader.TryGetValue(config.GetCsvHeaderName("Mittelname"), out MiddleName);
            csvTableReader.TryGetValue(config.GetCsvHeaderName("Nachname"), out LastName);
            csvTableReader.TryGetValue(config.GetCsvHeaderName("Geburtstag"), out BirthDate);
            csvTableReader.TryGetValue(config.GetCsvHeaderName("Rufname"), out NickName);
            csvTableReader.TryGetValue(config.GetCsvHeaderName("Geschlecht"), out Gender);
            csvTableReader.TryGetValue(config.GetCsvHeaderName("Anrede"), out Salutation);
            csvTableReader.TryGetValue(config.GetCsvHeaderName("Email"), out Email);

            if (csvTableReader.TryGetValue(config.GetCsvHeaderName("Name"), out string name))
            {
                var csvLineParser = new CsvLineParser(' ');

                var parts = csvLineParser.Parse(name);

                if (parts.Length == 2)
                {
                    FirstName = parts[0];
                    LastName = parts[1];
                }
                else if (parts.Length == 3)
                {
                    FirstName = parts[0];
                    MiddleName = parts[1];
                    LastName = parts[2];
                }
            }

            if (!csvTableReader.TryGetValue(config.GetCsvHeaderName("Id"), out Id))
            {
                Id = GenerateId();
            }
        }

        public ExportStudent(Configuration config, XlsxReader xlsReader)
        {
            xlsReader.TryGetValue(config.GetXlsxColumnName("Vorname"), out FirstName);
            xlsReader.TryGetValue(config.GetXlsxColumnName("Mittelname"), out MiddleName);
            xlsReader.TryGetValue(config.GetXlsxColumnName("Nachname"), out LastName);
            xlsReader.TryGetValue(config.GetXlsxColumnName("Geburtstag"), out BirthDate);
            xlsReader.TryGetValue(config.GetXlsxColumnName("Rufname"), out NickName);
            xlsReader.TryGetValue(config.GetXlsxColumnName("Geschlecht"),  out Gender);
            xlsReader.TryGetValue(config.GetXlsxColumnName("Anrede"), out Salutation);

            if (xlsReader.TryGetValue(config.GetXlsxColumnName("Name"), out string name))
            {
                var csvLineParser = new CsvLineParser(' ');

                var parts = csvLineParser.Parse(name);
                
                if (parts.Length == 2)
                {
                    FirstName = parts[0];
                    LastName = parts[1];
                }
                else if (parts.Length == 3)
                {
                    FirstName = parts[0];
                    MiddleName = parts[1];
                    LastName = parts[2];
                }
            }

            if (!xlsReader.TryGetValue(config.GetXlsxColumnName("Id"), out Id))
            {
                Id = GenerateId();
            }
        }

        private string GenerateId()
        {
            var csvLineBuilder = new CsvLineBuilder();

            csvLineBuilder.Append(FirstName);
            csvLineBuilder.Append(MiddleName);
            csvLineBuilder.Append(LastName);
            csvLineBuilder.Append(BirthDate?.ToString("yyyy-MM-dd"));

            return GuidGenerator.Create(GuidGenerator.DnsNamespace, csvLineBuilder.ToString()).ToString();
        }
    }
}
