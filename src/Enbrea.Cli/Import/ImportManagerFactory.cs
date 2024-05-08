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
    public static class ImportManagerFactory
    {
        public static EcfCustomManager CreateImportToEnbreaManager(ImportProvider provider, Configuration config, bool skipSnapshot, ConsoleWriter consoleWriter, EventWaitHandle cancellationEvent, CancellationToken cancellationToken)
        {
            return new ImportManager(
                provider,
                GetProviderEcfMapping(provider, config),
                GetEnbreaEcfTarget(provider, config),
                config, 
                skipSnapshot, 
                consoleWriter, 
                cancellationEvent, 
                cancellationToken);
        }

        public static EcfCustomManager CreateImportToProviderManager(ExportProvider provider, Configuration config, ConsoleWriter consoleWriter, CancellationToken cancellationToken)
        {
            switch (provider)
            {
                case ExportProvider.davinci:
                    return new DaVinci.ImportManager(config.DaVinci, consoleWriter, cancellationToken);
                case ExportProvider.magellan:
                    return new Magellan.ImportManager(config.Magellan, consoleWriter, cancellationToken);
                default:
                    return null;
            }
        }

        private static string GetEnbreaEcfTarget(ImportProvider provider, Configuration config)
        {
            switch (provider)
            {
                case ImportProvider.davinci:
                    return config.DaVinci.TargetFolder;
                case ImportProvider.magellan:
                    return config.Magellan.TargetFolder;
                case ImportProvider.untis: 
                    return config.Untis.TargetFolder;
                case ImportProvider.bbsplanung:
                    return config.BbsPlanung.TargetFolder;
                case ImportProvider.edoosys:
                    return config.Edoosys.TargetFolder;
                case ImportProvider.schildnrw:
                    return config.SchildNRW.TargetFolder;
                case ImportProvider.excel:
                    return config.Excel.TargetFolder;
                default:
                    return null;

            }
        }

        private static ProviderEcfMapping GetProviderEcfMapping(ImportProvider provider, Configuration config)
        {
            switch (provider)
            {
                case ImportProvider.davinci:
                    return config.DaVinci.EcfMapping;
                case ImportProvider.magellan:
                    return config.Magellan.EcfMapping;
                case ImportProvider.untis:
                    return config.Untis.EcfMapping;
                case ImportProvider.bbsplanung:
                    return config.BbsPlanung.EcfMapping;
                case ImportProvider.edoosys:
                    return config.Edoosys.EcfMapping;
                case ImportProvider.schildnrw:
                    return config.SchildNRW.EcfMapping;
                default:
                    return null;
            }
        }
    }
}
