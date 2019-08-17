using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace MyNamespace
{
    /// <summary>
    /// General
    /// 
    /// DbManager v0.1a / author: xlower / release: 18 aug 2019
    /// 
    /// 
    /// How to use
    /// 
    /// 1. Install NuGet package Newtonsoft.Json
    /// 1. Create class
    /// 2. Mark it with [TableManagerAttribute(SchemeName, TableName)] (can skip)
    ///    If item 2 is omitted, then the class name is considered the name of the table
    /// 3. Make Connection.Configure() for create connection of type you want
    /// 4. Connection.Check() (can skip)
    /// 
    /// 
    /// Examples:
    /// 
    /// [TableManagerAttribute(TableName="test")] 
    /// public class Test{  public int Id { get; set; }  public string Name { get; set; }  }
    /// 
    /// DbManager.Connection.Configure( new MySqlConnection( connString ) );
    /// DbManager.InsertNewEntity<Test,MySqlCommand>(  new TestModel() { Id=0, Name="name0" }  );
    /// var listOfTestEntities = DbManager.SelectEntity<Test, MySqlCommand>();
    /// 
    /// 
    /// If you find bugs/errors or want to suggest something, then write to the mail: venick007@gmail.com
    /// </summary>
    public class DbManager
    {
        internal static void ExecuteCommands<TCommand>(List<TCommand> commands) where TCommand : DbCommand, new()
        {
            if (commands == null) throw new ArgumentNullException();
            foreach (var command in commands)
            {
                command.Connection = Connection.Conn ?? throw new NullReferenceException();
                Connection.Check();
                command.ExecuteNonQuery();
            }
        }

        internal static void ExecuteCommand<TCommand>(TCommand command) where TCommand : DbCommand, new()
        {
            using (command ?? throw new NullReferenceException())
            {
                Connection.Check();
                command.Connection = Connection.Conn ?? throw new NullReferenceException();
                var res = command.ExecuteNonQuery();
            }
        }

        internal static void ExecuteQuery<TCommand>(string Sql) where TCommand : DbCommand, new()
        {
            using (TCommand command = new TCommand())
            {
                command.CommandText = Sql ?? throw new NullReferenceException();
                command.Connection = Connection.Conn ?? throw new NullReferenceException();
                Connection.Check();
                command.ExecuteNonQuery();

            }
        }

        internal static void ExecuteQueries<TCommand>(List<string> Sqls) where TCommand : DbCommand, new()
        {
            if (Sqls == null) throw new ArgumentNullException();
            using (TCommand command = new TCommand())
            {
                command.Connection = Connection.Conn ?? throw new NullReferenceException();

                foreach (var sql in Sqls)
                {
                    command.CommandText = sql ?? throw new NullReferenceException();
                    Connection.Check();
                    command.ExecuteNonQuery();
                }    
            }
        }

        public static void InsertNewEntity<TEntity, TCommand>(TEntity obj) where TCommand : DbCommand, new()
        {
            if (obj==null) throw new ArgumentNullException();
            var TEntityProperties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? throw new NullReferenceException();
            var TEntityAttribute = (TableManagerAttribute)typeof(TEntity).GetCustomAttributes(typeof(TableManagerAttribute), false).FirstOrDefault() ?? throw new NullReferenceException();
            string values = "";
            string names = "";
            int i = 0;
            foreach (var prop in TEntityProperties)
            {
                names += (i > 0 ? ", " : "") + prop.Name;
                values += (i++ > 0 ? ", " : "") + JsonConvert.SerializeObject(prop.GetValue(obj, null));
            }
            var sql = $"insert into {TEntityAttribute.SchemeName}{(TEntityAttribute.SchemeName == null ? "" : ".")}{TEntityAttribute.TableName ?? typeof(TEntity).Name}({names})values({values.Replace("\"", "'")})";
            ExecuteQuery<TCommand>(sql);
        }

        public static void UpdateEntity<TEntity, TCommand>(TEntity obj, string Id) where TCommand : DbCommand, new()
        {
            if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException();
            var TEntityProperties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? throw new NullReferenceException();
            var TEntityAttribute = (TableManagerAttribute)typeof(TEntity).GetCustomAttributes(typeof(TableManagerAttribute), false).FirstOrDefault() ?? throw new NullReferenceException();
            string updates = "";
            int i = 0;
            foreach (var prop in TEntityProperties)
            {
                updates += (i++ > 0 ? ", " : "") + prop.Name + "=" + JsonConvert.SerializeObject(prop.GetValue(obj, null));
            }
            var sql = $"update {TEntityAttribute.SchemeName}{(TEntityAttribute.SchemeName == null ? "" : ".")}{TEntityAttribute.TableName ?? typeof(TEntity).Name ?? typeof(TEntity).Name} SET {updates.Replace("\"", "'")} WHERE ID={Id}";
            ExecuteQuery<TCommand>(sql);
        }

        public static void DeleteEntity<TEntity, TCommand>(string id) where TCommand : DbCommand, new()
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException();
            var TEntityAttribute = (TableManagerAttribute)typeof(TEntity).GetCustomAttributes(typeof(TableManagerAttribute), false).FirstOrDefault();
            var sql = $"delete from {TEntityAttribute.SchemeName}{(TEntityAttribute.SchemeName == null ? "" : ".")}{TEntityAttribute.TableName ?? typeof(TEntity).Name} where ID={id}";
            ExecuteQuery<TCommand>(sql);
        }

        public static List<TEntity> SelectEntity<TEntity, TCommand>() where TCommand : DbCommand, new()
        {
            List<TEntity> list = new List<TEntity>();
            using (TCommand command = new TCommand())
            {
                var TEntityAttribute = (TableManagerAttribute)typeof(TEntity).GetCustomAttributes(typeof(TableManagerAttribute), false).FirstOrDefault();
                command.Connection = Connection.Conn ?? throw new NullReferenceException();
                command.CommandType = CommandType.Text;
                command.CommandText = $"SELECT * FROM {TEntityAttribute.SchemeName}{(TEntityAttribute.SchemeName == null ? "" : ".")}{TEntityAttribute.TableName ?? typeof(TEntity).Name}";
                Connection.Check();
                var reader = command.ExecuteReader() ?? throw new NullReferenceException();
                int count = reader.FieldCount;
                var p0 = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                while (reader.Read())
                {
                    var ins = Activator.CreateInstance<TEntity>();
                    for (int i = 0; i < count; i++)
                    {
                        var p = p0.FirstOrDefault(x => x.Name.ToUpper() == reader.GetName(i).ToUpper());
                        p.SetValue(ins, Convert.ChangeType(Convert.ChangeType(reader.GetValue(i), p.PropertyType), p.PropertyType), null);
                    }
                    list.Add(ins);
                }
            }
            return list;
        }

        internal static string SelectCellValue<TCommand>(string Sql) where TCommand : DbCommand, new()
        {
            string retVal = "";
            using (TCommand command = new TCommand())
            {
                command.Connection = Connection.Conn ?? throw new NullReferenceException();
                command.CommandType = CommandType.Text;
                command.CommandText = Sql ?? throw new NullReferenceException();
                Connection.Check();
                DbDataReader reader = command.ExecuteReader();
                while (reader.Read())
                    retVal = reader[0].ToString();
            }
            return retVal;
        }

        public static class Connection
        {
            public static DbConnection Conn { get; set; }

            internal static DbConnection Configure(DbConnection _conn)
            {
                Conn = _conn ?? throw new ArgumentNullException();
                return _conn;
            }

            internal static DbConnection Configure<TDbConnection>(string ConnectionString) where TDbConnection : DbConnection, new()
            {
                var _conn = new TDbConnection
                {
                    ConnectionString = ConnectionString
                };
                Conn = _conn ?? throw new ArgumentNullException();
                return _conn;
            }

            internal static void Open() => Conn.Open();
            
            internal static void Close() => Conn.Close();
            
            internal static void Check()
            {
                if (Conn.State != ConnectionState.Open)
                    Conn.Open();
            }
        }

        public class TableManagerAttribute : Attribute
        {
            public TableManagerAttribute(string _SchemeName = null, string _TableName = null)
            {
                this.SchemeName = _SchemeName;
                this.TableName = _TableName;
            }
            public string SchemeName { get; set; }
            public string TableName { get; set; }
        }

    }

}
