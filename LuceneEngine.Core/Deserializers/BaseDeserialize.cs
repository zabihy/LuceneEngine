using Lucene.Net.Documents;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuceneEngine.Core.Deserializers
{
    public abstract class BaseDeserialize<T> : List<T>, IBaseDeserialize where T : class
    {
        public BaseDeserialize(IEnumerable<Document> documents)
        {

        }
        protected long ExtractLong(IIndexableField field)
        {
            return long.TryParse(field.GetStringValue(), out long id) ? id : 0;
        }

        protected int ExtractInt(IIndexableField field)
        {
            return int.TryParse(field.GetStringValue(), out int id) ? id : 0;
        }
    }

    public interface IBaseDeserialize
    {
    }

}
