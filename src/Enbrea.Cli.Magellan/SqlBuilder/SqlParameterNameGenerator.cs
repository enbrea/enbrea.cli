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

namespace Enbrea.Cli.Magellan
{
    /// <summary>
    /// Generates unique names for parameters.
    /// </summary>
    public class SqlParameterNameGenerator
    {
        private uint _count = 0;

        /// <summary>
        /// Generates the next unique parameter name.
        /// </summary>
        /// <returns>The generated name.</returns>
        public virtual string GenerateNext() => "p" + _count++;

        /// <summary>
        /// Resets the counter to zero.
        /// </summary>
        public void Reset() => _count = 0;
    }
}
