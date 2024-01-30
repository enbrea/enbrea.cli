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

using CliWrap;
using Enbrea.Konsoli;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Enbrea.Cli.Untis
{
    public static class ConsoleUtils
    {
        static public async Task RunUntisBackup(
            ConsoleWriter consoleWriter,
            string schoolNo,
            string schoolYear,
            uint version,
            string userName,
            string password,
            string outputFile,
            CancellationToken cancellationToken)
        {
            consoleWriter.StartProgress($"Create Untis backup from server");
            try
            {
                // Delete old backup file
                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }

                // Execute Untis 
                var result = await CliWrap.Cli.Wrap(GetUntisPath())
                    .WithArguments(new[] {
                        $"DB~{schoolNo}~{schoolYear}~{version}",
                        $"/backup={outputFile}",
                        $"/user={userName}",
                        $"/pwd={password}"
                    })
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync(cancellationToken);

                // Analyze results
                if ((result.ExitCode == 0) && (File.Exists(outputFile)))
                {
                    consoleWriter.FinishProgress();
                }
                else
                {
                    throw new ConsoleException($"{Path.GetFileName(outputFile)} file not created.");
                }
            }
            catch
            {
                consoleWriter.CancelProgress();
                throw;
            }
        }

        static public void RunUntisBackup(
            ConsoleWriter consoleWriter,
            string inputFile,
            string outputFile)
        {
            consoleWriter.StartProgress($"Create Untis backup");
            try
            {
                // Delete old backup file
                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }

                // Just copy the file
                File.Copy(inputFile, outputFile);

                // Analyze results
                if (File.Exists(outputFile))
                {
                    consoleWriter.FinishProgress();
                }
                else
                {
                    throw new ConsoleException($"{Path.GetFileName(outputFile)} file not created.");
                }
            }
            catch
            {
                consoleWriter.CancelProgress();
                throw;
            }
        }

        static public async Task RunUntisGpuExport(
            ConsoleWriter consoleWriter,
            string untisFile,
            string[] outputTypes,
            string outputFolder,
            CancellationToken cancellationToken)
        {
            foreach (var outputType in outputTypes)
            {
                await RunUntisSingleGpuExport(consoleWriter, untisFile, outputType, outputFolder, cancellationToken);
            }
        }

        static public async Task RunUntisSingleGpuExport(
            ConsoleWriter consoleWriter,
            string untisFile,
            string outputType,
            string outputFolder,
            CancellationToken cancellationToken)
        {
            // Gpu file name
            var outputFile = Path.Combine(outputFolder, $"GPU{outputType}.TXT");

            // Start export
            consoleWriter.StartProgress($"Export {Path.GetFileName(outputFile)}");
            try
            {
                // Delete old gpu file
                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }

                // Execute Untis
                var result = await CliWrap.Cli.Wrap(GetUntisPath())
                    .WithArguments(new[] {
                        $"{untisFile}",
                        $"/exp{outputType}={outputFile}"
                    })
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync(cancellationToken);

                // Analyze results
                if (File.Exists(outputFile))
                {
                    consoleWriter.FinishProgress();
                }
                else
                {
                    throw new ConsoleException($"{Path.GetFileName(outputFile)} file not created.");
                }
            }
            catch
            {
                consoleWriter.CancelProgress();
                throw;
            }
        }

        static public async Task RunUntisXmlExport(
            ConsoleWriter consoleWriter,
            string untisFile,
            string outputFolder,
            CancellationToken cancellationToken)
        {
            // XML file name
            var outputFile = Path.Combine(outputFolder, $"untis.xml");

            // Start export
            consoleWriter.StartProgress($"Export {Path.GetFileName(outputFile)}");
            try
            {
                // Delete old XML file
                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }

                // Execute Untis 
                var result = await CliWrap.Cli.Wrap(GetUntisPath())
                    .WithArguments(new[] {
                    $"{untisFile}",
                    $"/xml={outputFile}"
                    })
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync(cancellationToken);

                // Analyze results
                if (File.Exists(outputFile))
                {
                    consoleWriter.FinishProgress();
                }
                else
                {
                    throw new ConsoleException($"{Path.GetFileName(outputFile)} file not created.");
                }
            }
            catch
            {
                consoleWriter.CancelProgress();
                throw;
            }
        }

        static private string GetUntisPath()
        {
            var versions = new string[] { "2024", "2023", "2022", "2021", "2020", "2019", "2018", "2017" };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var consolePath = Environment.GetEnvironmentVariable("UNTIS_CONSOLEPATH");
                if (consolePath == null)
                {
                    foreach (var version in versions)
                    {
                        if (Environment.Is64BitProcess)
                        {
                            var key = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\WOW6432Node\\Gruber&Petters\\Untis {version}");
                            if (key != null)
                            {
                                return Path.Combine(key.GetValue("Install_Dir") as string, "Untis.exe");
                            }
                        }
                        else
                        {
                            var key = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\Gruber&Petters\\Untis {version}");
                            if (key != null)
                            {
                                return Path.Combine(key.GetValue("Install_Dir") as string, "Untis.exe");
                            }
                        }
                    }
                    throw new ConsoleException("Binary path not found. Untis does not seem to be installed.");
                }
                else
                {
                    return consolePath;
                }
            }
            else
            {
                throw new PlatformNotSupportedException("Untis does only run on Windows platform.");
            }
        }
    }
}
