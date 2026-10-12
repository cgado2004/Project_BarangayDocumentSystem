// ---------------------------------------------------------------------------
//  SqlArguments.cs and RowReader.cs - two small helpers that make the
//  database code short and, more importantly, safe.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;

namespace BarangayDocumentSystem.Data
{
    /// <summary>
    /// The values that go into one command, written as a list of name and
    /// value pairs.
    ///
    /// Why this instead of building a string like "WHERE last_name = '" + name
    /// + "'"? Because that is how SQL injection happens: a resident named
    /// "O'Brien" breaks the query, and somebody with worse intentions can read
    /// or destroy the whole database. Every value in this system travels as a
    /// parameter, never as part of the SQL text.
    /// </summary>
    public class SqlArguments
    {
        private readonly List<KeyValuePair<string, object>> _items = new List<KeyValuePair<string, object>>();

        public int Count { get { return _items.Count; } }

        public IList<KeyValuePair<string, object>> Items { get { return _items; } }

        public SqlArguments Add(string name, object value)
        {
            _items.Add(new KeyValuePair<string, object>(Normalise(name), value));
            return this;
        }

        /// <summary>Adds a value only when it is not empty, and returns true
        /// when it added something. I use this when a filter is optional, so
        /// the same query serves "all dates" and "this date".</summary>
        public bool AddIfSet(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            Add(name, value.Trim());
            return true;
        }

        public bool TryGet(string name, out object value)
        {
            string wanted = Normalise(name);
            foreach (KeyValuePair<string, object> item in _items)
            {
                if (string.Equals(item.Key, wanted, StringComparison.Ordinal))
                {
                    value = item.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public object Get(string name)
        {
            object value;
            return TryGet(name, out value) ? value : null;
        }

        /// <summary>MySQL and SQL Server both accept the @ prefix, but I keep
        /// this in one place in case I ever need to switch to the : prefix that
        /// other engines use.</summary>
        public static string Normalise(string name)
        {
            if (string.IsNullOrEmpty(name)) return "@p";
            return name[0] == '@' ? name : "@" + name;
        }
    }

    /// <summary>
    /// Reads a value out of a database row without the program falling over.
    ///
    /// The reason this exists: a NULL column read straight into a string
    /// throws, and a column that a teammate renamed later would throw too. I
    /// read every column through these methods, so one missing value shows up
    /// as an empty text box instead of a crash in the middle of the residents
    /// screen.
    /// </summary>
    public static class RowReader
    {
        public static bool HasColumn(DbDataReader reader, string column)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                if (string.Equals(reader.GetName(i), column, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        public static string GetString(DbDataReader reader, string column)
        {
            if (!HasColumn(reader, column)) return string.Empty;

            object value = reader[column];
            if (value == null || value == DBNull.Value) return string.Empty;
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public static int GetInt(DbDataReader reader, string column)
        {
            if (!HasColumn(reader, column)) return 0;

            object value = reader[column];
            if (value == null || value == DBNull.Value) return 0;
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        public static long GetLong(DbDataReader reader, string column)
        {
            if (!HasColumn(reader, column)) return 0L;

            object value = reader[column];
            if (value == null || value == DBNull.Value) return 0L;
            return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        public static decimal GetDecimal(DbDataReader reader, string column)
        {
            if (!HasColumn(reader, column)) return 0m;

            object value = reader[column];
            if (value == null || value == DBNull.Value) return 0m;
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        public static bool GetBool(DbDataReader reader, string column)
        {
            if (!HasColumn(reader, column)) return false;

            object value = reader[column];
            if (value == null || value == DBNull.Value) return false;

            if (value is bool) return (bool)value;

            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.Equals(text, "1", StringComparison.Ordinal)) return true;
            if (string.Equals(text, "True", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static DateTime GetDate(DbDataReader reader, string column)
        {
            if (!HasColumn(reader, column)) return default(DateTime);

            object value = reader[column];
            if (value == null || value == DBNull.Value) return default(DateTime);
            return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }

        public static DateTime? GetNullableDate(DbDataReader reader, string column)
        {
            if (!HasColumn(reader, column)) return null;

            object value = reader[column];
            if (value == null || value == DBNull.Value) return null;
            return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }

        public static int? GetNullableInt(DbDataReader reader, string column)
        {
            if (!HasColumn(reader, column)) return null;

            object value = reader[column];
            if (value == null || value == DBNull.Value) return null;
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Reads an enum that was stored by name, e.g. a status column holding
        /// "ReadyForRelease".
        ///
        /// I store enum values by name and never by number on purpose: if I
        /// reorder an enum next year, an old row must still mean the same
        /// thing. This method is the other half of that promise.
        /// </summary>
        public static T GetEnum<T>(DbDataReader reader, string column, T fallback) where T : struct
        {
            string text = GetString(reader, column);
            if (string.IsNullOrWhiteSpace(text)) return fallback;

            try
            {
                return (T)Enum.Parse(typeof(T), text.Trim(), true);
            }
            catch (ArgumentException)
            {
                return fallback;
            }
        }

        /// <summary>
        /// Reads a [Flags] enum stored as a number (the classifications).
        /// I keep this one numeric because a person can be several things at
        /// once and a list of names in one column would be a mess to query.
        /// </summary>
        public static T GetFlags<T>(DbDataReader reader, string column) where T : struct
        {
            int raw = GetInt(reader, column);
            return (T)Enum.ToObject(typeof(T), raw);
        }
    }
}
