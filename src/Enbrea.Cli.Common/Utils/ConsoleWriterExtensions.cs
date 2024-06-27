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

using Enbrea.Konsoli;
using System;

namespace Enbrea.Cli.Common
{
    public static class ConsoleWriterExtensions
    {
        public static void WriteExternal(this ConsoleWriter consoleWriter, string text)
        {
            consoleWriter.Theme.MessageTextColor = ConsoleColor.DarkGray;
            consoleWriter.Message(text);
            consoleWriter.Theme.MessageTextColor = consoleWriter.Theme.DefaultTextColor;
        }
    }
}
