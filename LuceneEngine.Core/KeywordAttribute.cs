using System;
using System.Collections.Generic;
using System.Text;

namespace LuceneEngine.Core
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class KeywordAttribute:Attribute
    {
    }
}
