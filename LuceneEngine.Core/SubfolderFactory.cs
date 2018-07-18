using LuceneEngine.Models.CacheModels;
using LuceneEngine.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuceneEngine.Core
{
    public class SubfolderFactory
    {
        public string GetFolderName<TLuceneEntity>()where TLuceneEntity:ILuceneEntity
        {
            Type type = typeof(TLuceneEntity);

            if ( type == typeof(ArticleCacheEntity))
            {
                return "article";
            }
            else if(type==typeof(AuthorCacheEntity))
            {
                return "author";
            }
            else
            {
                throw new NotSupportedException($"the type {type.Name} not supported");
            }
        }
    }
}
