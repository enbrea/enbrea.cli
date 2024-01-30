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
using Enbrea.Konsoli;
using System.Threading;

namespace Enbrea.Cli
{
    public static class ExportManagerFactory
    {
        public static EcfCustomManager CreateExportFromEnbreaManager(ExportProvider provider, Configuration config, ConsoleWriter consoleWriter, EventWaitHandle cancellationEvent, CancellationToken cancellationToken)
        {
            return new ExportManager(provider, GetEnbreaEcfTarget(provider, config), config, consoleWriter, cancellationEvent, cancellationToken);
        }

        public static EcfCustomManager CreateExportFromProviderManager(ImportProvider provider, Configuration config, ConsoleWriter consoleWriter, CancellationToken cancellationToken)
        {
            switch (provider)
            {
                case ImportProvider.davinci:
                    return new DaVinci.ExportManager(config.DaVinci, consoleWriter, cancellationToken);
                case ImportProvider.magellan:
                    return new Magellan.ExportManager(config.Magellan, consoleWriter, cancellationToken);
                case ImportProvider.untis:
                    return new Untis.ExportManager(config.Untis, consoleWriter, cancellationToken);
                case ImportProvider.bbsplanung:
                    return new BbsPlanung.ExportManager(config.BbsPlanung, consoleWriter, cancellationToken);
                case ImportProvider.schildnrw:
                    return new SchildNRW.ExportManager(config.SchildNRW, consoleWriter, cancellationToken);
                case ImportProvider.edoosys:
                    if (config.Edoosys.DataProvider == Edoosys.DataProvider.Csv)
                    {
                        return new Edoosys.CsvExportManager(config.Edoosys, consoleWriter, cancellationToken);
                    }
                    else
                    {
                        return new Edoosys.DbExportManager(config.Edoosys, consoleWriter, cancellationToken);
                    }
                case ImportProvider.excel:
                    if (config.Excel.DataProvider == Excel.DataProvider.Csv)
                    {
                        return new Excel.CsvExportManager(config.Excel, consoleWriter, cancellationToken);
                    }
                    else
                    {
                        return new Excel.XlsxExportManager(config.Excel, consoleWriter, cancellationToken);
                    }
                default:
                    return null;
            }
        }

        private static string GetEnbreaEcfTarget(ExportProvider provider, Configuration config)
        {
            switch (provider)
            {
                case ExportProvider.davinci:
                    return config.DaVinci.SourceFolder;
                case ExportProvider.magellan:
                    return config.Magellan.SourceFolder;
                default:
                    return null;

            }
        }
    }
}
