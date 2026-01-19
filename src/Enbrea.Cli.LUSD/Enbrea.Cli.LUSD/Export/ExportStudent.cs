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

using Enbrea.Csv;
using Enbrea.Ecf;
using Enbrea.GuidFactory;
using System;

namespace Enbrea.Cli.LUSD
{
    public class ExportStudent
    {
        public readonly DateOnly? BirthDate = null;
        public readonly string FirstName = null;
        public readonly EcfGender? Gender = null;
        public readonly string Id;
        public readonly string LastName = null;
        public readonly string MiddleName = null;

        public ExportStudent(Configuration config, XlsxReader xlsReader)
        {
            xlsReader.TryGetValue(config.GetXlsxColumnName("Vorname"), out FirstName);
            xlsReader.TryGetValue(config.GetXlsxColumnName("Mittelname"), out MiddleName);
            xlsReader.TryGetValue(config.GetXlsxColumnName("Nachname"), out LastName);
            xlsReader.TryGetValue(config.GetXlsxColumnName("Geburtsdatum"), out BirthDate);
            xlsReader.TryGetValue(config.GetXlsxColumnName("Geschlecht"),  out Gender);
            Id = GenerateId();
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
