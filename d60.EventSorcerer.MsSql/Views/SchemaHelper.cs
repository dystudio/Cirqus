﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace d60.EventSorcerer.MsSql.Views
{
    public class SchemaHelper
    {
        static readonly Dictionary<Type, Tuple<SqlDbType, string>> DbTypes = new Dictionary
            <Type, Tuple<SqlDbType, string>>
        {
            {typeof (long), Tuple.Create(SqlDbType.BigInt, "")},
            {typeof (int), Tuple.Create(SqlDbType.Int, "")},
            {typeof (string), Tuple.Create(SqlDbType.NVarChar, "max")},
            {typeof (decimal), Tuple.Create(SqlDbType.Decimal, "12,5")},
        };

        public static Prop[] GetSchema<TView>()
        {
            return typeof(TView)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new
                {
                    Property = p,
                    Attribute = p.GetCustomAttribute<ColumnAttribute>()
                })
                .Select(p =>
                {
                    var propertyInfo = p.Property;
                    var columnName = p.Attribute != null
                        ? p.Attribute.ColumnName
                        : propertyInfo.Name;

                    var sqlDbType = MapType(propertyInfo.PropertyType);
                    return new Prop
                    {
                        ColumnName = columnName,
                        SqlDbType = sqlDbType.Item1,
                        Size = sqlDbType.Item2
                    };
                })
                .ToArray();
        }

        static Tuple<SqlDbType, string> MapType(Type propertyType)
        {
            try
            {
                return DbTypes[propertyType];
            }
            catch (Exception exception)
            {
                throw new ArgumentException(string.Format("Could not map .NET type {0} to a proper SqlDbType", propertyType), exception);
            }
        }
    }

    public class Prop
    {
        public string ColumnName { get; set; }
        public Func<object> Getter { get; set; }
        public Action<object> Setter { get; set; }
        public SqlDbType SqlDbType { get; set; }
        public string Size { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", ColumnName, SqlDbType);
        }
    }

}