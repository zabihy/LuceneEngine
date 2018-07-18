using ArmanQ.Models.Attributes;
using ArmanQ.Models.CacheModels;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ArmanQ.LuceneSearch
{
    public abstract class NewLuceneIndexer<TEntity, TCacheEntity, TGroup> : IDisposable 
        where TCacheEntity : BaseCacheEntity, new()
        where TGroup: GroupObject
    {
        private Type _type;
        private List<PropertyInfo> _keywordProps;
        private List<PropertyInfo> _tokenizedProps;
        private Lucene.Net.Index.IndexWriter _writer = null;
        private IHostingEnvironment _environment;
        private readonly string luceneIndexStoragePath;
        private readonly NewAnalyzerFactory _analyzerFactory;
        private readonly Analyzer _analyzer;

        public BaseLuceneIndexer(IHostingEnvironment environment, NewAnalyzerFactory analyzerFactory, string subPath)
        {
            _type = typeof(TCacheEntity);

            var props = _type.GetProperties();

            var keywordAttribute = typeof(KeywordAttribute);
            var tokenizedAttribute = typeof(TokenizedAttribute);

            _keywordProps = new List<PropertyInfo>(props.Where(p=>p.CustomAttributes.Any(ca=>ca.AttributeType==keywordAttribute)));
            _tokenizedProps = new List<PropertyInfo>(props.Where(p => p.CustomAttributes.Any(ca => ca.AttributeType == tokenizedAttribute)));

            _environment = environment;

            luceneIndexStoragePath = Path.Combine(_environment.ContentRootPath, "lucene", subPath);

            _analyzerFactory = analyzerFactory;

            _analyzer = analyzerFactory.Create<TCacheEntity>();
        }

        protected abstract void CreateDocument(IEnumerable<TCacheEntity> entities, IndexWriter indexWriter);

        private void CreateDoc(IEnumerable<TCacheEntity> entities, IndexWriter indexWriter)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            string emptyValue = string.Empty;

            foreach (var entity in entities)
            {
                //if(!DocumentExist(item))
                {
                    indexWriter.AddDocument(_Create(entity, emptyValue));
                }
            }
        }

        private IEnumerable<IIndexableField> _Create(TCacheEntity entity, string emptyValue)
        {
            try
            {
                CompleteGroupObject cGroupObject = new CompleteGroupObject();

                var classYearGroupObject = new ClassYearGroupObject();

                var authorIsArticleGroupObjects = new List<AuthorIsArticleGroupObject>();

                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();

                foreach (var item in _keywordProps)
                {
                    string name = item.Name.ToLower();

                    if(item.PropertyType==typeof(string))
                    {
                        var obj = item.GetValue(entity, null);

                        string value = obj!=null?obj.ToString():"";

                        doc.Add(new StringField(name, value, Field.Store.YES));
                    }
                    else
                        if(item.PropertyType.IsGenericType && item.PropertyType.GetGenericTypeDefinition()==typeof(IEnumerable<>))
                    {
                        foreach (var item2 in (System.Collections.IEnumerable)item.GetValue(entity, null))
                        {
                            string value = item2 != null ? item2.ToString() : "";

                            doc.Add(new StringField(name, value, Field.Store.YES));
                        }
                    }
                }
                doc.Add(new StringField(nameof(entity.ArticleTypeName).ToLower(), entity.ArticleTypeName, Field.Store.YES));
                doc.Add(new StringField(nameof(entity.ArticleId).ToLower(), entity.ArticleId.ToString(), Field.Store.YES));
                doc.Add(new StringField(nameof(entity.ArticleTypeId).ToLower(), entity.ArticleTypeId.ToString(), Field.Store.YES));

                foreach (long item in entity.AllAuthorIds)
                {
                    doc.Add(new StringField(nameof(entity.AllAuthorIds).ToLower(), item.ToString(), Field.Store.YES));
                }

                foreach (string item in entity.AuthorName)
                {
                    doc.Add(new StringField(nameof(entity.AuthorName).ToLower(), item, Field.Store.YES));
                }
                foreach (long item in entity.AuthorId)
                {
                    doc.Add(new StringField(nameof(entity.AuthorId).ToLower(), item.ToString(), Field.Store.YES));

                    authorIsArticleGroupObjects.Add(new AuthorIsArticleGroupObject());
                    var au = authorIsArticleGroupObjects.LastOrDefault();
                    au.AuthorId = item;
                    au.IsArticle = entity.IsArticle;
                }

                foreach (var item in entity.Authors)
                {
                    doc.Add(new StringField(nameof(entity.Authors).ToLower(), $"{item.Key}:{item.Value}", Field.Store.YES));
                }

                doc.Add(new StringField(nameof(entity.ClassId).ToLower(), entity.ClassId.ToString(), Field.Store.YES));
                cGroupObject.ClassId = entity.ClassId;
                cGroupObject.IsArticle = entity.IsArticle;
                classYearGroupObject.ClassId = entity.ClassId;

                if (entity.IsArticle == 1)
                {
                    doc.Add(new StringField(nameof(entity.MagazineId).ToLower(), entity.MagazineId.ToString(), Field.Store.YES));
                    cGroupObject.MagazineId = entity.MagazineId;
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.MagazineId).ToLower(), entity.SourceId.ToString(), Field.Store.YES));
                    cGroupObject.MagazineId = entity.SourceId.Value;
                }

                doc.Add(new StringField(nameof(entity.ClassName).ToLower(), entity.ClassName, Field.Store.YES));
                doc.Add(new StringField(nameof(entity.ClassNameFa).ToLower(), entity.ClassNameFa, Field.Store.YES));
                doc.Add(new StringField(nameof(entity.MagazineName).ToLower(), entity.MagazineName, Field.Store.YES));

                if (entity.CrawlHistoryId.HasValue)
                {
                    doc.Add(new StringField(nameof(entity.CrawlHistoryId).ToLower(), entity.CrawlHistoryId.ToString(), Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.CrawlHistoryId).ToLower(), 0.ToString(), Field.Store.YES));
                }
                doc.Add(new StringField(nameof(entity.DateCreated).ToLower(), DateTools.DateToString(entity.DateCreated, DateTools.Resolution.SECOND), Field.Store.YES));
                if (entity.DateUpdated.HasValue)
                {
                    doc.Add(new StringField(nameof(entity.DateUpdated).ToLower(), DateTools.DateToString(entity.DateUpdated.Value, DateTools.Resolution.SECOND), Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.DateUpdated).ToLower(), emptyValue, Field.Store.YES));
                }

                if (entity.GrandParentSourceId.HasValue)
                {
                    doc.Add(new StringField(nameof(entity.GrandParentSourceId).ToLower(), entity.GrandParentSourceId.ToString(), Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.GrandParentSourceId).ToLower(), 0.ToString(), Field.Store.YES));
                }

                doc.Add(new TextField(nameof(entity.GrandParentSourceName).ToLower(), entity.GrandParentSourceName, Field.Store.YES));
                doc.Add(new StringField(nameof(entity.IsArticle).ToLower(), entity.IsArticle.ToString(), Field.Store.YES));
                doc.Add(new StringField(nameof(entity.IsDeleted).ToLower(), entity.IsDeleted.ToString(), Field.Store.YES));
                doc.Add(new StringField(nameof(entity.ClassIsDeleted).ToLower(), entity.ClassIsDeleted.ToString(), Field.Store.YES));

                if (entity.MainArticleTypeId.HasValue)
                {
                    doc.Add(new StringField(nameof(entity.MainArticleTypeId).ToLower(), entity.MainArticleTypeId.ToString(), Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.MainArticleTypeId).ToLower(), 0.ToString(), Field.Store.YES));
                }

                if (entity.ParentSourceId.HasValue)
                {
                    doc.Add(new StringField(nameof(entity.ParentSourceId).ToLower(), entity.ParentSourceId.ToString(), Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.ParentSourceId).ToLower(), 0.ToString(), Field.Store.YES));
                }

                doc.Add(new TextField(nameof(entity.ParentSourceName).ToLower(), entity.ParentSourceName, Field.Store.YES));

                if (entity.PublicationDateText != null)
                {
                    doc.Add(new StringField(nameof(entity.PublicationDateText).ToLower(), entity.PublicationDateText, Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.PublicationDateText).ToLower(), entity.PublicationYear.ToString(), Field.Store.YES));
                }

                if (entity.PublicationYear.HasValue)
                {
                    doc.Add(new StringField(nameof(entity.PublicationYear).ToLower(), entity.PublicationYear.ToString(), Field.Store.YES));
                    cGroupObject.PublicationYear = entity.PublicationYear.Value;
                    classYearGroupObject.PublicationYear = entity.PublicationYear.Value;
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.PublicationYear).ToLower(), 0.ToString(), Field.Store.YES));
                    cGroupObject.PublicationYear = 0;
                    classYearGroupObject.PublicationYear = 0;
                }
                if (entity.SourceId.HasValue)
                {
                    doc.Add(new StringField(nameof(entity.SourceId).ToLower(), entity.SourceId.ToString(), Field.Store.YES));
                    cGroupObject.SourceId = entity.SourceId.Value;
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.SourceId).ToLower(), 0.ToString(), Field.Store.YES));
                    cGroupObject.SourceId = 0;
                }

                doc.Add(new TextField(nameof(entity.SourceName).ToLower(), entity.SourceName, Field.Store.YES));
                doc.Add(new TextField(nameof(entity.Title).ToLower(), entity.Title, Field.Store.YES));

                if (entity.University != null)
                {
                    doc.Add(new StringField(nameof(entity.University).ToLower(), entity.University, Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.University).ToLower(), emptyValue, Field.Store.YES));
                }

                if (entity.UniversityId.HasValue)
                {
                    doc.Add(new StringField(nameof(entity.UniversityId).ToLower(), entity.UniversityId.ToString(), Field.Store.YES));
                    cGroupObject.UniversityId = entity.UniversityId.Value;
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.UniversityId).ToLower(), 0.ToString(), Field.Store.YES));
                    cGroupObject.UniversityId = 0;
                }

                if (entity.AdvisorId.HasValue)
                {
                    doc.Add(new StringField(nameof(entity.AdvisorId).ToLower(), entity.AdvisorId.ToString(), Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.AdvisorId).ToLower(), 0.ToString(), Field.Store.YES));
                }

                if (entity.SupervisorId.HasValue)
                {
                    doc.Add(new StringField(nameof(entity.SupervisorId).ToLower(), entity.SupervisorId.ToString(), Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.SupervisorId).ToLower(), 0.ToString(), Field.Store.YES));
                }

                if (entity.AdvisorName != null)
                {
                    doc.Add(new StringField(nameof(entity.AdvisorName).ToLower(), entity.AdvisorName, Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.AdvisorName).ToLower(), emptyValue, Field.Store.YES));
                }

                if (entity.SuperVisorName != null)
                {
                    doc.Add(new StringField(nameof(entity.SuperVisorName).ToLower(), entity.SuperVisorName, Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.SuperVisorName).ToLower(), emptyValue, Field.Store.YES));
                }

                if (entity.Keywords != null)
                {
                    doc.Add(new TextField(nameof(entity.Keywords).ToLower(), entity.Keywords, Field.Store.YES));

                    foreach (var item in entity.Keywords.Split('/'))
                    {
                        doc.Add(new StringField(nameof(entity.Keyword).ToLower(), item, Field.Store.YES));
                    }
                }
                else
                {
                    doc.Add(new TextField(nameof(entity.Keywords).ToLower(), emptyValue, Field.Store.YES));
                }

                if (entity.Abstract != null)
                {
                    int byteSize = System.Text.ASCIIEncoding.Unicode.GetByteCount(entity.Abstract);

                    if (byteSize > 32766)
                    {
                        int maxLength = byteSize / 32766;
                        if (byteSize % 32766 > 0)
                        {
                            maxLength++;
                        }

                        for (int i = 0; i < maxLength; i++)
                        {
                            int start = ((i) * 16383);

                            int length = 16383;

                            if (start + length > entity.Abstract.Length)
                            {
                                length = entity.Abstract.Length - start;
                            }

                            var subString = entity.Abstract.Substring(start, length);

                            doc.Add(new TextField(nameof(entity.Abstract).ToLower(), subString, Field.Store.YES));

                            break;
                        }
                    }
                    else
                    {
                        doc.Add(new TextField(nameof(entity.Abstract).ToLower(), entity.Abstract, Field.Store.YES));
                    }
                }
                else
                {
                    doc.Add(new TextField(nameof(entity.Abstract).ToLower(), emptyValue, Field.Store.YES));
                }

                doc.Add(new StringField(typeof(CompleteGroupObject).Name.ToLower(), JsonConvert.SerializeObject(cGroupObject).Replace(" ", ""), Field.Store.YES));

                doc.Add(new StringField(typeof(ClassYearGroupObject).Name.ToLower(), JsonConvert.SerializeObject(classYearGroupObject).Replace(" ", ""), Field.Store.YES));

                foreach (var item in authorIsArticleGroupObjects)
                {
                    doc.Add(new StringField(typeof(AuthorIsArticleGroupObject).Name.ToLower(), JsonConvert.SerializeObject(item).Replace(" ", ""), Field.Store.YES));
                }

                return doc;
            }
            catch
            {
                if (entity != null)
                {
                    throw new Exception(JsonConvert.SerializeObject(entity));
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task Seed()
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

        public async Task StartAsync()
        {
            await Seed();
        }

        public abstract Task<IEnumerable<TCacheEntity>> GetFromDbAsync(int skip, int take);

        public abstract Task UpdateDbAsync<T>(IEnumerable<TCacheEntity> entities) where T : TEntity;


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

        private bool CreateIndex(IEnumerable<TCacheEntity> articles, Lucene.Net.Store.Directory directory)
        {
            if (articles.Count() <= 0)
            {
                return false;
            }

            IndexWriter writer = writer = new Lucene.Net.Index.IndexWriter(directory, new Lucene.Net.Index.IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, _analyzer));

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
