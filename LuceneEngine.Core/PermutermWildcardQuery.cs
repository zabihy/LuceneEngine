using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LuceneEngine.Core
{
    public class PermutermWildcardQuery : MultiTermQuery
    {
        private Term _term;
        public PermutermWildcardQuery(Term term) : base(term.Field)
        {
            this._term = term;
        }

        public override string ToString(string field)
        {
            var s = string.Join(" ", _getTermsEnum().Select(p => p.Text().Contains("$")?$"{p}*":$"{p}").ToArray());

            return s;
        }

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            String field = _term.Field;
            String text = _term.Text();
            if (text.IndexOf('*') == -1)
            {
                // no wildcards, use a term query
                return new SingleTermsEnum(TermsEnum.EMPTY, _term.Bytes);
            }
            else
            {
                if (text[0] == '*' &&
                    text[text.Length - 1] == '*')
                {
                    // leading and trailing '*', remove trailing '*'
                    // and permute to make suffix query
                    string ptext = permute(
                      text.Substring(0, text.Length - 1), 0);
                    return new PrefixTermsEnum(TermsEnum.EMPTY, new Term(field, ptext).Bytes);
                }
                else if (text[text.Length - 1] == '*')
                {
                    // trailing '*', pad with '$' on front and convert
                    // to prefix query
                    string ptext = "$" + text.Substring(0, text.Length - 1);
                    return new PrefixTermsEnum(TermsEnum.EMPTY, new Term(field, ptext).Bytes);
                }
                else
                {
                    // leading/within '*', pad with '$" on end and permute
                    // to convert to prefix query
                    String ptext = permute(text + "$", text.IndexOf('*'));
                    return new PrefixTermsEnum(TermsEnum.EMPTY, new Term(field, ptext).Bytes);
                }
            }
        }

        private List<Term> _getTermsEnum()
        {
            String field = _term.Field;
            String text = _term.Text();
            if (text.IndexOf('*') == -1)
            {
                // no wildcards, use a term query
                return new List<Term> { _term };
            }
            else
            {
                var termList = new List<Term>();

                if (text[0] == '*' &&
                    text[text.Length - 1] == '*')
                {
                    // leading and trailing '*', remove trailing '*'
                    // and permute to make suffix query
                    string ptext = "$" + text.Substring(1, text.Length - 2);

                    termList.Add(new Term(field, ptext));

                    ptext = text.Substring(1, text.Length - 2)+ "$";

                    termList.Add(new Term(field, ptext));
                }
                else
                if (text[text.Length - 1] == '*')
                {
                    // trailing '*', pad with '$' on front and convert
                    // to prefix query
                    string ptext = "$" + text.Substring(0, text.Length - 1);
                    termList.Add(new Term(field, ptext));
                }
                else
                {
                    // leading/within '*', pad with '$" on end and permute
                    // to convert to prefix query
                    String ptext = permute(text + "$", text.IndexOf('*'));
                    termList.Add( new Term(field, ptext));
                }

                return termList;
            }
        }

        private string permute(string text, int starAt)
        {
            char[] tbuf = new char[text.Length];
            int tpos = 0;
            for (int i = starAt + 1; i < tbuf.Length; i++)
            {
                tbuf[tpos++] = text[i];
            }
            for (int i = 0; i < starAt; i++)
            {
                tbuf[tpos++] = text[i];
            }
            return new string(tbuf, 0, tpos);
        }
    }
}
