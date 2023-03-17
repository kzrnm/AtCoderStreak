using AtCoderStreak.Model;
using AtCoderStreak.Model.Entities;
using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AtCoderStreak.Service
{
    public interface IDataService
    {
        void SaveSource(Source source);
        SavedSource? GetSourceById(int id);
        IEnumerable<SavedSource> GetSourcesByUrl(string url);
        IEnumerable<SavedSource> GetSources(SourceOrder order);
        void DeleteSources(IEnumerable<int> ids);

        void SaveSession(string cookie);
        string? GetSession();
    }
    public class DataService : IDataService, IDisposable
    {
        private string DbPath { get; }
        public DataService(string dbPath)
        {
            DbPath = dbPath;
        }

        private const string MemoryKey = ":memory:";
        public static DataService Memory() => new(MemoryKey);


        private LiteDatabase? db;
        internal LiteDatabase Connect()
        {
            if (db != null)
                return db;

            return db = new LiteDatabase(new ConnectionString { Filename = DbPath });
        }

        public void SaveSession(string cookie)
        {
            var db = Connect();
            db.BeginTrans();
            var col = db.GetCollection<Setting>();
            col.Upsert(Setting.Session(cookie));
            db.Commit();
        }

        public string? GetSession()
        {
            var db = Connect();
            var col = db.GetCollection<Setting>();
            return col.FindById(new BsonValue(Setting.SessionId))?.Data;
        }

        public IEnumerable<SavedSource> GetSources(SourceOrder order = SourceOrder.None)
        {
            var db = Connect();
            var col = db.GetCollection<Source>();
            var ret = col.FindAll().Select(s => s.ToImmutable());

            if (order == SourceOrder.None)
            {
                ret = ret
                    .OrderByDescending(x => x.Priority)
                    .ThenBy(x => x.Id);
            }
            else if (order == SourceOrder.Reverse)
            {
                ret = ret
                    .OrderByDescending(x => x.Priority)
                    .ThenByDescending(x => x.Id);
            }
            else
                throw new InvalidEnumArgumentException(nameof(order), (int)order, typeof(SourceOrder));

            return ret;
        }
        public SavedSource? GetSourceById(int id)
        {
            var db = Connect();
            var col = db.GetCollection<Source>();
            return col.Query().Where(s => s.Id == id).FirstOrDefault()?.ToImmutable();
        }
        public IEnumerable<SavedSource> GetSourcesByUrl(string url)
        {
            var db = Connect();
            var col = db.GetCollection<Source>();
            return col.Query().Where(s => s.TaskUrl == url).ToEnumerable().Select(s => s.ToImmutable());
        }
        public void SaveSource(Source source)
        {
            if (source.CompressedSourceCode.Length >= (1024 * 1024))
                throw new ArgumentException("source code is too long", nameof(source));

            var db = Connect();
            var col = db.GetCollection<Source>();
            db.BeginTrans();
            col.Insert(source);
            db.Commit();
        }
        public void DeleteSources(IEnumerable<int> ids)
        {
            var db = Connect();
            var col = db.GetCollection<Source>();
            db.BeginTrans();
            foreach (var id in ids)
            {
                col.Delete(new BsonValue(id));
            }
            db.Commit();
        }

        public void Dispose()
        {
            db?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public enum SourceOrder
    {
        None = 0,
        Reverse = 1,
    }

}
