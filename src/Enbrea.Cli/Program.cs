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
using System.CommandLine;
using System.Threading.Tasks;

namespace Enbrea.Cli
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // Conole window title
            Console.Title = AssemblyInfo.GetTitle();

            // Build up command line api
            var rootCommand = new RootCommand(description: "Tool for synchronizing external data provider with Enbrea")
            {
                CommandDefinitions.Init(),
                CommandDefinitions.Export(),
                CommandDefinitions.Import(),
                CommandDefinitions.ListSchoolTerms(),
                CommandDefinitions.CreateSnaphot(),
                CommandDefinitions.RestoreSnaphot(),
                CommandDefinitions.DeleteSnaphot(),
                CommandDefinitions.ListSnaphots(),
                CommandDefinitions.CreateImportTask(),
                CommandDefinitions.EnableImportTask(),
                CommandDefinitions.DisableImportTask(),
                CommandDefinitions.CreateExportTask(),
                CommandDefinitions.EnableExportTask(),
                CommandDefinitions.DisableExportTask(),
                CommandDefinitions.DeleteImportTask(),
                CommandDefinitions.DeleteExportTask(),
                CommandDefinitions.DeleteAllTasks(),
                CommandDefinitions.ListAllTasks(),
                CommandDefinitions.Backup()
            };

            // Parse the incoming args and invoke the handler
            var parseResult = rootCommand.Parse(args);
            return await parseResult.InvokeAsync();
        }
    }
}
