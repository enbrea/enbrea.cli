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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Enbrea.Cli
{
    /// <summary>
    /// Provides HTTP content based on JSON data
    /// </summary>
    public class JsonContent : ByteArrayContent
    {
        private const string DefaultMediaType = MediaTypeNames.Application.Json;

        public JsonContent(object content)
            : this(content, null, null)
        {
        }

        public JsonContent(object content, Encoding encoding)
            : this(content, encoding, null)
        {
        }

        public JsonContent(object content, Encoding encoding, string mediaType)
            : base(GetContentByteArray(content, encoding))
        {
            MediaTypeHeaderValue headerValue = new MediaTypeHeaderValue(mediaType ?? DefaultMediaType)
            {
                CharSet = (encoding == null) ? Encoding.UTF8.WebName : encoding.WebName
            };

            Headers.ContentType = headerValue;
        }

        private static byte[] GetContentByteArray(object content, Encoding encoding)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            var s = JsonSerializer.Serialize(content);

            return encoding.GetBytes(s);
        }
    }
}
