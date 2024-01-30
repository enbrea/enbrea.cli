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

using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli
{
    public static class ImportContextManager
    {
        public static async Task<ImportContext> LoadFromFileAsync(string fileName, CancellationToken cancellationToken = default)
        {
            if (File.Exists(fileName))
            {
                using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                var loadSerializerOptions = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                };
                return await JsonSerializer.DeserializeAsync<ImportContext>(fileStream, loadSerializerOptions, cancellationToken);
            }
            else
            {
                return new ImportContext();
            }
        }

        public static async Task SaveToFileAsync(string fileName, ImportContext context, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            
            using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read);

            var templateSerializerOptions = new JsonSerializerOptions() 
            { 
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };

            await JsonSerializer.SerializeAsync(fileStream, context, templateSerializerOptions, cancellationToken);
        }
    }
}