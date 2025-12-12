using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using System.Text;
using Dapper;

namespace BaseRepository
{
    public static class DbContext
    {
        public static async Task<T> GetOne<T>(this IDbConnection connection, object param)
        {
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE Id = @Id";
            return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
        }
        
        public static async Task<IEnumerable<T>> Get<T>(this IDbConnection connection, object param)
        {
            string sql = $"SELECT * FROM {GetTableName<T>()}";
            return await connection.QueryAsync<T>(sql, param);
        }
        public static async Task<IEnumerable<T>> QueryIn<T>(this IDbConnection connection, int[] ints, string campo = "id")
        {
            if (ints.Length == 0)
                return Enumerable.Empty<T>();
            var values = string.Join(",", ints.Distinct());
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE {campo} in({values})";
            return await connection.QueryAsync<T>(sql, new { });
        }

        public static async Task<IEnumerable<T>> QueryIn<T>(this IDbConnection connection, DateTime[] ints, string campo = "id")
        {
            if (ints.Length == 0)
                return Enumerable.Empty<T>();
            var values = string.Join(",", ints.Distinct().Select(x => "'" + x.ToString("yyyyMMdd") + "'"));
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE {campo} in({values})";
            return await connection.QueryAsync<T>(sql);
        }
        public static async Task<IEnumerable<T>> QueryIn<T>(this IDbConnection connection, string[] ints, string campo = "id")
        {
            if (ints.Length == 0)
                return Enumerable.Empty<T>();
            var values = string.Join(",", ints.Distinct().Select(x => $"'{x}'"));
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE {campo} in({values})";
            return await connection.QueryAsync<T>(sql);
        }
        public static async Task<int> DeleteIn<T>(this IDbConnection connection, string[] ints, string campo = "id")
        {
            if (ints.Length == 0)
                return 0;
            var values = string.Join(",", ints.Distinct().Select(x => $"'{x}'"));
            string sql = $"DELETE FROM {GetTableName<T>()} WHERE {campo} in({values})";
            return await connection.ExecuteAsync(sql);
        }
        public static async Task<int> DeleteIn<T>(this IDbConnection connection, int[] ints, string campo = "id")
        {
            if (ints.Length == 0)
                return 0;
            var values = string.Join(",", ints.Distinct().Select(x => $"'{x}'"));
            string sql = $"DELETE FROM {GetTableName<T>()} WHERE {campo} in({values})";
            return await connection.ExecuteAsync(sql);
        }
        public static async Task<int> NewIdAsync<T>(this IDbConnection connection, string idColumn = "Id")
        {
            string sql = $"SELECT MAX({idColumn}) FROM {GetTableName<T>()}";
            var result = await connection.QuerySingleOrDefaultAsync<int>(sql);
            return Convert.ToInt32(result) + 1;
        }

        public static async Task<IEnumerable<T>> Get<T>(this IDbConnection connection, string expressao, object param)
        {
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE {expressao}";
            return await connection.QueryAsync<T>(sql, param);
        }
        public static async Task<T> QueryFirstOrDefaultAsync<T>(this IDbConnection connection, object param)
        {
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE Id = @Id";
            return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
        }
        public static async Task<T> QueryFirstOrDefaultAsync<T>(this IDbConnection connection, string expressao, object param)
        {
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE {expressao}";
            return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
        }

        public static async Task<int> InsertAsync<T>(this IDbConnection connection, T entity, string ignore = "Id")
        {
            ValidarObjeto(entity);
            string sql = GenSqlInsert<T>(ignore);
            return await connection.ExecuteAsync(sql, entity);
        }
        public static async Task<T> GetLastInsertedAsync<T>(this IDbConnection connection, string idColumn = "Id")
        {
            string sql = $@"
                SELECT * FROM {GetTableName<T>()} 
                WHERE {idColumn} = (SELECT MAX({idColumn}) FROM {GetTableName<T>()})";

            return await connection.QueryFirstOrDefaultAsync<T>(sql);
        }

        public static async Task<int> UpdateAsync<T>(this IDbConnection connection, T entity, string ignore = "Id")
        {
            ValidarObjeto(entity);
            string sql = GenSqlUpdate<T>(ignore);
            return await connection.ExecuteAsync(sql, entity);
        }

        public static async Task<int> Delete<T>(this IDbConnection connection, object param)
        {
            string sql = $"DELETE FROM {GetTableName<T>()} WHERE Id = @Id";
            return await connection.ExecuteAsync(sql, param);
        }
        private static string GetTableName<T>()
        {
            var atr = typeof(T).GetCustomAttribute<TableAttribute>();
            return atr == null ? typeof(T).Name : atr.Name;
        }

        private static string GetColumns<T>()
        {
            var columns = typeof(T).GetProperties().Select(p => p.Name);
            return string.Join(",", columns);
        }

        private static string GenSqlInsert<T>(string ignore = "")
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
        private static string GenSqlUpdate<T>(string ignore = "")
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

        public static async Task PrintColumnsTypeSQLSERVER(this IDbConnection connection, string tableName)
        {
            string sql = @"SELECT 
                        c.name AS NomeDaColuna,
                        t.name AS TipoDeDado,
                        c.max_length AS TamanhoMaximo
                        FROM sys.columns c
                        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                        WHERE c.object_id = OBJECT_ID('" + tableName + "') ORDER BY c.column_id;";
            var result = await connection.QueryAsync(sql);

            foreach (var item in result)
            {
                Console.WriteLine($"public {parseType(item.TipoDeDado)} {item.NomeDaColuna} {{get;set;}}");
            }
        }

        public static async Task PrintColumnsTypeMySQL(this IDbConnection connection, string tableName)
        {
            string sql = @"SELECT 
                        COLUMN_NAME AS NomeDaColuna,
                        DATA_TYPE AS TipoDeDado
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '" + tableName + "'ORDER BY ORDINAL_POSITION;";
            var result = await connection.QueryAsync(sql);

            foreach (var item in result)
            {
                Console.WriteLine($"public {parseType(item.TipoDeDado)} {item.NomeDaColuna} {{get;set;}}");
            }
        }

        public static async Task PrintColumnsTypeSQLite(this IDbConnection connection, string tableName)
        {
            string sql = @"PRAGMA table_info('" + tableName + ");";
            var result = await connection.QueryAsync(sql);

            foreach (var item in result)
            {
                Console.WriteLine($"public {parseType(item.type)} {item.name} {{get;set;}}");
            }
        }
        private static string parseType(string type)
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

        private static void ValidarObjeto(object obj)
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

    }
}