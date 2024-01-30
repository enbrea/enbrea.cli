﻿#region ENBREA - Copyright (c) STÜBER SYSTEMS GmbH
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

namespace Enbrea.Cli
{
    /// <summary>
    /// The ECF import manifest is an optional file called "_Manifest.csv", which contains 
    /// global information such as the validity period. 
    /// </summary>
    public class ImportContext
    {
        /// <summary>
        /// Import data for this school term
        /// </summary>
        public string SchoolTerm { get; set; }

        /// <summary>
        /// Import data for validity period from
        /// </summary>
        public DateTimeOffset? ValidFrom { get; set; }

        /// <summary>
        /// Import data for validity period to
        /// </summary>
        public DateTimeOffset? ValidTo { get; set; }
    }
}
