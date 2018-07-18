using LuceneEngine.Models.CacheModels;
using LuceneEngine.Models.Contracts;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace LuceneEngine.Core.Contracts
{
    public interface ILuceneQueryable<TLuceneEntity>: ICommander<ISearchProvider<TLuceneEntity>> where TLuceneEntity : ILuceneEntity
    {
        QueryProvider<TLuceneEntity> Close();
        QueryProvider<TLuceneEntity> DoubleRangeQuery<TResult>(Occur occur, Expression<Func<TLuceneEntity, TResult>> selector, double min, double max);
        BooleanQuery GetBooleanQuery();
        bool GetContainsWildCard();
        int GetPageSize();
        int GetSkip();
        Sort GetSort();
        QueryProvider<TLuceneEntity> IncludePhrase<TResult>(string term, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur);
        QueryProvider<TLuceneEntity> IncludeWildCard<TResult>(string term, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur);
        QueryProvider<TLuceneEntity> IntRangeQuery<TResult>(Occur occur, Expression<Func<TLuceneEntity, TResult>> selector, int min, int max);
        QueryProvider<TLuceneEntity> LongRangeQuery<TResult>(Occur occur, Expression<Func<TLuceneEntity, TResult>> selector, long min, long max);
        QueryProvider<TLuceneEntity> Open(Occur occur);
        QueryProvider<TLuceneEntity> OrderBy(string fieldName);
        QueryProvider<TLuceneEntity> OrderBy<TResult>(Expression<Func<TLuceneEntity, TResult>> selector);
        QueryProvider<TLuceneEntity> OrderByDesc(string fieldName);
        QueryProvider<TLuceneEntity> OrderByDesc<TResult>(Expression<Func<TLuceneEntity, TResult>> selector);
        QueryProvider<TLuceneEntity> OrderByRelevance();
        QueryProvider<TLuceneEntity> Phrase<TResult>(bool value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur);
        QueryProvider<TLuceneEntity> Phrase<TResult>(double value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur);
        QueryProvider<TLuceneEntity> Phrase<TResult>(IEnumerable<double> value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur);
        QueryProvider<TLuceneEntity> Phrase<TResult>(IEnumerable<int> value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur);
        QueryProvider<TLuceneEntity> Phrase<TResult>(IEnumerable<long> value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur);
        QueryProvider<TLuceneEntity> Phrase<TResult>(IEnumerable<string> value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur);
        QueryProvider<TLuceneEntity> Phrase<TResult>(int value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur);
        QueryProvider<TLuceneEntity> Phrase<TResult>(long value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur);
        QueryProvider<TLuceneEntity> Phrase<TResult>(string value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur);
        QueryProvider<TLuceneEntity> Skip(int skip);
        QueryProvider<TLuceneEntity> Take(int take);
    }
}
