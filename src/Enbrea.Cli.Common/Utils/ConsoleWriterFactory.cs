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

using Enbrea.Konsoli;
using Microsoft.Extensions.Logging;

namespace Enbrea.Cli.Common
{
    /// <summary>
    /// Static factory class for <see cref="ConsoleWriter"/> instances
    /// </summary>
    public static class ConsoleWriterFactory
    {
        /// <summary>
        /// Creates a new <see cref="ConsoleWriter"/> instance.
        /// </summary>
        /// <param name="progressValueUnit">The progress value unit</param>
        /// <param name="logger">A logger instance</param>
        /// <returns>The new instance</returns>
        public static ConsoleWriter CreateConsoleWriter(ProgressUnit progressValueUnit, ILogger logger = null)
        {
            return new ConsoleWriter(progressValueUnit, logger)
            {
                Theme = new ConsoleWriterTheme()
                {
                    MessageTextFormat = "> {0}",
                    ProgressTextFormat = "> {0}",
                    SuccessLabel = ">",
                    WarningLabel = ">",
                    InformationLabel = ">"
                }
            };
        }
    }
}
