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

using System;

namespace Enbrea.Cli
{
    /// <summary>
    /// Extensions for <see cref="Uri"/>
    /// </summary>
    public static class UriExtensions
    {
        /// <summary>
        /// Adds in any case a trailing slash to the given uri
        /// </summary>
        /// <param name="uri">The uri</param>
        /// <returns>Uri with a trailing slash</returns>
        public static Uri AddTrailingSlash(this Uri uri)
        {
            var absoluteUri = uri.AbsoluteUri;

            if (absoluteUri[^1] == '/')
            {
                return uri;
            }
            else
            {
                return new Uri(absoluteUri + '/');
            }
        }
    }
}
