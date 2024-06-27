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
using Enbrea.Ecf;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.Common
{
    /// <summary>
    /// Abstract manager for dealing with ECF files from or to an external application
    /// </summary>
    public abstract class EcfCustomManager
    {
        protected readonly CancellationToken _cancellationToken;
        protected readonly ConsoleWriter _consoleWriter;
        protected readonly string _dataFolderName;

        /// <summary>
        /// Initializes a new instance of the <see cref="EcfCustomManager"/> class.
        /// </summary>
        /// <param name="dataFolderName"></param>
        /// <param name="consoleWriter"></param>
        /// <param name="cancellationToken"></param>
        public EcfCustomManager(string dataFolderName, ConsoleWriter consoleWriter, CancellationToken cancellationToken)
        {
            _dataFolderName = dataFolderName;
            _cancellationToken = cancellationToken;
            _consoleWriter = consoleWriter;
        }

        public abstract Task Execute();

        public string GetEcfFolderName()
        {
            return Path.Combine(_dataFolderName, "ecf");
        }

        public string GetEcfManifestFileName()
        {
            return Path.ChangeExtension(Path.Combine(_dataFolderName, "ecf", EcfTables.Manifest), "csv");
        }

        public string GetLogFileName()
        {
            return Path.Combine(GetLogFolderName(), "log.txt");
        }

        public string GetLogFolderName()
        {
            return Path.Combine(_dataFolderName, "log");
        }
        protected void PrepareEcfFolder()
        {
            _consoleWriter.StartProgress("Prepare ECF folder");
            try
            {
                if (Directory.Exists(GetEcfFolderName()))
                {
                    foreach (var fileName in Directory.EnumerateFiles(GetEcfFolderName(), "*.csv"))
                    {
                        if (fileName.EndsWith(".csv", StringComparison.CurrentCultureIgnoreCase))
                        {
                            File.Delete(fileName);
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory(GetEcfFolderName());
                }

                _consoleWriter.FinishProgress();
            }
            catch
            {
                _consoleWriter.CancelProgress();
                throw;
            }
        }
    }
}