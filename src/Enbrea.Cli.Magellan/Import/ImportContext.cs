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

using Enbrea.Ecf;
using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Enbrea.Cli.Magellan
{
    public class ImportContext
    {
        static private readonly Dictionary<Guid, string> _idMap = new();
        private readonly EcfTableReader _ecfTableReader;
        private readonly FbConnection _fbConnection;
        private readonly FbTransaction _fbTransaction;

        public ImportContext(
            FbConnection fbConnection, 
            FbTransaction fbTransaction, 
            EcfTableReader ecfTableReader)
        {
            _fbConnection = fbConnection;
            _fbTransaction = fbTransaction;
            _ecfTableReader = ecfTableReader;
        }

        public async Task LookupEntity(string tableName, string whereClause, Action<FbCommand> prepare, Func<int, Task> action)
        {
            string sql =
                $"""
                select 
                  "ID" 
                from 
                  "{tableName}"
                where
                  {whereClause}
                """;

            using var fbCommand = new FbCommand(sql, _fbConnection, _fbTransaction);

            prepare(fbCommand);

            var localId = (int?)await fbCommand.ExecuteScalarAsync();

            if (localId != null)
            {
                await action((int)localId);
            }
        }

        public async Task LookupEntityByCodeColumn(string tableName, Action<string> action)
        {
            if (_ecfTableReader.TryGetValue<string>(EcfHeaders.Id, out var code))
            {
                if (!string.IsNullOrEmpty(code))
                {
                    if (Guid.TryParse(code, out var globalId))
                    {
                        if (_idMap.TryGetValue(globalId, out var localCode))
                        {
                            action(localCode);
                        }
                    }
                    else 
                    {
                        if (await LookupCode(tableName, code) != null)
                        {
                            action(code);
                        }
                        else
                        {
                            throw new InvalidOperationException($"ECF record cannot be resolved for {EcfHeaders.Id}");
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"ECF record has no value for {EcfHeaders.Id}");
                }
            }
            else
            {
                throw new InvalidOperationException($"ECF record has no column {EcfHeaders.Id}");
            }
        }

        public async Task LookupEntityById(string tableName, string id, Action<int> action)
        {
            if (!string.IsNullOrEmpty(id))
            {
                if (Guid.TryParse(id, out var globalId))
                {
                    if (_idMap.TryGetValue(globalId, out var localIdAsString))
                    {
                        if (int.TryParse(localIdAsString, out var localId))
                        {
                            action(localId);
                        }
                    }
                }
                else if (int.TryParse(id, out var localId))
                {
                    if (await LookupId(tableName, localId) != null)
                    {
                        action(localId);
                    }
                    else
                    {
                        throw new InvalidOperationException($"ECF record cannot be resolved for id {id}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"ECF record cannot be resolved id {id}");
                }
            }
            else
            {
                throw new InvalidOperationException($"ECF record has no value for id {id}");
            }
        }

        public async Task LookupEntityById(string tableName, string id, Func<int, Task> action)
        {
            if (!string.IsNullOrEmpty(id))
            {
                if (Guid.TryParse(id, out var globalId))
                {
                    if (_idMap.TryGetValue(globalId, out var localIdAsString))
                    {
                        if (int.TryParse(localIdAsString, out var localId))
                        {
                            await action(localId);
                        }
                    }
                }
                else if (int.TryParse(id, out var localId))
                {
                    if (await LookupId(tableName, localId) != null)
                    {
                        await action(localId);
                    }
                    else
                    {
                        throw new InvalidOperationException($"ECF record cannot be resolved for id {id}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"ECF record cannot be resolved for id {id}");
                }
            }
            else
            {
                throw new InvalidOperationException($"ECF record has no value for id {id}");
            }
        }

        public async Task LookupEntityById(string tableName, string id, int tenantId, Action<int> action)
        {
            if (!string.IsNullOrEmpty(id))
            {
                if (Guid.TryParse(id, out var globalId))
                {
                    if (_idMap.TryGetValue(globalId, out var localIdAsString))
                    {
                        if (int.TryParse(localIdAsString, out var localId))
                        {
                            action(localId);
                        }
                    }
                }
                else if (int.TryParse(id, out var localId))
                {
                    if (await LookupId(tableName, tenantId, localId) != null)
                    {
                        action(localId);
                    }
                    else
                    {
                        throw new InvalidOperationException($"ECF record cannot be resolved for id {id}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"ECF record cannot be resolved for id {id}");
                }
            }
            else
            {
                throw new InvalidOperationException($"ECF record has no value for id {id}");
            }
        }

        public async Task LookupEntityById(string tableName, string id, int tenantId, Func<int, Task> action)
        {
            if (!string.IsNullOrEmpty(id))
            {
                if (Guid.TryParse(id, out var globalId))
                {
                    if (_idMap.TryGetValue(globalId, out var localIdAsString))
                    {
                        if (int.TryParse(localIdAsString, out var localId))
                        {
                            await action(localId);
                        }
                    }
                }
                else if (int.TryParse(id, out var localId))
                {
                    if (await LookupId(tableName, tenantId, localId) != null)
                    {
                        await action(localId);
                    }
                    else
                    {
                        throw new InvalidOperationException($"ECF record cannot be resolved for id {id}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"ECF record cannot be resolved for id {id}");
                }
            }
            else
            {
                throw new InvalidOperationException($"ECF record has no value for id {id}");
            }
        }

        public async Task LookupEntityByIdColumn(string headerName, string tableName, Action<int> action)
        {
            if (_ecfTableReader.TryGetValue<string>(headerName, out var id))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    if (Guid.TryParse(id, out var globalId))
                    {
                        if (_idMap.TryGetValue(globalId, out var localIdAsString))
                        {
                            if (int.TryParse(localIdAsString, out var localId))
                            {
                                action(localId);
                            }
                        }
                    }
                    else if (int.TryParse(id, out var localId))
                    {
                        if (await LookupId(tableName, localId) != null)
                        {
                            action(localId);
                        }
                        else
                        {
                            throw new InvalidOperationException($"ECF record cannot be resolved for {headerName}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"ECF record cannot be resolved for {headerName}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"ECF record has no value for {headerName}");
                }
            }
            else
            {
                throw new InvalidOperationException($"ECF record has no column {headerName}");
            }
        }

        public async Task LookupEntityByIdColumn(string headerName, string tableName, int tenantId, Action<int> action)
        {
            if (_ecfTableReader.TryGetValue<string>(headerName, out var id))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    if (Guid.TryParse(id, out var globalId))
                    {
                        if (_idMap.TryGetValue(globalId, out var localIdAsString))
                        {
                            if (int.TryParse(localIdAsString, out var localId))
                            {
                                action(localId);
                            }
                        }
                    }
                    else if (int.TryParse(id, out var localId))
                    {
                        if (await LookupId(tableName, tenantId, localId) != null)
                        {
                            action(localId);
                        }
                        else
                        {
                            throw new InvalidOperationException($"ECF record cannot be resolved for {headerName}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"ECF record cannot be resolved for {headerName}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"ECF record has no value for {headerName}");
                }
            }
            else
            {
                throw new InvalidOperationException($"ECF record has no column {headerName}");
            }
        }

        public async Task LookupEntityByIdColumn(string headerName, string tableName, int tenantId, Func<int, Task> action)
        {
            if (_ecfTableReader.TryGetValue<string>(headerName, out var id))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    if (Guid.TryParse(id, out var globalId))
                    {
                        if (_idMap.TryGetValue(globalId, out var localIdAsString))
                        {
                            if (int.TryParse(localIdAsString, out var localId))
                            {
                                await action(localId);
                            }
                        }
                    }
                    else if (int.TryParse(id, out var localId))
                    {
                        if (await LookupId(tableName, tenantId, localId) != null)
                        {
                            await action(localId);
                        }
                        else
                        {
                            throw new InvalidOperationException($"ECF record cannot be resolved for {headerName}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"ECF record cannot be resolved for {headerName}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"ECF record has no value for {headerName}");
                }
            }
            else
            {
                throw new InvalidOperationException($"ECF record has no column {headerName}");
            }
        }

        public async Task LookupEntityByIdColumn(string tableName, int tenantId, Func<int, Task> action)
        {
            if (_ecfTableReader.TryGetValue<string>(EcfHeaders.Id, out var id))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    if (Guid.TryParse(id, out var globalId))
                    {
                        if (_idMap.TryGetValue(globalId, out var localIdAsString))
                        {
                            if (int.TryParse(localIdAsString, out var localId))
                            {
                                await action(localId);
                            }
                        }
                    }
                    else if (int.TryParse(id, out var localId))
                    {
                        if (await LookupId(tableName, tenantId, localId) != null)
                        {
                            await action(localId);
                        }
                        else
                        {
                            throw new InvalidOperationException($"ECF record cannot be resolved for {EcfHeaders.Id}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"ECF record cannot be resolved for {EcfHeaders.Id}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"ECF record has no value for {EcfHeaders.Id}");
                }
            }
            else
            {
                throw new InvalidOperationException($"ECF record has no column {EcfHeaders.Id}");
            }
        }

        public async Task LookupEntityListById(string tableName, int tenantId, Func<int, Task> action)
        {
            if (_ecfTableReader.TryGetValue(EcfHeaders.Id, out List<string> idList))
            {
                foreach (var id in idList)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        if (Guid.TryParse(id, out var globalId))
                        {
                            if (_idMap.TryGetValue(globalId, out var localIdAsString))
                            {
                                if (int.TryParse(localIdAsString, out var localId))
                                {
                                    await action(localId);
                                }
                            }
                        }
                        else if (int.TryParse(id, out var localId))
                        {
                            if (await LookupId(tableName, tenantId, localId) != null)
                            {
                                await action(localId);
                            }
                            else
                            {
                                throw new InvalidOperationException($"ECF record cannot be resolved for {EcfHeaders.Id}");
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"ECF record cannot be resolved for {EcfHeaders.Id}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"ECF record has no value for {EcfHeaders.Id}");
                    }
                }
            }
        }

        public async Task LookupOrCreateEntity(string tableName, string whereClause, Action<FbCommand> prepare, Func<SqlInsertOrUpdate, Task> action)
        {
            if (await Lookup(tableName, whereClause, prepare))
            {
                await action(SqlInsertOrUpdate.Update);
            }
            else
            {
                await action(SqlInsertOrUpdate.Insert);
            }
        }

        public async Task LookupOrCreateEntity(string tableName, string whereClause, Action<FbCommand> prepare, Func<SqlInsertOrUpdate, int?, Task> action)
        {
            var localId = await LookupId(tableName, whereClause, prepare);

            if (localId != null)
            {
                await action(SqlInsertOrUpdate.Update, localId);
            }
            else
            {
                await action(SqlInsertOrUpdate.Insert, null);
            }
        }

        public async Task LookupOrCreateEntityByCodeColumn(string tableName, int maxLength, Func<SqlInsertOrUpdate, string, Task<string>> action)
        {
            if (_ecfTableReader.TryGetValue<string>(EcfHeaders.Id, out var id))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    if (Guid.TryParse(id, out var globalId))
                    {
                        if (_idMap.TryGetValue(globalId, out var localCode))
                        {
                            await action(SqlInsertOrUpdate.Update, localCode);
                        }
                        else
                        {
                            if (_ecfTableReader.TryGetValue<string>(EcfHeaders.Code, out var code))
                            {
                                code = ValueConverter.Limit(code, maxLength);
                                if (await LookupCode(tableName, code) != null)
                                {
                                    _idMap.TryAdd(globalId, await action(SqlInsertOrUpdate.Update, code));
                                }
                                else
                                {
                                    _idMap.TryAdd(globalId, await action(SqlInsertOrUpdate.Insert, code));
                                }
                            }
                        }
                    }
                    else
                    {
                        var code = ValueConverter.Limit(id, maxLength);
                        if (await LookupCode(tableName, code) != null)
                        {
                            await action(SqlInsertOrUpdate.Update, code);
                        }
                        else
                        {
                            await action(SqlInsertOrUpdate.Insert, code);
                        }
                    }
                }
            }
            else
            {
                throw new InvalidOperationException($"ECF record has no value for {EcfHeaders.Id}");
            }
        }

        public async Task LookupOrCreateEntityByCodeColumn(string tableName, int maxLength, int tenantId, Func<SqlInsertOrUpdate, string, Task<string>> action)
        {
            if (_ecfTableReader.TryGetValue<string>(EcfHeaders.Id, out var id))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    if (Guid.TryParse(id, out var globalId))
                    {
                        if (_idMap.TryGetValue(globalId, out var localCode))
                        {
                            await action(SqlInsertOrUpdate.Update, localCode);
                        }
                        else
                        {
                            if (_ecfTableReader.TryGetValue<string>(EcfHeaders.Code, out var code))
                            {
                                code = ValueConverter.Limit(code, maxLength);
                                if (await LookupCode(tableName, code) != null)
                                {
                                    _idMap.TryAdd(globalId, await action(SqlInsertOrUpdate.Update, code));
                                }
                                else
                                {
                                    _idMap.TryAdd(globalId, await action(SqlInsertOrUpdate.Insert, code));
                                }
                            }
                        }
                    }
                    else
                    {
                        var code = ValueConverter.Limit(id, maxLength);
                        if (await LookupCode(tableName, tenantId, code) != null)
                        {
                            await action(SqlInsertOrUpdate.Update, code);
                        }
                        else
                        {
                            await action(SqlInsertOrUpdate.Insert, code);
                        }
                    }
                }
            }
            else
            {
                throw new InvalidOperationException($"ECF record has no value for {EcfHeaders.Id}");
            }
        }

        public async Task LookupOrCreateEntityByIdColumn(string tableName, Func<SqlInsertOrUpdate, int?, Task<int>> action)
        {
            if (_ecfTableReader.TryGetValue<string>(EcfHeaders.Id, out var id))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    if (Guid.TryParse(id, out var globalId))
                    {
                        if (_idMap.TryGetValue(globalId, out var localIdAsString))
                        {
                            if (int.TryParse(localIdAsString, out var localId))
                            {
                                await action(SqlInsertOrUpdate.Update, localId);
                            }
                        }
                        else 
                        {
                            var localId = await LookupId(tableName, globalId);
                            if (localId != null)
                            {
                                _idMap.TryAdd(globalId, (await action(SqlInsertOrUpdate.Update, localId)).ToString());
                            }
                            else
                            {
                                _idMap.TryAdd(globalId, (await action(SqlInsertOrUpdate.Insert, null)).ToString());
                            }
                        }
                    }
                    else if (int.TryParse(id, out var localId))
                    {
                        if (await LookupId(tableName,localId) != null)
                        {
                            await action(SqlInsertOrUpdate.Update, localId);
                        }
                        else
                        {
                            await action(SqlInsertOrUpdate.Insert, localId);
                        }
                    }
                }
            }
            else
            {
                throw new InvalidOperationException($"ECF record has no value for {EcfHeaders.Id}");
            }
        }

        public async Task LookupOrCreateEntityByIdColumn(string tableName, int tenantId, Func<SqlInsertOrUpdate, int?, Task<int>> action)
        {
            if (_ecfTableReader.TryGetValue<string>(EcfHeaders.Id, out var id))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    if (Guid.TryParse(id, out var globalId))
                    {
                        if (_idMap.TryGetValue(globalId, out var localIdAsString))
                        {
                            if (int.TryParse(localIdAsString, out var localId))
                            {
                                await action(SqlInsertOrUpdate.Update, localId);
                            }
                        }
                        else
                        {
                            var localId = await LookupId(tableName, tenantId, globalId);
                            if (localId != null)
                            {
                                _idMap.TryAdd(globalId, (await action(SqlInsertOrUpdate.Update, localId)).ToString());
                            }
                            else
                            {
                                _idMap.TryAdd(globalId, (await action(SqlInsertOrUpdate.Insert, null)).ToString());
                            }
                        }
                    }
                    else if (int.TryParse(id, out var localId))
                    {
                        if (await LookupId(tableName, tenantId, localId) != null)
                        {
                            await action(SqlInsertOrUpdate.Update, localId);
                        }
                        else
                        {
                            await action(SqlInsertOrUpdate.Insert, localId);
                        }
                    }
                }
            }
            else
            {
                throw new InvalidOperationException($"ECF record has no value for {EcfHeaders.Id}");
            }
        }

        public void LookupValue<TValue>(string headerName, Action<TValue> action)
        {
            if (_ecfTableReader.TryGetValue(headerName, out TValue value))
            {
                action(value);
            }
        }

        public async Task LookupValue<TValue>(string headerName, Func<TValue, Task> action)
        {
            if (_ecfTableReader.TryGetValue(headerName, out TValue value))
            {
                await action(value);
            }
        }

        public void LookupValueOrDefault<TValue>(string headerName, TValue defaultValue, Action<TValue> action)
        {
            if (_ecfTableReader.TryGetValue(headerName, out TValue value))
            {
                action(value);
            }
            else
            {
                action(defaultValue);
            }
        }

        public async Task TryLookupEntityByCode(string headerName, string tableName, Action<string> action)
        {
            if (_ecfTableReader.TryGetValue<string>(headerName, out var code))
            {
                if (!string.IsNullOrEmpty(code))
                {
                    if (Guid.TryParse(code, out var globalId))
                    {
                        if (_idMap.TryGetValue(globalId, out var localCode))
                        {
                            action(localCode);
                        }
                    }
                    else
                    {
                        if (await LookupCode(tableName, code) != null)
                        {
                            action(code);
                        }
                        else
                        {
                            throw new InvalidOperationException($"ECF record cannot be resolved for {headerName}");
                        }
                    }
                }
                else
                {
                    action(null);
                }
            }
        }

        public async Task TryLookupEntityByCode2<TEntity>(string tableName, string code, Action<string> action)
        {
            if (!string.IsNullOrEmpty(code))
            {
                if (Guid.TryParse(code, out var globalId))
                {
                    if (_idMap.TryGetValue(globalId, out var localCode))
                    {
                        action(localCode);
                    }
                }
                else
                {
                    if (await LookupCode(tableName, code) != null)
                    {
                        action(code);
                    }
                    else
                    {
                        throw new InvalidOperationException($"ECF record cannot be resolved for code {code}");
                    }
                }
            }
        }
        
        public async Task TryLookupEntityById(string headerName, string tableName, int tenantId, Action<int?> action)
        {
            if (_ecfTableReader.TryGetValue<string>(headerName, out var id))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    if (Guid.TryParse(id, out var globalId))
                    {
                        if (_idMap.TryGetValue(globalId, out var localIdAsString))
                        {
                            if (int.TryParse(localIdAsString, out var localId))
                            {
                                action(localId);
                            }
                        }
                    }
                    else if (int.TryParse(id, out var localId))
                    {
                        if (await LookupId(tableName, tenantId, localId) != null)
                        {
                            action(localId);
                        }
                        else
                        {
                            throw new InvalidOperationException($"ECF record cannot be resolved for {headerName}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"ECF record cannot be resolved for {headerName}");
                    }
                }
                else
                {
                    action(null);
                }
            }
        }

        private async Task<string> LookupCode(string tableName, string code)
        {
            string sql =
                $"""
                select 
                  "Kuerzel" 
                from 
                  "{tableName}"
                """;

            if (Guid.TryParse(code, out var globalId))
            {
                if (_idMap.TryGetValue(globalId, out var localCode))
                {
                    return localCode;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                using var fbCommand = new FbCommand(sql, _fbConnection, _fbTransaction);
                return (string)await fbCommand.ExecuteScalarAsync();
            }
        }

        private async Task<string> LookupCode(string tableName, int tenantId, string code)
        {
            string sql =
                $"""
                select 
                  "Kuerzel" 
                from 
                  "{tableName}"
                where
                  "Mandant" = @tenantId and "Kuerzel" = @code
                """;

            if (Guid.TryParse(code, out var globalId))
            {
                if (_idMap.TryGetValue(globalId, out var localCode))
                {
                    return localCode;
                }
                else
                {
                    return null;
                }
            }
            else 
            {
                using var fbCommand = new FbCommand(sql, _fbConnection, _fbTransaction);

                fbCommand.Parameters.Add("@tenantId", tenantId);
                fbCommand.Parameters.Add("@code", code);

                return (string)await fbCommand.ExecuteScalarAsync();
            }
        }

        private async Task<int?> LookupId(string tableName, int id)
        {
            string sql =
                $"""
                select 
                  "ID" 
                from 
                  "{tableName}"
                where
                  "ID" = @id
                """;

            using var fbCommand = new FbCommand(sql, _fbConnection, _fbTransaction);

            fbCommand.Parameters.Add("@id", id);

            return (int?)await fbCommand.ExecuteScalarAsync();
        }

        private async Task<int?> LookupId(string tableName, int tenantId, int id)
        {
            string sql =
                $"""
                select 
                  "ID" 
                from 
                  "{tableName}"
                where
                  "Mandant" = @tenantId and "ID" = @id
                """;

            using var fbCommand = new FbCommand(sql, _fbConnection, _fbTransaction);

            fbCommand.Parameters.Add("@tenantId", tenantId);
            fbCommand.Parameters.Add("@id", id);

            return (int?)await fbCommand.ExecuteScalarAsync();
        }

        private async Task<int?> LookupId(string tableName, Guid globalId)
        {
            string sql =
                $"""
                select 
                  "ID" 
                from 
                  "{tableName}"
                where
                  "EnbreaId" = @globalId
                """;

            using var fbCommand = new FbCommand(sql, _fbConnection, _fbTransaction);

            fbCommand.Parameters.Add("@globalId", globalId);

            return (int?)await fbCommand.ExecuteScalarAsync();
        }

        private async Task<int?> LookupId(string tableName, int tenantId, Guid globalId)
        {
            string sql =
                $"""
                select 
                  "ID" 
                from 
                  "{tableName}"
                where
                  "Mandant" = @tenantId and "EnbreaId" = @globalId
                """;

            using var fbCommand = new FbCommand(sql, _fbConnection, _fbTransaction);

            fbCommand.Parameters.Add("@tenantId", tenantId);
            fbCommand.Parameters.Add("@globalId", globalId);

            return (int?)await fbCommand.ExecuteScalarAsync();
        }

        private async Task<bool> Lookup(string tableName, string whereClause, Action<FbCommand> prepare)
        {
            string sql =
                $"""
                select 
                  * 
                from 
                  "{tableName}"
                where
                  {whereClause}
                """;

            using var fbCommand = new FbCommand(sql, _fbConnection, _fbTransaction);

            prepare(fbCommand);

            return await fbCommand.ExecuteScalarAsync() != null;
        }

        private async Task<int?> LookupId(string tableName, string whereClause, Action<FbCommand> prepare)
        {
            string sql =
                $"""
                select 
                  "ID" 
                from 
                  "{tableName}"
                where
                  {whereClause}
                """;

            using var fbCommand = new FbCommand(sql, _fbConnection, _fbTransaction);

            prepare(fbCommand);

            return (int?)await fbCommand.ExecuteScalarAsync();
        }
    }
}