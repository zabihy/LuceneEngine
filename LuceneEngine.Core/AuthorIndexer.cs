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
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuceneEngine.Core
{
    public class AuthorIndexer : BaseLuceneIndexer<Author, AuthorCacheEntity>
    {
        public AuthorIndexer(IHostingEnvironment environment,
            IServiceProvider serviceProvider,
            AnalyzerFactory analyzerFactory,
            IUnitOfWork uow) : base(environment, analyzerFactory, "author")
        {
            _environment = environment;
            _serviceProvider = serviceProvider;
            _uow = uow;
        }

        private readonly IHostingEnvironment _environment;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnitOfWork _uow;

        protected override async Task<IEnumerable<AuthorCacheEntity>> GetFromDbAsync(int skip, int take)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var repo = scope.ServiceProvider.GetRequiredService<IRepository<Author>>();

                    var authors = await repo
                                .AsQueryable()
                                    .AsNoTracking()
                                        .Where(p => !p.IsIndexed)
                                    .OrderBy(a => a.AuthorId)
                                            .Skip(skip)
                                                .Take(take)
                                                    .Select(a => new AuthorCacheEntity
                                                    {
                                                        LuceneKey = a.AuthorId.ToString(),
                                                        IsDeleted = a.IsDeleted?1:0,
                                                        SourceId = a.SourceId,
                                                        University = a.University,
                                                        AuthorId= a.AuthorId,
                                                        Name=a.Name
                                                    })
                                                    .ToListAsync();

                    return authors;
                }

            }
            catch
            {
                throw;
            }
        }

        protected override async Task UpdateDbAsync<Author>(IEnumerable<AuthorCacheEntity> entites)
        {
            await _uow.Bulk<Author>().Update(a => a.IsIndexed, a => a.AuthorId, entites.Select(a => a.AuthorId), true);
        }

        protected override void CreateDocument(IEnumerable<AuthorCacheEntity> entities, IndexWriter writer)
        {
            string emptyValue = string.Empty;

            foreach (var entity in entities)
            {
                //if(!DocumentExist(item))
                {
                    writer.AddDocument(Create(entity, emptyValue));
                }
            }
        }

        private IEnumerable<IIndexableField> Create(AuthorCacheEntity entity, string emptyValue)
        {
            try
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();

                doc.Add(new TextField(nameof(entity.Name).ToLower(), entity.Name, Field.Store.YES));
                doc.Add(new StringField(nameof(entity.AuthorId).ToLower(), entity.AuthorId.ToString(), Field.Store.YES));

                doc.Add(new StringField(nameof(entity.IsDeleted).ToLower(), entity.IsDeleted.ToString(), Field.Store.YES));

                if (entity.SourceId.HasValue)
                {
                    doc.Add(new StringField(nameof(entity.SourceId).ToLower(), entity.SourceId.ToString(), Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.SourceId).ToLower(), 0.ToString(), Field.Store.YES));
                }

                if (entity.University != null)
                {
                    doc.Add(new StringField(nameof(entity.University).ToLower(), entity.University, Field.Store.YES));
                }
                else
                {
                    doc.Add(new StringField(nameof(entity.University).ToLower(), emptyValue, Field.Store.YES));
                }

                return doc;
            }
            catch
            {
                throw;
            }
        }

        protected override Task<IEnumerable<AuthorCacheEntity>> GetUpdatedFromDbAsync()
        {
            throw new NotImplementedException();
        }
    }

}
