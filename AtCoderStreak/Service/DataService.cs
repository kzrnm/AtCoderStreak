using AtCoderStreak.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;

namespace AtCoderStreak.Service
{
    public interface IDataService
    {
        void SaveSource(
            string url,
            string lang,
            int priority,
            byte[] fileBytes);
        SavedSource? GetSourceById(int id);
        IEnumerable<SavedSource> GetSourcesByUrl(string url);
        IEnumerable<SavedSource> GetSources(SourceOrder order);
        void DeleteSources(IEnumerable<int> ids);

        void SaveSession(string cookie);
        string? GetSession();
    }
#pragma warning disable CA1063
    public class DataService : IDataService, IDisposable
    {
        private string SqlitePath { get; }
        public DataService(string sqlitePath)
        {
            this.SqlitePath = sqlitePath;
        }

        private SQLiteConnection? connection;
        internal SQLiteConnection Connect()
        {
            if (connection != null)
                return connection;

            if (SqlitePath == ":memory:" || File.Exists(SqlitePath)) { }
            else
            {
                try
                {
                    using var file = File.Create(SqlitePath);
                }
                catch (Exception e)
                {
                    throw new FileNotFoundException("sqlite file doesn't exist", SqlitePath, e);
                }
            }

            var connsb = new SQLiteConnectionStringBuilder { DataSource = SqlitePath };
            var conn = new SQLiteConnection(connsb.ToString());
            conn.Open();
            using var command = conn.CreateCommand();
            command.CommandText = "CREATE TABLE IF NOT EXISTS SETTING(name TEXT PRIMARY KEY,data text)";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE TABLE IF NOT EXISTS program(id INTEGER PRIMARY KEY,TaskUrl text, LanguageId text, sourceCode blob, priority INTEGER NOT NULL DEFAULT 0)";
            command.ExecuteNonQuery();

            return connection = conn;
        }


        public void SaveSession(string cookie)
        {
            var conn = Connect();
            using var command = conn.CreateCommand();
            command.CommandText = "INSERT INTO SETTING(name,data) values('session', @cookie)";
            command.Parameters.Add(new SQLiteParameter("@cookie", cookie));
            command.ExecuteNonQuery();
        }

        public string? GetSession()
        {
            var conn = Connect();
            using var command = conn.CreateCommand();
            command.CommandText = "SELECT data FROM SETTING WHERE name = 'session'";
            using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;
            return reader.GetString(0);
        }

        public IEnumerable<SavedSource> GetSources(SourceOrder order = SourceOrder.None)
        {
            const string defaultCommand = "SELECT * FROM program";
            string commandOrder;

            var conn = Connect();
            using var command = conn.CreateCommand();
            if (order == SourceOrder.None)
            {
                commandOrder = "ORDER BY priority desc, id";
            }
            else if (order == SourceOrder.Reverse)
            {
                commandOrder = "ORDER BY priority desc, id desc";
            }
            else
                throw new InvalidEnumArgumentException(nameof(order), (int)order, typeof(SourceOrder));
            command.CommandText = $"{defaultCommand} {commandOrder}";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                yield return SavedSource.FromReader(reader);
            }
        }
        public SavedSource? GetSourceById(int id)
        {
            var conn = Connect();
            using var command = conn.CreateCommand();
            command.CommandText = "SELECT * FROM program WHERE id = @id";
            command.Parameters.Add(new SQLiteParameter("@id", id));

            using var reader = command.ExecuteReader();
            return reader.Read() ? SavedSource.FromReader(reader) : null;

        }
        public IEnumerable<SavedSource> GetSourcesByUrl(string url)
        {
            var conn = Connect();
            using var command = conn.CreateCommand();
            command.CommandText = "SELECT * FROM program WHERE TaskUrl = @url";
            command.Parameters.Add(new SQLiteParameter("@url", url));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                yield return SavedSource.FromReader(reader);
            }
        }
        public void SaveSource(
            string url,
            string lang,
            int priority,
            byte[] fileBytes)
        {
            if (fileBytes.Length > (512 << 10))
                throw new ArgumentException("source code is too long", nameof(fileBytes));

            var conn = Connect();
            using var command = conn.CreateCommand();
            command.CommandText = "INSERT INTO program(TaskUrl,LanguageId,sourceCode, priority) values(@url, @lang, @source, @priority)";
            command.Parameters.Add(new SQLiteParameter("@url", url));
            command.Parameters.Add(new SQLiteParameter("@lang", lang));
            command.Parameters.Add(new SQLiteParameter("@priority", priority));
            using var ms = new MemoryStream(512 * 1024);
            using (var gz = new GZipStream(ms, CompressionMode.Compress))
                gz.Write(fileBytes);
            var gzSource = ms.ToArray();

            command.Parameters.Add(new SQLiteParameter("@source", gzSource));
            command.ExecuteNonQuery();
        }
        public void DeleteSources(IEnumerable<int> ids)
        {
            var conn = Connect();
            using var tr = conn.BeginTransaction();
            foreach (var id in ids)
            {
                using var command = conn.CreateCommand();
                command.CommandText = "DELETE FROM program where id = @id";
                command.Parameters.Add(new SQLiteParameter("@id", id));
                command.ExecuteNonQuery();
            }
            tr.Commit();
        }

        public void Dispose()
        {
            this.connection?.Dispose();
            GC.SuppressFinalize(this);
        }

#pragma warning restore
    }

    public enum SourceOrder
    {
        None = 0,
        Reverse = 1,
    }

}
