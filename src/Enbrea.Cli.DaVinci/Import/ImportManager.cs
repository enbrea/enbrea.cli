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

using CliWrap.EventStream;
using Enbrea.Cli.Common;
using Enbrea.Konsoli;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.DaVinci
{
    public class ImportManager : EcfCustomManager
    {
        private readonly Configuration _config;

        public ImportManager(Configuration config, ConsoleWriter consoleWriter, CancellationToken cancellationToken)
            : base(config.TargetFolder, consoleWriter, cancellationToken)
        {
            _config = config;
        }

        public async override Task Execute()
        {
            _consoleWriter.Caption("Import to DAVINCI");

            // Prepare ECF export folder
            PrepareEcfFolder();

            // Create command for DAVINCI CONSOLE 
            var cmd = _config.DataProvider == DataProvider.File ?
                CliWrap.Cli.Wrap(ConsoleUtils.GetConsolePath())
                    .WithArguments(new[] {
                        "import", "-minimal-display",
                        "-s", "file",
                        "-i", GetEcfFolderName(),
                        "-fn", _config.DataFile
                    }) :
                CliWrap.Cli.Wrap(ConsoleUtils.GetConsolePath())
                    .WithArguments(new[] {
                        "import", "-minimal-display",
                        "-s", "server",
                        "-i", GetEcfFolderName(),
                        "-sn", _config.ServerName,
                        "-sp", _config.ServerPort.ToString(),
                        "-un", _config.ServerUserName,
                        "-up", _config.ServerPassword,
                        "-sf", $"{{{_config.ServerFileId}}}"
                    });

            // Excecute DAVINCI CONSOLE
            await foreach (var cmdEvent in cmd.ListenAsync(_cancellationToken))
            {
                switch (cmdEvent)
                {
                    case StartedCommandEvent:
                        _consoleWriter.Message("External DAVINCI CONSOLE started...");
                        break;
                    case ExitedCommandEvent:
                        _consoleWriter.Message("External DAVINCI CONSOLE exited.");
                        break;
                    case StandardOutputCommandEvent stdOutput:
                        _consoleWriter.WriteExternal(stdOutput.Text);
                        break;
                    case StandardErrorCommandEvent stdError:
                        throw new ConsoleException(stdError.Text);
                }
            }

            _consoleWriter.NewLine();
        }
    }
}
