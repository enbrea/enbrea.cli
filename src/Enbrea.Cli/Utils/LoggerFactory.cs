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

using Serilog;
using Serilog.Extensions.Logging;

namespace Enbrea.Cli
{
    public static class LoggerFactory
    {
        public static Microsoft.Extensions.Logging.ILogger CreateLogger(string logFile)
        {
            if (!string.IsNullOrEmpty(logFile))
            {
                // Create a new Serilog instance
                var seriLog = new LoggerConfiguration()
                    //.WriteTo.File(logFile)
                    .WriteTo.File(
                        path: logFile,
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 90)
                    .CreateLogger();

                // Create a new Microsoft logging instance out of Serilog.
                return new SerilogLoggerFactory(seriLog).CreateLogger(null);
            }
            else
            {
                return null;
            }
        }
    }
}
