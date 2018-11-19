﻿using System;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Toolbox.SQL
{
    public class ClassInserter
    {
        public static void Insert(string connString, object classToInsert)
        {
            string tableName;
            string schemaName = "dbo";
            var classType = classToInsert.GetType();
            if (classType.CustomAttributes.Any(a => a.AttributeType == typeof(TableName)))
            {
                var schemaAttribute = classType.CustomAttributes.First(a => a.AttributeType == typeof(TableName)).ConstructorArguments[0].ToString();
                tableName = classType.CustomAttributes.First(a => a.AttributeType == typeof(TableName)).ConstructorArguments[1].ToString();
                if (!string.IsNullOrEmpty(schemaAttribute)) schemaName = schemaAttribute;
            }
            else
                tableName = classToInsert.GetType().Name;

            string insertInto = "INSERT INTO " + schemaName + "." + tableName + "(\n";
            insertInto = insertInto.Replace("\"", "");
            string values = "VALUES\n(";
            bool first = true;
            foreach (var info in classType.GetProperties())
            {
                if (info.CustomAttributes.Any(a => a.AttributeType == typeof(SkipInInserter))) continue;
                insertInto += first ? info.Name : "\n," + info.Name;
                values += first ? setQuotes(info,classToInsert) : "\n," + setQuotes(info,classToInsert).ToString();
                first = false;
            }

            var insertString = insertInto + ")\n" + values + ")";


            using (var conn = new SqlConnection(connString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = insertString;
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static string setQuotes(PropertyInfo info, object obj)
        {
            dynamic val = info.GetValue(obj);
            var pType = info.PropertyType;
            if (pType == typeof(string) || pType == typeof(char))
                val = "'" + val + "'";
            if (pType == typeof(DateTimeOffset) || pType == typeof(DateTime))
            {
                val = "'" + val.ToString("s") + "'";
            }
            if (pType == typeof(bool))
            {
                val = (bool) val ? "1" : "0";
            }

            return val.ToString();
        }
    }

    public class TableName : System.Attribute
    {
        private string _name;
        private string _schema;
        //private string _database;

        public TableName(string name, string schema = "")
        {
            this._name = name;
            this._schema = schema;
            //this._database = database;
        }
    }
    public class SkipInInserter : System.Attribute
    {
    }

}
