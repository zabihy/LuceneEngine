using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LuceneEngine.Core.Contracts;
using LuceneEngine.Core.Deserializers;
using LuceneEngine.Models.CacheModels;
using LuceneEngine.Models.Contracts;

namespace LuceneEngine.Core
{
    public interface ISearchProvider<TLuceneEntity> where TLuceneEntity : ILuceneEntity
    {
        TS GetResult<TS>() where TS : IBaseDeserialize;
        IEnumerable<KeyValuePair<T, int>> GroupBy<T>() where T : GroupObject;
        IEnumerable<KeyValuePair<string, int>> GroupBy<TResult>(Expression<Func<TLuceneEntity, TResult>> selector);
        SearchProvider<TLuceneEntity> Select<TResult>(Expression<Func<TLuceneEntity, TResult>> fieldSelector);
        SearchProvider<TLuceneEntity> SelectAll();
        int DocCount();
    }
}