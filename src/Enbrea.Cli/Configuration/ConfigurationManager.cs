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

using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli
{
    public static class ConfigurationManager
    {
        public static async Task<Configuration> LoadFromFile(FileInfo file, CancellationToken cancellationToken = default)
        {
            if (File.Exists(file.FullName))
            {
                using var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

                var loadSerializerOptions = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                };
                return await JsonSerializer.DeserializeAsync<Configuration>(fileStream, loadSerializerOptions, cancellationToken);
            }
            else
            {
                throw new FileNotFoundException($"File \"{file.FullName}\" does not exists.");
            }
        }

        public static async Task SaveTemplateToFile(FileInfo file, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(file.FullName))
            {
                using var fileStream = new FileStream(file.FullName, FileMode.Create, FileAccess.Write, FileShare.Read);

                var templateConfiguration = new Configuration();
                var templateSerializerOptions = new JsonSerializerOptions() 
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };
                await JsonSerializer.SerializeAsync(fileStream, templateConfiguration, templateSerializerOptions, cancellationToken);
            }
            else
            {
                throw new FileNotFoundException($"File \"{file.FullName}\" already exists.");
            }
        }
    }
}