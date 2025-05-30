using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

public static class SQLiteManager
{
    private static readonly string dbFile = "sqlite.db";

    public static int SqlWrite(string sql)
    {
        using var conn = new SqliteConnection($"Data source={dbFile}");
        conn.Open();

        var command = conn.CreateCommand();
        command.CommandText = sql;

        return command.ExecuteNonQuery();
    }

    public static Dictionary<int, Dictionary<string, object>> SqlRead(string sql)
    {
        using var conn = new SqliteConnection($"Data source={dbFile}");
        conn.Open();

        var command = conn.CreateCommand();
        command.CommandText = sql;

        Dictionary<int, Dictionary<string, object>> resultSet = [];

        using (var reader = command.ExecuteReader())
        {
            int index = 0;

            while (reader.Read())
            {
                resultSet.Add(index, []);

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    resultSet[index].Add(reader.GetName(i), reader.IsDBNull(i) ? null : Convert.ChangeType(reader.GetString(i), reader.GetFieldType(i)));
                }

                ++index;
            }
        }

        return resultSet;
    }
}
