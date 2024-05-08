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

using Enbrea.Ecf;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.Common
{
    public static class EcfManifestManager
    {
        public static async Task<EcfManifest> LoadFromFileAsync(string fileName, CancellationToken cancellationToken = default)
        {
            if (File.Exists(fileName))
            {
                // Open file stream
                using var strReader = File.OpenText(fileName);

                // Create ECF dictionary
                var ecfDictionary = new EcfDictionary();

                // Load ECF dictionary
                await ecfDictionary.LoadAsync(strReader, cancellationToken);

                // Create, fill and return manifest
                return new EcfManifest(ecfDictionary);
            }
            else
            {
                return new EcfManifest();
            }
        }

        public static async Task SaveToFileAsync(string fileName, EcfManifest manifest, CancellationToken cancellationToken = default)
        {
            // Create file stream
            using var strWriter = File.CreateText(fileName);

            // Create ECF dictionary
            var ecfDictionary = new EcfDictionary();

            // Fill ECF dictionary
            ecfDictionary.SetValue(EcfKeys.ValidFrom, manifest.ValidFrom);
            ecfDictionary.SetValue(EcfKeys.ValidTo, manifest.ValidTo);

            // Store ECF dictionary
            await ecfDictionary.StoreAsync(strWriter, cancellationToken);
        }
    }
}