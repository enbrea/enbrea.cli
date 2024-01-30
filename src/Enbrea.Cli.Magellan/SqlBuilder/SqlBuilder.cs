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

using System.Collections.Generic;
using System.Text;

namespace Enbrea.Cli.Magellan
{
    public class SqlBuilder(string tableName)
    {
        private readonly List<SqlAssigment> _assignments = [];
        private readonly SqlParameterNameGenerator _parameterNameGenerator = new();
        private readonly string _tableName = tableName;

        public List<SqlAssigment> Assignments
        { 
            get { return _assignments; }
        }

        public string AsDelete(string whereClause)
        {
            var strBuilder = new StringBuilder();

            strBuilder.Append("DELETE");
            strBuilder.Append(' ');
            strBuilder.Append("FROM");
            strBuilder.Append(' ');
            strBuilder.Append($"{_tableName}");

            if (!string.IsNullOrEmpty(whereClause))
            {
                strBuilder.Append(' ');
                strBuilder.Append("WHERE");
                strBuilder.Append(' ');
                strBuilder.Append(whereClause);
            }

            return strBuilder.ToString();
        }

        public string AsInsert()
        {
            var strBuilder = new StringBuilder();

            strBuilder.Append("INSERT");
            strBuilder.Append(' ');
            strBuilder.Append("INTO");
            strBuilder.Append(' ');
            strBuilder.Append($"{_tableName}");
            strBuilder.Append(' ');
            strBuilder.Append('(');
            strBuilder.Append(GetColumnNames());
            strBuilder.Append(')');
            strBuilder.Append(' ');
            strBuilder.Append("VALUES");
            strBuilder.Append(' ');
            strBuilder.Append('(');
            strBuilder.Append(GetParamNames());
            strBuilder.Append(')');

            return strBuilder.ToString();
        }

        public string AsInsertOrUpdate(SqlInsertOrUpdate insertOrUpdate, string whereClause)
        {
            if (insertOrUpdate == SqlInsertOrUpdate.Insert) 
                return AsInsert();
            else 
                return AsUpdate(whereClause);
        }

        public string AsUpdate(string whereClause)
        {
            var strBuilder = new StringBuilder();

            strBuilder.Append("UPDATE");
            strBuilder.Append(' ');
            strBuilder.Append($"{_tableName}");
            strBuilder.Append(' ');
            strBuilder.Append("SET");
            strBuilder.Append(' ');
            strBuilder.Append(GetColumnAndParamNames());

            if (!string.IsNullOrEmpty(whereClause))
            {
                strBuilder.Append(' ');
                strBuilder.Append("WHERE");
                strBuilder.Append(' ');
                strBuilder.Append(whereClause);
            }

            return strBuilder.ToString();
        }

        public void SetValue(string fieldName, object value)
        {
            var paramName = _parameterNameGenerator.GenerateNext();
            _assignments.Add(new SqlAssigment(fieldName, paramName, value));
        }
        
        private string GetColumnAndParamNames()
        {
            var strBuilder = new StringBuilder();

            foreach (var assignments in _assignments)
            {
                if (strBuilder.Length > 0) strBuilder.Append(',');
                strBuilder.Append($"\"{assignments.FieldName}\" = {assignments.ParamName}");
            }

            return strBuilder.ToString();
        }

        private string GetColumnNames()
        {
            var strBuilder = new StringBuilder();

            foreach (var assignments in _assignments)
            {
                if (strBuilder.Length > 0) strBuilder.Append(',');
                strBuilder.Append($"\"{assignments.FieldName}\"");
            }

            return strBuilder.ToString();
        }

        private string GetParamNames()
        {
            var strBuilder = new StringBuilder();

            foreach (var assignments in _assignments)
            {
                if (strBuilder.Length > 0) strBuilder.Append(',');
                strBuilder.Append($"@{assignments.ParamName}");
            }

            return strBuilder.ToString();
        }
    }
}
