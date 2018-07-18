using ArmanQ.Core.Constants;
using ArmanQ.Core.Enums;
using ArmanQ.DataAccess;
using ArmanQ.DataAccess.Contracts;
using ArmanQ.Entities.Models;
using LuceneEngine.Models.CacheModels;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuceneEngine.Core
{
    public class ArticleIndexer : BaseLuceneIndexer<Article, ArticleCacheEntity>
    {
        public ArticleIndexer(IHostingEnvironment environment,
            IRepository<Article> repository,
            IServiceProvider serviceProvider,
            AnalyzerFactory analyzerFactory,
            IUnitOfWork uow) : base(environment, analyzerFactory, "article")
        {
            _environment = environment;
            _repository = repository;
            _serviceProvider = serviceProvider;
            _uow = uow;
        }

        private readonly IHostingEnvironment _environment;
        private readonly IRepository<Article> _repository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnitOfWork _uow;

        protected override async Task<IEnumerable<ArticleCacheEntity>> GetFromDbAsync(int skip, int take)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var repo = scope.ServiceProvider.GetRequiredService<IRepository<Article>>();

                    var entities = await repo
                                .AsQueryable()
                                    .AsNoTracking()
                                        .Where(p => !p.IsIndexed)
                                    .OrderBy(a => a.ArticleId)
                                    .Include(a => a.ArticleAuthors)
                                        .ThenInclude(aa => aa.Author)
                                    .Include(a => a.ArticleAuthors)
                                        .ThenInclude(aa => aa.AuthorType)
                                        .Include(a => a.ArticleType)
                                            .Include(a => a.CrawlHistory)
                                                .ThenInclude(ch => ch.Magazine)
                                                    .ThenInclude(m => m.Parent)
                                                    .Include(a => a.Class)
                                                .Include(a => a.University)
                                            .Skip(skip)
                                                .Take(take)
                                                    .ToListAsync();
                    return SelectEnumerable(entities);
                }

            }
            catch
            {
                throw;
            }
        }

        protected override async Task<IEnumerable<ArticleCacheEntity>> GetUpdatedFromDbAsync()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var repo = scope.ServiceProvider.GetRequiredService<IRepository<Article>>();

                    var entities = await repo
                                .AsQueryable()
                                    .AsNoTracking()
                                            .Where(p=>p.MustReIndex)
                                    .OrderBy(a => a.ArticleId)
                                    .Include(a => a.ArticleAuthors)
                                        .ThenInclude(aa => aa.Author)
                                    .Include(a => a.ArticleAuthors)
                                        .ThenInclude(aa => aa.AuthorType)
                                        .Include(a => a.ArticleType)
                                            .Include(a => a.CrawlHistory)
                                                .ThenInclude(ch => ch.Magazine)
                                                    .ThenInclude(m => m.Parent)
                                                    .Include(a => a.Class)
                                                .Include(a => a.University)
                                                    .ToListAsync();
                    return SelectEnumerable(entities);
                }
            }
            catch
            {
                throw;
            }
        }

        private IEnumerable<ArticleCacheEntity> SelectEnumerable(List<Article> entities)
        {
            return entities
                                            .Select(a => new ArticleCacheEntity
                                            {
                                                LuceneKey = a.ArticleId.ToString(),
                                                IsDeleted = a.IsDeleted ? 1 : 0,
                                                ClassIsDeleted = a.Class.IsDeleted ? 1 : 0,
                                                ArticleId = a.ArticleId,
                                                ClassId = a.ClassId.Value,
                                                ArticleTypeId = a.ArticleTypeId.Value,
                                                MainArticleTypeId = a.ArticleType.ParentId,
                                                PublicationDateText = a.PublicationDateText,
                                                PublicationYear = a.PublicationYear,
                                                SourceId = a.SourceId,
                                                Title = a.Title,
                                                University = a.University?.Name,
                                                UniversityId = a.UniId,
                                                AllAuthorIds = a.ArticleAuthors.Select(aa => aa.AuthorId),
                                                IsArticle = a.IsArticle ? 1 : 0,
                                                IsThesis = !a.IsArticle,
                                                ClassName = a.Class.Name,
                                                ClassNameFa = a.Class.NameFa,
                                                DateCreated = a.DateCreated,
                                                DateUpdated = a.DateUpdated,
                                                AuthorCount=a.AuthorCount,
                                                ArticleTypeName = a.ArticleType.NameFa,
                                                SourceName = a.CrawlHistory != null ? a.CrawlHistory.Magazine.NameFa : "",
                                                ParentSourceName = a.CrawlHistory != null && a.CrawlHistory.Magazine.Parent != null ? a.CrawlHistory.Magazine.Parent.Name : "",
                                                ParentSourceId = a.CrawlHistory != null ? a.CrawlHistory.Magazine.ParentId : null,
                                                GrandParentSourceName = a.CrawlHistory != null && a.CrawlHistory.Magazine.Parent != null && a.CrawlHistory.Magazine.Parent.Parent != null ? a.CrawlHistory.Magazine.Parent.Parent.Name : "",
                                                GrandParentSourceId = a.CrawlHistory != null && a.CrawlHistory.Magazine.Parent != null ? a.CrawlHistory.Magazine.Parent.ParentId : null,
                                                CrawlHistoryId = a.CrawlHistoryId,
                                                AuthorName = a.ArticleAuthors.Where(aa => aa.AuthorTypeId == 1 || aa.AuthorTypeId == 2).Select(aa => aa.Author.Name),
                                                AuthorId = a.ArticleAuthors.Where(aa => aa.AuthorTypeId == 1 || aa.AuthorTypeId == 2).Select(aa => aa.AuthorId),
                                                AdvisorId = a.ArticleAuthors.FirstOrDefault(aa => aa.AuthorType.Name == AuthorTypeLookup.Advisor)?.Author.AuthorId,
                                                AdvisorName = a.ArticleAuthors.FirstOrDefault(aa => aa.AuthorType.Name == AuthorTypeLookup.Advisor)?.Author.Name,
                                                SupervisorId = a.ArticleAuthors.FirstOrDefault(aa => aa.AuthorType.Name == AuthorTypeLookup.Supervisor)?.Author.AuthorId,
                                                SuperVisorName = a.ArticleAuthors.FirstOrDefault(aa => aa.AuthorType.Name == AuthorTypeLookup.Supervisor)?.Author.Name,
                                                Keywords = a.Keywords,
                                                Abstract = a.Abstract,
                                                Authors = a.ArticleAuthors.Where(aa => aa.AuthorTypeId == 1 || aa.AuthorTypeId == 2).Select(aa => new KeyValuePair<long, string>(aa.AuthorId, aa.Author.Name)),
                                                MagazineName = (a.IsArticle && a.CrawlHistory != null && a.CrawlHistory.Magazine.ParentId.HasValue) ? a.CrawlHistory.Magazine.Parent.NameFa : a.CrawlHistory != null ? a.CrawlHistory.Magazine.NameFa : "",
                                                MagazineId = (a.IsArticle && a.CrawlHistory != null && a.CrawlHistory.Magazine.ParentId.HasValue) ? a.CrawlHistory.Magazine.ParentId.Value : a.CrawlHistory != null ? a.CrawlHistory.MagazineId : 0
                                            });
        }

        protected override async Task UpdateDbAsync<Article>(IEnumerable<ArticleCacheEntity> entites)
        {
            await _uow.Bulk<Article>().Update(a => a.IsIndexed, a => a.ArticleId, entites.Select(a => a.ArticleId), true);
        }

        protected override async Task<bool> UpdateIndex(IEnumerable<ArticleCacheEntity> articles, Lucene.Net.Store.Directory directory)
        {
            bool result = await base.UpdateIndex(articles, directory);

            if(result)
            {
                await _uow.Bulk<Article>().Update(a => a.MustReIndex, a => a.ArticleId, articles.Select(a => a.ArticleId), false);
            }

            return result;
        }

        protected override void CreateDocument(IEnumerable<ArticleCacheEntity> entities, IndexWriter writer)
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
                    writer.AddDocument(Create(entity, emptyValue));
                }
            }
        }

        private IEnumerable<IIndexableField> Create(ArticleCacheEntity entity, string emptyValue)
        {
            try
            {
                CompleteGroupObject cGroupObject = new CompleteGroupObject();

                var classYearGroupObject = new ClassYearGroupObject();

                var authorIsArticleGroupObjects = new List<AuthorIsArticleGroupObject>();

                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();

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

                if(entity.IsArticle==1)
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
                if (entity.AuthorCount.HasValue)
                {
                    doc.Add(new StringField(nameof(entity.AuthorCount).ToLower(), entity.AuthorCount.ToString(), Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.AuthorCount).ToLower(), 0.ToString(), Field.Store.YES));
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

                doc.Add(new StringField(typeof(CompleteGroupObject).Name.ToLower(), JsonConvert.SerializeObject(cGroupObject).Replace(" ",""), Field.Store.YES));

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
    }

}
