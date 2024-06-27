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

using Enbrea.Ecf;
using System;

namespace Enbrea.Cli.Common
{
    /// <summary>
    /// The ECF manifest is an optional file called "_Manifest.csv", which contains 
    /// global information such as the validity period. 
    /// </summary>
    public class EcfManifest
    {
        /// <summary>
        /// Creates a new <see cref="EcfManifest"/> instance.
        /// </summary>
        public EcfManifest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="EcfManifest"/> instance from an ECF dictionary.
        /// </summary>
        /// <param name="ecfDictionary">The ECF dictionary</param>
        public EcfManifest(EcfDictionary ecfDictionary)
        {
            ecfDictionary.UseValue<DateTimeOffset?>(EcfKeys.ValidFrom, value => ValidFrom = value);
            ecfDictionary.UseValue<DateTimeOffset?>(EcfKeys.ValidTo, value => ValidTo = value);
        }

        /// <summary>
        /// Validity period from
        /// </summary>
        public DateTimeOffset? ValidFrom { get; set; }

        /// <summary>
        /// Validity period to
        /// </summary>
        public DateTimeOffset? ValidTo { get; set; }
    }
}
