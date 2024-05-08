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

using Enbrea.Cli.Common;
using System.IO;

namespace Enbrea.Cli
{
    public class ImportFile
    {
        public ImportFile(string ecfFolderName, EcfFileMapping fileMapping)
        {
            FullName = Path.Combine(ecfFolderName, Path.ChangeExtension(fileMapping.Name, "csv"));
            KeyHeaders = fileMapping.KeyHeaders;
        }

        public string FullName { get; }

        public string FullNameForChangedOnlyRows
        {
            get { return Path.ChangeExtension(FullName, "csv.changed"); }
        }

        public string FullNameForDeletedOnlyRows
        {
            get { return Path.ChangeExtension(FullName, "csv.deleted"); }
        }

        public string FullNameForPreviousRows
        {
            get { return Path.ChangeExtension(FullName, "csv.previous"); }
        }

        public string FullNameForTemporaryUse
        {
            get { return Path.ChangeExtension(FullName, "csv.tmp"); }
        }

        public string[] KeyHeaders { get; }
        
        public string TableName
        {
            get { return Path.GetFileName(Path.GetFileNameWithoutExtension(FullName)); }
        }
    }
}
