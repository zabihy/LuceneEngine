using ArmanQ.DataAccess.Contracts;
using ArmanQ.Entities.Models;
using LuceneEngine.Models.Attributes;
using LuceneEngine.Models.CacheModels;
using LuceneEngine.Models.Contracts;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LuceneEngine.Core
{
    public abstract class BaseLuceneIndexer<TEntity, TLuceneEntity> : IDisposable where TLuceneEntity : BaseCacheEntity, new()
    {
        private Type _type;
        private List<PropertyInfo> _properties;
        private Lucene.Net.Index.IndexWriter _writer = null;
        private IHostingEnvironment _environment;
        private readonly string luceneIndexStoragePath;
        private readonly AnalyzerFactory _analyzerFactory;
        private readonly Analyzer _analyzer;

        public BaseLuceneIndexer(IHostingEnvironment environment, AnalyzerFactory analyzerFactory, string subPath)
        {
            _type = typeof(TLuceneEntity);

            _properties = new List<PropertyInfo>(_type.GetProperties());

            _environment = environment;

            luceneIndexStoragePath = Path.Combine(_environment.ContentRootPath, "lucene", subPath);

            _analyzerFactory = analyzerFactory;

            _analyzer = analyzerFactory.Create<TLuceneEntity>();
        }

        protected abstract void CreateDocument(IEnumerable<TLuceneEntity> entities, IndexWriter indexWriter);

        public async Task StartAsync()
        {
            bool loop = true;

            while (loop)
            {
                try
                {
                    var cached = await GetFromDbAsync(0, 1000);

                    if (cached.Count() <= 0)
                    {
                        loop = false;
                    }

                    else
                    {
                        var directory = ConfigDirectory();

                        CreateIndex(cached, directory);

                        await UpdateDbAsync<TEntity>(cached);
                        //_uow.Bulk.Update<Article>(cached.Select(c=>c.))
                    }
                }
                catch (Exception ex)
                {
                    await Task.Delay(1000);
                }
            }
        }

        protected abstract Task<IEnumerable<TLuceneEntity>> GetUpdatedFromDbAsync();
        public async Task UpdateAsync()
        {
            while (true)
            {
                try
                {
                    var cached = await GetUpdatedFromDbAsync();

                    if (cached.Count() <= 0)
                    {
                        await Task.Delay(10000);
                    }

                    else
                    {
                        var directory = ConfigDirectory();

                        await UpdateIndex(cached, directory);
                    }
                }
                catch (Exception ex)
                {
                    await Task.Delay(10000);
                }
            }
        }

        protected abstract Task<IEnumerable<TLuceneEntity>> GetFromDbAsync(int skip, int take);

        protected abstract Task UpdateDbAsync<T>(IEnumerable<TLuceneEntity> entities) where T : TEntity;


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
                if (_analyzer != null)
                    _analyzer.Dispose();
            }
            // free native resources if there are any.

        }

        private bool CreateIndex(IEnumerable<TLuceneEntity> articles, Lucene.Net.Store.Directory directory)
        {
            if (articles.Count() <= 0)
            {
                return false;
            }

            IndexWriter writer = new Lucene.Net.Index.IndexWriter(directory, new Lucene.Net.Index.IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, _analyzer));

            try
            {
                CreateDocument(articles, writer);
            }
            catch
            {
                Lucene.Net.Index.IndexWriter.Unlock(directory);
                throw;
            }
            finally
            {
                writer.Dispose();
            }
            return true;
        }

        protected virtual Task<bool> UpdateIndex(IEnumerable<TLuceneEntity> articles, Lucene.Net.Store.Directory directory)
        {
            if (articles.Count() <= 0)
            {
                return Task.FromResult(false);
            }

            var propertyInfo = typeof(TLuceneEntity).GetProperties().FirstOrDefault(p => p.CustomAttributes.Any(ca => ca.AttributeType == typeof(LuceneKeyAttribute)));
            string keyName = propertyInfo?.Name.ToString().ToLower();

            if(string.IsNullOrEmpty(keyName))
            {
                return Task.FromResult(true);
            }

            IndexWriter writer = new Lucene.Net.Index.IndexWriter(directory, new Lucene.Net.Index.IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, _analyzer));

            var tempQuery = new PhraseQuery();

            foreach (var item in articles)
            {
                string value = propertyInfo.GetValue(item).ToString();

                tempQuery.Add(new Term(keyName, value));
            }

            var boolQuery = new BooleanQuery();
            boolQuery.Add(tempQuery, Occur.MUST);

            var queryParser = new QueryParser(LuceneVersion.LUCENE_48, keyName, _analyzer);

            var query = queryParser.Parse(boolQuery.ToString());

            writer.DeleteDocuments(query);

            try
            {
                CreateDocument(articles, writer);
            }
            catch
            {
                Lucene.Net.Index.IndexWriter.Unlock(directory);
                throw;
            }
            finally
            {
                writer.Dispose();
            }
            return Task.FromResult(true);
        }

        private Lucene.Net.Store.FSDirectory ConfigDirectory()
        {
            if (!Directory.Exists(luceneIndexStoragePath))
            {
                Directory.CreateDirectory(luceneIndexStoragePath);
            }

            return Lucene.Net.Store.MMapDirectory.Open(new System.IO.DirectoryInfo(luceneIndexStoragePath));
        }
    }
}
