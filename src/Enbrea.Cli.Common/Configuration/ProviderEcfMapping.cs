﻿#region Enbrea - Copyright (c) STÜBER SYSTEMS GmbH
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

using System.Collections.Generic;

namespace Enbrea.Cli.Common
{
    /// <summary>
    /// Mapping of ECF files exported by an ECF provider
    /// </summary>
    /// <remarks>
    /// This mapping is optional. It defines for certain ECF files whether they should be exported or not. 
    /// And if they are exported, which columns should be exported.
    /// </remarks>
    public class ProviderEcfMapping
    {
        /// <summary>
        /// The list of file mappings
        /// </summary>
        public ICollection<ProviderEcfFileMapping> Files { get; set; }
    }
}
