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

using Enbrea.Cli.Common;
using Enbrea.Konsoli;
using System.Threading;

namespace Enbrea.Cli
{
    public static class ImportManagerFactory
    {
        public static EcfCustomManager CreateImportToEnbreaManager(
            ImportProvider provider, 
            Configuration config, 
            ImportBehaviour behaviour, 
            bool skipSnapshot, 
            bool skipImport,
            ConsoleWriter consoleWriter, 
            EventWaitHandle cancellationEvent, 
            CancellationToken cancellationToken)
        {
            return new ImportManager(
                provider,
                GetProviderEcfMapping(provider, config),
                GetEnbreaEcfTarget(provider, config),
                config,
                behaviour,
                skipSnapshot,
                skipImport,
                consoleWriter, 
                cancellationEvent, 
                cancellationToken);
        }

        public static EcfCustomManager CreateImportToProviderManager(
            ExportProvider provider, 
            Configuration config, 
            ConsoleWriter consoleWriter, 
            CancellationToken cancellationToken)
        {
            return provider switch
            {
                ExportProvider.davinci => new DaVinci.ImportManager(config.DaVinci, consoleWriter, cancellationToken),
                ExportProvider.magellan => new Magellan.ImportManager(config.Magellan, consoleWriter, cancellationToken),
                _ => null,
            };
        }

        private static string GetEnbreaEcfTarget(ImportProvider provider, Configuration config)
        {
            return provider switch
            {
                ImportProvider.davinci => config.DaVinci.TargetFolder,
                ImportProvider.magellan => config.Magellan.TargetFolder,
                ImportProvider.untis => config.Untis.TargetFolder,
                ImportProvider.bbsplanung => config.BbsPlanung.TargetFolder,
                ImportProvider.edoosys => config.Edoosys.TargetFolder,
                ImportProvider.schildnrw => config.SchildNRW.TargetFolder,
                ImportProvider.danis => config.Danis.TargetFolder,
                ImportProvider.saxsvs => config.SaxSVS.TargetFolder,
                ImportProvider.lusd => config.LUSD.TargetFolder,
                ImportProvider.excel => config.Excel.TargetFolder,
                _ => null,
            };
        }

        private static ProviderEcfMapping GetProviderEcfMapping(ImportProvider provider, Configuration config)
        {
            return provider switch
            {
                ImportProvider.davinci => config.DaVinci.EcfMapping,
                ImportProvider.magellan => config.Magellan.EcfMapping,
                ImportProvider.untis => config.Untis.EcfMapping,
                ImportProvider.bbsplanung => config.BbsPlanung.EcfMapping,
                ImportProvider.edoosys => config.Edoosys.EcfMapping,
                ImportProvider.schildnrw => config.SchildNRW.EcfMapping,
                ImportProvider.danis => config.Danis.EcfMapping,
                ImportProvider.saxsvs => config.SaxSVS.EcfMapping,
                _ => null,
            };
        }
    }
}
