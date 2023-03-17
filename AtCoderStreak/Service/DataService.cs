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
        SavedSource[] GetSourcesByUrl(string url);
        SavedSource[] GetSources(SourceOrder order);
        void DeleteSources(IEnumerable<int> ids);

        void SaveSession(string cookie);
        string? GetSession();
    }
    public class DataService : IDataService
    {
        public DataService(string dbPath)
        {
            ConnectionString = new ConnectionString { Filename = dbPath };
        }

        private readonly ConnectionString ConnectionString;
        protected virtual LiteDatabase Connect()
        {
            return new LiteDatabase(ConnectionString);
        }

        public void SaveSession(string cookie)
        {
            using var db = Connect();
            db.BeginTrans();
            var col = db.GetCollection<Setting>();
            col.Upsert(Setting.Session(cookie));
            db.Commit();
        }

        public string? GetSession()
        {
            using var db = Connect();
            var col = db.GetCollection<Setting>();
            return col.FindById(new BsonValue(Setting.SessionId))?.Data;
        }

        public SavedSource[] GetSources(SourceOrder order = SourceOrder.None)
        {
            using var db = Connect();
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

            return ret.ToArray();
        }
        public SavedSource? GetSourceById(int id)
        {
            using var db = Connect();
            var col = db.GetCollection<Source>();
            return col.Query().Where(s => s.Id == id).FirstOrDefault()?.ToImmutable();
        }
        public SavedSource[] GetSourcesByUrl(string url)
        {
            using var db = Connect();
            var col = db.GetCollection<Source>();
            return col.Query().Where(s => s.TaskUrl == url).ToEnumerable().Select(s => s.ToImmutable()).ToArray();
        }
        public void SaveSource(Source source)
        {
            if (source.CompressedSourceCode.Length >= (1024 * 1024))
                throw new ArgumentException("source code is too long", nameof(source));

            using var db = Connect();
            var col = db.GetCollection<Source>();
            db.BeginTrans();
            col.Insert(source);
            db.Commit();
        }
        public void DeleteSources(IEnumerable<int> ids)
        {
            using var db = Connect();
            var col = db.GetCollection<Source>();
            db.BeginTrans();
            foreach (var id in ids)
            {
                col.Delete(new BsonValue(id));
            }
            db.Commit();
        }
    }

    public enum SourceOrder
    {
        None = 0,
        Reverse = 1,
    }

}
