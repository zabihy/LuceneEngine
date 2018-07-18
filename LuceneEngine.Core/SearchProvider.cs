using LuceneEngine.Core.Contracts;
using LuceneEngine.Core.Deserializers;
using LuceneEngine.Models.Attributes;
using LuceneEngine.Models.CacheModels;
using LuceneEngine.Models.Contracts;
using LuceneEngine.Models.ReportModels;
using LuceneEngine.Models.ReportModels.GridModels;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;
using Lucene.Net.Search.Grouping.Terms;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LuceneEngine.Core
{
    public class SearchProvider<TLuceneEntity> : IDisposable, ISearchProvider<TLuceneEntity> where TLuceneEntity : ILuceneEntity
    {
        public SearchProvider(IHostingEnvironment environment,
            AnalyzerFactory analyzerFactory,
            string subFolder,
            ILuceneQueryable<TLuceneEntity> queryProvider)
        {
            _luceneDir = Path.Combine(environment.ContentRootPath, "lucene", subFolder);

            analyzer = analyzerFactory.Create<TLuceneEntity>();

            _indexReader = DirectoryReader.Open(_directory);

            searcher = new Lucene.Net.Search.IndexSearcher(DirectoryReader.Open(_directory));

            _queryProvider = queryProvider;
        }
        private string _luceneDir = null;
        IndexSearcher searcher;
        private readonly ILuceneQueryable<TLuceneEntity> _queryProvider;
        private Lucene.Net.Store.FSDirectory _directoryTemp;
        private Lucene.Net.Store.FSDirectory _directory
        {
            get
            {
                if (_directoryTemp == null) _directoryTemp = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(_luceneDir));
                if (Lucene.Net.Index.IndexWriter.IsLocked(_directoryTemp)) Lucene.Net.Index.IndexWriter.Unlock(_directoryTemp);
                var lockFilePath = System.IO.Path.Combine(_luceneDir, "write.lock");
                if (System.IO.File.Exists(lockFilePath)) System.IO.File.Delete(lockFilePath);
                return _directoryTemp;
            }
        }

        Analyzer analyzer = null;

        protected List<Document> _docs = new List<Document>();
        protected TopDocs _td;
        
        public TS GetResult<TS>() where TS : IBaseDeserialize
        {
            return (TS)Activator.CreateInstance(typeof(TS), _docs);
        }

        public SearchProvider<TLuceneEntity> SelectAll()
        {
            DoSearch(_queryProvider.GetSkip(), _queryProvider.GetPageSize(), out _td);

            for (int i = _queryProvider.GetSkip(); i < _queryProvider.GetSkip() + _queryProvider.GetPageSize() && i < _td.ScoreDocs.Length; i++)
            {
                Document doc = searcher.Doc(_td.ScoreDocs[i].Doc);
                _docs.Add(doc);
            }

            return this;
        }

        public SearchProvider<TLuceneEntity> Select<TResult>(Expression<Func<TLuceneEntity, TResult>> fieldSelector)
        {
            DoSearch(_queryProvider.GetSkip(), _queryProvider.GetPageSize(), out _td);

            var fieldsToLoad = new HashSet<string>();

            if (fieldSelector != null)
            {
                var fields = fieldSelector.GetListName();

                foreach (var item in fields)
                {
                    fieldsToLoad.Add(item.ToLower());
                }
            }

            if (fieldsToLoad == null || fieldsToLoad.Count <= 0)
            {
                for (int i = _queryProvider.GetSkip(); i < _queryProvider.GetSkip() + _queryProvider.GetPageSize() && i < _td.ScoreDocs.Length; i++)
                {
                    Document doc = searcher.Doc(_td.ScoreDocs[i].Doc);
                    _docs.Add(doc);
                }
            }

            else
            {
                for (int i = _queryProvider.GetSkip(); i < _queryProvider.GetSkip() + _queryProvider.GetPageSize() && i < _td.ScoreDocs.Length; i++)
                {
                    Document doc = searcher.Doc(_td.ScoreDocs[i].Doc, fieldsToLoad);
                    _docs.Add(doc);
                }
            }

            return this;
        }

        private DirectoryReader _indexReader;

        private void DoSearch(int skip, int pageSize, out TopDocs td)
        {
            // Open the IndexReader with readOnly=true. 
            // This makes a big difference when multiple threads are sharing the same reader, 
            // as it removes certain sources of thread contention.

            string rawQuery = CreateRawQuery();

            rawQuery = rawQuery.Replace("+()", "").Replace("()", "");

            if (!rawQuery.Contains("isdeleted"))
            {
                rawQuery += " +isdeleted:0";
            }
            var queryParser = new QueryParser(LuceneVersion.LUCENE_48, "isdeleted", analyzer);

            queryParser.AllowLeadingWildcard = _queryProvider.GetContainsWildCard();

            var query = queryParser.Parse(rawQuery);

            TopDocs tdTotal = searcher.Search(query, 2000000);

            Sort sort = _queryProvider.GetSort();

            if (sort != null)
            {
                td = searcher.Search(query, skip + pageSize, sort);

                sort = null;
            }
            else
            {
                td = searcher.Search(query, skip + pageSize);
            }

            var topHits = skip + pageSize;
        }

        private string CreateRawQuery()
        {
            return _queryProvider.GetBooleanQuery().ToString();
        }

        public IEnumerable<KeyValuePair<string, int>> GroupBy<TResult>(Expression<Func<TLuceneEntity, TResult>> selector)
        {
            string fieldName = selector.GetName();
            return _GroupBy(_queryProvider.GetSkip(), _queryProvider.GetPageSize(), fieldName);
        }

        private IEnumerable<KeyValuePair<string, int>> _GroupBy(int skip, int pageSize, string fieldName)
        {
            GroupingSearch groupingSearch = new GroupingSearch(fieldName);
            groupingSearch.SetGroupSort(Sort.RELEVANCE);
            groupingSearch.SetFillSortFields(false);
            groupingSearch.SetCachingInMB(40.0, true);
            groupingSearch.SetAllGroups(true);
            // Render groupsResult...
            try
            {
                var reader = DirectoryReader.Open(_directory);

                var searcher = new Lucene.Net.Search.IndexSearcher(reader);

                Sort groupSort = Sort.RELEVANCE;
                int groupOffset = 0;
                int groupLimit = 10000000;

                string rawQuery = _queryProvider.GetBooleanQuery().ToString();

                if (!rawQuery.Contains("isdeleted"))
                {
                    rawQuery += "+isdeleted:0";
                }
                var queryParser = new QueryParser(LuceneVersion.LUCENE_48, "isdeleted", analyzer);

                queryParser.AllowLeadingWildcard = _queryProvider.GetContainsWildCard();

                var query = queryParser.Parse(rawQuery);

                ITopGroups<object> result = groupingSearch.Search(searcher, query, groupOffset, groupLimit);

                if (result.Groups == null || result.Groups.Count() <= 0)
                {
                    return new List<KeyValuePair<string, int>>();
                }

                var d = result.Groups.OrderByDescending(p => p.TotalHits).ToList();

                if (d.FirstOrDefault().GroupValue == null)
                {
                    d.RemoveAt(0);
                }

                _groupCount = d.Count;

                if (pageSize > d.Count)
                {
                    pageSize = d.Count;
                }
                d = d.Skip(skip).Take(pageSize).ToList();

                if (d.Count > 0)
                {
                    var rs = d.Select(p => new KeyValuePair<string, int>(((BytesRef)p.GroupValue)?.Utf8ToString(), p.TotalHits)).ToList();

                    return rs;
                }
                else
                {
                    return new List<KeyValuePair<string, int>>();
                }
            }

            catch
            {
                throw;
            }

            finally
            {
            }
        }

        public IEnumerable<KeyValuePair<T, int>> GroupBy<T>() where T : GroupObject
        {
            var result = _GroupBy(_queryProvider.GetSkip(), _queryProvider.GetPageSize(), typeof(T).Name.ToLower());

            var output = new List<KeyValuePair<T, int>>();

            foreach (var item in result)
            {
                var grouped = JsonConvert.DeserializeObject<T>(item.Key);

                output.Add(new KeyValuePair<T, int>(grouped, item.Value));
            }

            return output;
        }

        private int _groupCount;

        public int DocCount()
        {
            string rawQuery = CreateRawQuery();

            rawQuery = rawQuery.Replace("+()", "").Replace("()", "");

            if (!rawQuery.Contains("isdeleted"))
            {
                rawQuery += " +isdeleted:0";
            }
            var queryParser = new QueryParser(LuceneVersion.LUCENE_48, "isdeleted", analyzer);

            queryParser.AllowLeadingWildcard = _queryProvider.GetContainsWildCard();

            var query = queryParser.Parse(rawQuery);

            TopDocs tdTotal = searcher.Search(query, 2000000);

            return tdTotal.TotalHits;
        }

        public int GroupCount()
        {
            return _groupCount;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (analyzer != null)
                    analyzer.Dispose();
            }
            // free native resources if there are any.
        }
    }
}
