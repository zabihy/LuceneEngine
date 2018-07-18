using LuceneEngine.Core.Contracts;
using LuceneEngine.Models.CacheModels;
using LuceneEngine.Models.Contracts;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LuceneEngine.Core
{
    public class SearchEngine<TLuceneEntity> where TLuceneEntity : ILuceneEntity
    {
        private readonly IHostingEnvironment _env;
        private readonly AnalyzerFactory _analyzerFactory;
        private readonly SubfolderFactory _folderFactory;

        public SearchEngine(
            IHostingEnvironment environment,
            AnalyzerFactory analyzerFactory,
            SubfolderFactory folderFactory
            )
        {
            _env = environment;
            _analyzerFactory = analyzerFactory;
            _folderFactory = folderFactory;
        }

        public ILuceneQueryable<TLuceneEntity> AsLuceneQueryable()
        {
            return new QueryProvider<TLuceneEntity>(_env, _analyzerFactory, _folderFactory);
        }
    }
}
