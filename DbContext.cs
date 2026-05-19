using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using System.Text;
using Dapper;

namespace Sabino.BaseRepository
{
    public class DbContext : IDisposable
    {
        IDbConnection _connection;
        public DbContext(IDbConnection connection)
        {
            _connection = connection;
        }

        public IDbConnection GetConnection()
        {
            return _connection;
        }
        public async Task<IEnumerable<T>> QueryAsync<T>(object param)
        {
            string sql = $"SELECT * FROM {GetTableName<T>()}";
            return await _connection.QueryAsync<T>(sql, param);
        }
        public async Task<IEnumerable<T>> QueryIn<T>(int[] ints, string campo = "id")
        {
            if (ints.Length == 0)
                return Enumerable.Empty<T>();
            var values = string.Join(",", ints.Distinct());
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE {campo} in({values})";
            return await _connection.QueryAsync<T>(sql, new { });
        }

        public async Task<IEnumerable<T>> QueryIn<T>(DateTime[] ints, string campo = "id")
        {
            if (ints.Length == 0)
                return Enumerable.Empty<T>();
            var values = string.Join(",", ints.Distinct().Select(x => "'" + x.ToString("yyyyMMdd") + "'"));
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE {campo} in({values})";
            return await _connection.QueryAsync<T>(sql);
        }
        public async Task<IEnumerable<T>> QueryIn<T>(string[] ints, string campo = "id")
        {
            if (ints.Length == 0)
                return Enumerable.Empty<T>();
            var values = string.Join(",", ints.Distinct().Select(x => $"'{x}'"));
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE {campo} in({values})";
            return await _connection.QueryAsync<T>(sql);
        }
        public async Task<int> DeleteIn<T>(string[] ints, string campo = "id")
        {
            if (ints.Length == 0)
                return 0;
            var values = string.Join(",", ints.Distinct().Select(x => $"'{x}'"));
            string sql = $"DELETE FROM {GetTableName<T>()} WHERE {campo} in({values})";
            return await _connection.ExecuteAsync(sql);
        }
        public async Task<int> DeleteIn<T>(int[] ints, string campo = "id")
        {
            if (ints.Length == 0)
                return 0;
            var values = string.Join(",", ints.Distinct().Select(x => $"'{x}'"));
            string sql = $"DELETE FROM {GetTableName<T>()} WHERE {campo} in({values})";
            return await _connection.ExecuteAsync(sql);
        }
        public async Task<int> NewIdAsync<T>(string idColumn = "Id")
        {
            string sql = $"SELECT MAX({idColumn}) FROM {GetTableName<T>()}";
            var result = await _connection.QuerySingleOrDefaultAsync<int>(sql);
            return Convert.ToInt32(result) + 1;
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string expressao, object param)
        {
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE {expressao}";
            return await _connection.QueryAsync<T>(sql, param);
        }
        public async Task<T> QueryFirstOrDefaultAsync<T>(object param)
        {
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE Id = @Id";
            return await _connection.QueryFirstOrDefaultAsync<T>(sql, param);
        }
        public async Task<T> QueryFirstOrDefaultAsync<T>(string expressao, object param)
        {
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE {expressao}";
            return await _connection.QueryFirstOrDefaultAsync<T>(sql, param);
        }

        public async Task<int> InsertAsync<T>(T entity, string ignore = "Id")
        {
            ValidarObjeto(entity);
            string sql = GenSqlInsert<T>(ignore);

            return await _connection.ExecuteAsync(sql, entity);
        }

        public async Task<int> InsertAsync<T>(IEnumerable<T> entitys, string ignore = "Id")
        {
            string sql = GenSqlInsert<T>(ignore);
            var list = entitys.ToArray();
            return await _connection.ExecuteAsync(sql, list);
        }

        public async Task<int> InsertAsync<T>(IEnumerable<T> entitys, string ignore = "Id",int batchSize=1000)
        {
            string sql = GenSqlInsert<T>(entitys, ignoreColumn: ignore, batchSize:batchSize);
            return await _connection.ExecuteAsync(sql);
        }


        public async Task<T> GetLastInsertedAsync<T>(string idColumn = "Id")
        {
            string sql = $@"
                SELECT * FROM {GetTableName<T>()} 
                WHERE {idColumn} = (SELECT MAX({idColumn}) FROM {GetTableName<T>()})";

            return await _connection.QueryFirstOrDefaultAsync<T>(sql);
        }

        public async Task<int> UpdateAsync<T>(T entity, string ignore = "Id")
        {
            ValidarObjeto(entity);
            string sql = GenSqlUpdate<T>(ignore);
            return await _connection.ExecuteAsync(sql, entity);
        }

        public async Task<int> Delete<T>(object param)
        {
            string sql = $"DELETE FROM {GetTableName<T>()} WHERE Id = @Id";
            return await _connection.ExecuteAsync(sql, param);
        }
        private static string GetTableName<T>()
        {
            var atr = typeof(T).GetCustomAttribute<TableAttribute>();
            return atr == null ? typeof(T).Name : atr.Name;
        }

        private string GetColumns<T>()
        {
            var columns = typeof(T).GetProperties().Select(p => p.Name);
            return string.Join(",", columns);
        }

        private string GenSqlInsert<T>(string ignore = "")
        {
            var type = typeof(T);
            var properties = type.GetProperties();

            // Filtra propriedades que não são NotMapped
            var validColumns = properties
                .Where(p => !p.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute), true).Any())
                .Select(p => p.Name);

            // Adiciona colunas para ignorar manualmente
            string[] ignoreColumns = !string.IsNullOrEmpty(ignore) ? ignore.Split(',') : new string[0];

            var tableName = GetTableName<T>();
            var finalColumns = string.Join(",", validColumns.Where(c => !ignoreColumns.Contains(c)));

            return $"INSERT INTO {tableName} ({finalColumns}) VALUES ({string.Join(",", finalColumns.Split(',').Select(c => $"@{c}"))})";
        }

        public string GenSqlInsert<T>(IEnumerable<T> items, int batchSize = 1000, string ignoreColumn = "Id")
        {
            if (items == null || !items.Any())
                throw new ArgumentException("The items collection cannot be null or empty.");

            // Get table name from the first item
            var tableName = GetTableName<T>();

            // Get properties excluding NotMapped and ignored columns
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && 
                           !p.Name.Equals(ignoreColumn, StringComparison.OrdinalIgnoreCase) &&
                           !p.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute), true).Any())
                .ToList();

            if (!properties.Any())
                throw new InvalidOperationException($"No valid public properties found in type {typeof(T).Name}");

            var batches = items.Chunk(batchSize);
            var sqlBuilder = new StringBuilder();

            foreach (var batch in batches)
            {
                var columns = string.Join(", ", properties.Select(p => $"{p.Name}"));
                var values = new List<string>();

                foreach (var item in batch)
                {
                    var itemValues = properties.Select(p =>
                    {
                        var value = p.GetValue(item);
                        if (value == null)
                            return "NULL";

                        if (value is string)
                            return $"'{value.ToString().Replace("'", "\\'")}'";

                        if (value is DateTime dateTime)
                            return $"'{dateTime:yyyy-MM-dd HH:mm:ss}'";

                        if (value is Guid guid)
                            return $"'{guid}'";

                        if (value is bool)
                            return (bool)value ? "1" : "0";

                        if (value is decimal || value is double || value is float)
                            return value.ToString().Replace(",", ".");

                        return value.ToString();
                    });

                    values.Add($"({string.Join(", ", itemValues)})");
                }

                sqlBuilder.AppendLine($"INSERT INTO {tableName} ({columns})");
                sqlBuilder.AppendLine("VALUES");
                sqlBuilder.AppendLine(string.Join(",\n", values));
                sqlBuilder.AppendLine(";");
                sqlBuilder.AppendLine();
            }

            return sqlBuilder.ToString();
        }
        private string GenSqlUpdate<T>(string ignore = "")
        {
            var type = typeof(T);
            var properties = type.GetProperties();

            // Filtra propriedades que não são NotMapped
            var validColumns = properties
                .Where(p => !p.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute), true).Any())
                .Select(p => p.Name);

            // Adiciona colunas para ignorar manualmente
            string[] ignoreColumns = !string.IsNullOrEmpty(ignore) ? ignore.Split(',') : new string[0];

            var tableName = GetTableName<T>();
            var columnsUpdate = string.Join(",", validColumns
                .Where(c => !ignoreColumns.Contains(c))
                .Select(c => $"{c} = @{c}"));

            return $"UPDATE {tableName} SET {columnsUpdate} WHERE Id = @Id";
        }

        public async Task PrintColumnsTypeSQLSERVER(string tableName)
        {
            string sql = @"SELECT 
                        c.name AS NomeDaColuna,
                        t.name AS TipoDeDado,
                        c.max_length AS TamanhoMaximo
                        FROM sys.columns c
                        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                        WHERE c.object_id = OBJECT_ID('" + tableName + "') ORDER BY c.column_id;";
            var result = await _connection.QueryAsync(sql);

            foreach (var item in result)
            {
                Console.WriteLine($"public {parseType(item.TipoDeDado)} {item.NomeDaColuna} {{get;set;}}");
            }
        }

        public async Task PrintColumnsTypeMySQL(string tableName)
        {
            string sql = @"SELECT 
                        COLUMN_NAME AS NomeDaColuna,
                        DATA_TYPE AS TipoDeDado
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '" + tableName + "'ORDER BY ORDINAL_POSITION;";
            var result = await _connection.QueryAsync(sql);

            foreach (var item in result)
            {
                Console.WriteLine($"public {parseType(item.TipoDeDado)} {item.NomeDaColuna} {{get;set;}}");
            }
        }

        public async Task PrintColumnsTypeSQLite(string tableName)
        {
            string sql = @"PRAGMA table_info('" + tableName + ");";
            var result = await _connection.QueryAsync(sql);

            foreach (var item in result)
            {
                Console.WriteLine($"public {parseType(item.type)} {item.name} {{get;set;}}");
            }
        }
        private string parseType(string type)
        {
            switch (type)
            {
                case "int":
                    return "int";
                case "varchar":
                    return "string ?";
                case "datetime":
                    return "DateTime";
                case "bit":
                    return "bool";
                default:
                    return "string";
            }
        }

        private void ValidarObjeto(object obj)
        {
            var resultados = new List<ValidationResult>();
            var contexto = new ValidationContext(obj, serviceProvider: null, items: null);

            Validator.TryValidateObject(obj, contexto, resultados, validateAllProperties: true);

            StringBuilder stringBuilder = new StringBuilder("Campos Inválidos: ");
            if (resultados.Count() > 0)
            {
                foreach (var item in resultados)
                    stringBuilder.AppendLine(item.ErrorMessage);


                throw new Exception(stringBuilder.ToString());
            }
        }

        public void Dispose()
        {
            try
            {
                _connection.Close();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
    }
}
