using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuceneEngine.Core
{
    public class PermutermTokenFilter : TokenFilter
    {
        private ICharTermAttribute termAttr;
        private IPositionIncrementAttribute posIncAttr;
        private AttributeSource.State current;
        private TokenStream input;
        private Stack<char[]> permuterms;
        protected internal PermutermTokenFilter(TokenStream input) : base(input)
        {
            this.termAttr = AddAttribute<ICharTermAttribute>();
            this.posIncAttr = AddAttribute<IPositionIncrementAttribute>();
            this.input = input;
            this.permuterms = new Stack<char[]>();
        }

        public sealed override bool IncrementToken()
        {
            if (permuterms.Count > 0)
            {
                char[] permuterm = permuterms.Pop();
                RestoreState(current);
                termAttr.CopyBuffer(permuterm, 0, permuterm.Length);
                posIncAttr.PositionIncrement = 0;
                return true;
            }
            if (!input.IncrementToken())
            {
                return false;
            }
            if (addPermuterms())
            {
                current = CaptureState();
            }
            return true;
        }

        private bool addPermuterms()
        {
            char[] buf = termAttr.Buffer;
            char[] obuf = new char[termAttr.Length + 1];
            for (int i = 0; i < obuf.Length - 1; i++)
            {
                obuf[i] = buf[i];
            }
            obuf[obuf.Length - 1] = '$';
            for (int i = 0; i < obuf.Length; i++)
            {
                char[] permuterm = getPermutermAt(obuf, i);
                permuterms.Push(permuterm);
            }
            return true;
        }

        private char[] getPermutermAt(char[] obuf, int pos)
        {
            char[] pbuf = new char[obuf.Length];
            int curr = pos;
            for (int i = 0; i < pbuf.Length; i++)
            {
                pbuf[i] = obuf[curr];
                curr++;
                if (curr == obuf.Length)
                {
                    curr = 0;
                }
            }
            return pbuf;
        }
    }
}
