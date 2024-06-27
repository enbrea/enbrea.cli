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

using Enbrea.Cli.Common;
using Enbrea.Ecf;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Enbrea.Cli
{
    public static class EcfUtils
    {
        static public async Task ApplyProviderExportFileMappings(IEnumerable<ImportFile> files, ICollection<ProviderEcfFileMapping> fileMappings)
        {
            if (fileMappings != null && fileMappings.Count > 0)
            {
                foreach (var file in files)
                {
                    var fileMapping = fileMappings.FirstOrDefault(x => x.Name == file.TableName);

                    if (fileMapping != null)
                    {
                        if (File.Exists(file.FullName))
                        {
                            if (fileMapping.NoExport)
                            {
                                File.Delete(file.FullName);
                            }
                            else
                            {
                                try
                                {
                                    using (var ecfTextReader = File.OpenText(file.FullName))
                                    {
                                        using (var ecfTextWriter = File.CreateText(file.FullNameForTemporaryUse))
                                        {
                                            var ecfTableReader = new EcfTableReader(ecfTextReader);
                                            var ecfTableWriter = new EcfTableWriter(ecfTextWriter);

                                            await ecfTableReader.ReadHeadersAsync();

                                            var reducedHeaders = ecfTableReader.Headers.Where(x => !fileMapping.RemoveExportHeaders.Any(h => h == x));

                                            await ecfTableWriter.WriteHeadersAsync(reducedHeaders);

                                            while (await ecfTableReader.ReadAsync() > 0)
                                            {
                                                foreach (var headerName in ecfTableReader.Headers)
                                                {
                                                    ecfTableWriter.TrySetValue(headerName, ecfTableReader[headerName]);
                                                }
                                                await ecfTableWriter.WriteAsync();
                                            }
                                        }
                                    }

                                    File.Delete(file.FullName);
                                    File.Move(file.FullNameForTemporaryUse, file.FullName);
                                }
                                catch
                                {
                                    File.Delete(file.FullNameForTemporaryUse);
                                    throw;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
