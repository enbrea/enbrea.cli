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
using System.Text.Json.Serialization;
using Enbrea.Cli.Common;

namespace Enbrea.Cli
{
    public class CreateImportJobOptions
    {
        public CreateImportJobOptions(string schoolTerm, ImportProvider provider, EcfManifest manifest)
        {
            Provider = provider;
            SchoolTerm = schoolTerm;
            ValidFrom = manifest.ValidFrom;
            ValidTo = manifest.ValidTo;
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ImportProvider Provider { get; set; }
        public string SchoolTerm { get; set; }
        public DateTimeOffset? ValidFrom { get; set; }
        public DateTimeOffset? ValidTo { get; set; }
    }
}