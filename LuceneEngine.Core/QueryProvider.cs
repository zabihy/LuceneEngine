using LuceneEngine.Core.Contracts;
using LuceneEngine.Models.Attributes;
using LuceneEngine.Models.CacheModels;
using LuceneEngine.Models.Contracts;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LuceneEngine.Core
{
    public class QueryProvider<TLuceneEntity> : ILuceneQueryable<TLuceneEntity>
        where TLuceneEntity : ILuceneEntity
    {
        public QueryProvider(
            IHostingEnvironment environment,
            AnalyzerFactory analyzerFactory,
            SubfolderFactory folderFactory
            )
        {
            _boolQuery = new BooleanQuery();
            _current = _boolQuery;

            //for search purpose
            _env = environment;
            _analyzerFactory = analyzerFactory;
            _subfolder = folderFactory.GetFolderName<TLuceneEntity>();
        }
        private BooleanQuery _boolQuery { get; set; }
        public QueryProvider<TLuceneEntity> Phrase<TResult>(string value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            return _Phrase(value, selector, occur);
        }
        public QueryProvider<TLuceneEntity> Phrase<TResult>(int value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            return _Phrase(value.ToString(), selector, occur);
        }
        public QueryProvider<TLuceneEntity> Phrase<TResult>(long value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            return _Phrase(value.ToString(), selector, occur);
        }
        public QueryProvider<TLuceneEntity> Phrase<TResult>(double value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            return _Phrase(value.ToString(), selector, occur);
        }
        private QueryProvider<TLuceneEntity> _Phrase<TResult>(string value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            TrimTerm(ref value);

            if (IsNull(ref value))
            {
                return this;
            }

            var q = new PhraseQuery();

            q.Add(new Term(selector.GetName(), value));

            _current.Add(q, occur);

            return this;
        }

        public QueryProvider<TLuceneEntity> Phrase<TResult>(bool value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            if (value)
            {
                return _Phrase(1.ToString(), selector, occur);
            }
            else
            {
                return _Phrase(0.ToString(), selector, occur);
            }
        }

        public QueryProvider<TLuceneEntity> Phrase<TResult>(IEnumerable<string> value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            if (value == null || value.Count() <= 0)
            {
                return this;
            }

            this.Open(occur);

            foreach (var item in value)
            {
                this._Phrase(item, selector, Occur.SHOULD);
            }

            this.Close();

            return this;
        }

        public QueryProvider<TLuceneEntity> Phrase<TResult>(IEnumerable<int> value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            if (value == null || value.Count() <= 0)
            {
                return this;
            }

            this.Open(occur);

            foreach (var item in value.Select(id => id.ToString()))
            {
                this._Phrase(item, selector, Occur.SHOULD);
            }

            this.Close();

            return this;
        }
        public QueryProvider<TLuceneEntity> Phrase<TResult>(IEnumerable<long> value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            if (value == null || value.Count() <= 0)
            {
                return this;
            }

            this.Open(occur);

            foreach (var item in value.Select(id => id.ToString()))
            {
                this._Phrase(item, selector, Occur.SHOULD);
            }

            this.Close();

            return this;
        }
        public QueryProvider<TLuceneEntity> Phrase<TResult>(IEnumerable<double> value, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            if (value == null || value.Count() <= 0)
            {
                return this;
            }

            this.Open(occur);

            foreach (var item in value.Select(id => id.ToString()))
            {
                this._Phrase(item, selector, Occur.SHOULD);
            }

            this.Close();

            return this;
        }

        public QueryProvider<TLuceneEntity> IncludeWildCard<TResult>(string term, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            TrimTerm(ref term);

            if (IsNull(ref term))
            {
                return this;
            }

            var propertyInfo = selector.GetPropertyInfo();

            if (propertyInfo.CustomAttributes.Any(ca => ca.AttributeType == typeof(TokenizedAttribute)))
            {
                return TokenizedWildCard(term, selector, occur);
            }
            else
            {
                return NoneTokenizedWildCard(term, selector, occur);
            }
        }

        private QueryProvider<TLuceneEntity> NoneTokenizedWildCard<TResult>(string term, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            CorrectTerm(ref term);

            var query = new WildcardQuery(new Term(selector.GetName(), $"/.*{term}*./"));

            _current.Add(query, occur);

            _containsWildCard = true;

            return this;
        }

        private QueryProvider<TLuceneEntity> TokenizedWildCard<TResult>(string term, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            if (term.Contains(" "))
            {
                this.Open(occur);

                var termsList = term.Split(' ');

                termsList = RemoveEmpty(termsList);

                var wildCards = new List<PermutermWildcardQuery>();

                for (int i = 0; i < termsList.Length; i++)
                {
                    if (i == 0)
                    {
                        wildCards.Add(new PermutermWildcardQuery(new Term(selector.GetName(), $"*{termsList[i]}")));
                    }
                    else if (i == termsList.Length - 1)
                    {
                        wildCards.Add(new PermutermWildcardQuery(new Term(selector.GetName(), $"{termsList[i]}*")));
                    }
                    else
                    {
                        wildCards.Add(new PermutermWildcardQuery(new Term(selector.GetName(), $"{termsList[i]}")));
                    }
                }

                foreach (var item in wildCards)
                {
                    _current.Add(item, Occur.SHOULD);
                }

                this.Close();
            }

            else
            {
                var query = new PermutermWildcardQuery(new Term(selector.GetName(), $"*{term}*"));

                _current.Add(query, occur);
            }

            _containsWildCard = true;

            return this;
        }

        private static string[] RemoveEmpty(string[] termsList)
        {
            termsList = termsList.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
            return termsList;
        }

        public QueryProvider<TLuceneEntity> IncludePhrase<TResult>(string term, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            TrimTerm(ref term);

            if (IsNull(ref term))
            {
                return this;
            }

            var propertyInfo = selector.GetPropertyInfo();

            if (propertyInfo.CustomAttributes.Any(ca => ca.AttributeType == typeof(TokenizedAttribute)))
            {
                return TokenizedIncludePhrase(term, selector, occur);
            }
            else
            {
                return NoneTokenizedIncludePhrase(term, selector, occur);
            }
        }
        private QueryProvider<TLuceneEntity> TokenizedIncludePhrase<TResult>(string term, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            if (term.Contains(" "))
            {
                this.Open(occur);

                var termsList = term.Split(' ');

                termsList = RemoveEmpty(termsList);

                var wildCards = new List<PermutermWildcardQuery>();

                for (int i = 0; i < termsList.Length; i++)
                {
                    wildCards.Add(new PermutermWildcardQuery(new Term(selector.GetName(), $"{termsList[i]}")));
                }


                var multiPhrase = new MultiPhraseQuery();

                foreach (var item in termsList)
                {
                    multiPhrase.Add(new Term(selector.GetName(), item));
                }

                multiPhrase.Slop = 8;

                _current.Add(multiPhrase, Occur.MUST);

                this.Close();
            }

            else
            {
                var query = new PermutermWildcardQuery(new Term(selector.GetName(), $"{term}"));

                _current.Add(query, occur);
            }

            _containsWildCard = true;

            return this;
        }

        private QueryProvider<TLuceneEntity> NoneTokenizedIncludePhrase<TResult>(string term, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        {
            this.Open(occur);

            this._Phrase(term, selector, Occur.SHOULD);

            CorrectTerm(ref term);

            StringBuilder sb = new StringBuilder();
            sb.Append(term);
            sb.Append("\\ ");
            sb.Append("*");
            _current.Add(new WildcardQuery(new Term(selector.GetName(), sb.ToString())), Occur.SHOULD);

            sb.Clear();
            sb.Append("*");
            sb.Append("\\ ");
            sb.Append(term);
            sb.Append("\\ ");
            sb.Append("*");
            _current.Add(new WildcardQuery(new Term(selector.GetName(), sb.ToString())), Occur.SHOULD);

            sb.Clear();
            sb.Append("*");
            sb.Append("\\ ");
            sb.Append(term);
            _current.Add(new WildcardQuery(new Term(selector.GetName(), sb.ToString())), Occur.SHOULD);

            _containsWildCard = true;

            this.Close();

            return this;
        }

        //public QueryEngine<TLuceneEntity> RawPrefix<TResult>(string term, Expression<Func<TLuceneEntity, TResult>> selector, Occur occur)
        //{
        //    ChekNull(term);
        //    term = CorrectTerm(term);

        //    var query = new PermutermWildcardQuery(new Term(selector.GetName(), $"{term}*"));

        //    _current.Add(query, occur);

        //    _containsWildCard = true;

        //    return this;
        //}

        private static void CorrectTerm(ref string term)
        {
            var termList = term.Split(' ');

            termList = RemoveEmpty(termList);

            if (termList.Length > 1)
            {
                term = string.Empty;

                StringBuilder sb = new StringBuilder();

                foreach (var item in termList)
                {
                    sb.Append(item);
                    sb.Append("\\ ");
                }
                sb.Remove(sb.Length - 2, 2);

                term = sb.ToString();
            }
        }

        public QueryProvider<TLuceneEntity> LongRangeQuery<TResult>(Occur occur, Expression<Func<TLuceneEntity, TResult>> selector, long min, long max)
        {
            _current.Add(new BooleanClause(NumericRangeQuery.NewInt64Range(selector.GetName(), min, max, true, true), occur));

            return this;
        }

        public QueryProvider<TLuceneEntity> IntRangeQuery<TResult>(Occur occur, Expression<Func<TLuceneEntity, TResult>> selector, int min, int max)
        {
            _current.Add(NumericRangeQuery.NewInt32Range(selector.GetName(), min, max, true, true), occur);

            return this;
        }

        public QueryProvider<TLuceneEntity> DoubleRangeQuery<TResult>(Occur occur, Expression<Func<TLuceneEntity, TResult>> selector, double min, double max)
        {
            _current.Add(new BooleanClause(NumericRangeQuery.NewDoubleRange(selector.GetName(), min, max, true, true), occur));

            return this;
        }

        BooleanQuery _current = null;
        private readonly IHostingEnvironment _env;
        private readonly AnalyzerFactory _analyzerFactory;
        private readonly string _subfolder;
        BooleanQuery _prev = null;

        private bool _containsWildCard;

        public QueryProvider<TLuceneEntity> Open(Occur occur)
        {
            _prev = _current;

            var subQuery = new BooleanQuery();

            _current.Add(subQuery, occur);

            _current = subQuery;

            return this;
        }

        public QueryProvider<TLuceneEntity> Close()
        {
            _current = _prev;

            return this;
        }

        Sort _sort;
        public QueryProvider<TLuceneEntity> OrderBy<TResult>(Expression<Func<TLuceneEntity, TResult>> selector)
        {
            return _OrderBy(selector.GetName(), false);
        }

        public QueryProvider<TLuceneEntity> OrderByDesc<TResult>(Expression<Func<TLuceneEntity, TResult>> selector)
        {
            return _OrderBy(selector.GetName(), true);
        }

        public QueryProvider<TLuceneEntity> OrderByRelevance()
        {
            _sort = new Sort(SortField.FIELD_SCORE);

            return this;
        }

        public QueryProvider<TLuceneEntity> OrderBy(string fieldName)
        {
            return _OrderBy(fieldName, false);
        }

        public QueryProvider<TLuceneEntity> OrderByDesc(string fieldName)
        {
            return _OrderBy(fieldName, true);
        }

        private QueryProvider<TLuceneEntity> _OrderBy(string fieldName, bool reverse)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return this;
            }

            _sort = new Sort(new SortField(fieldName.ToLower(), SortFieldType.STRING, reverse));

            return this;
        }

        private void TrimTerm(ref string searchTerm)
        {
            if (IsNull(ref searchTerm))
            {
                return;
            }

            searchTerm = searchTerm.Trim();

            if (searchTerm.Any(c1 => specialChars.Any(c2 => c1 == c2)))
            {
                searchTerm = searchTerm.Trim(specialChars);

                StringBuilder sb = new StringBuilder();

                foreach (var ch in searchTerm)
                {
                    if (!specialChars.Any(sch => ch == sch))
                    {
                        sb.Append(ch);
                    }
                    else
                    {
                        sb.Append(' ');
                    }
                }

                searchTerm = sb.ToString();
            }
        }
        private bool IsNull(ref string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            return false;
        }

        public QueryProvider<TLuceneEntity> Skip(int skip)
        {
            this._skip = skip;
            return this;
        }

        public QueryProvider<TLuceneEntity> Take(int take)
        {
            this._pageSize = take;
            return this;
        }

        public int GetSkip()
        {
            return _skip;
        }

        public int GetPageSize()
        {
            return _pageSize;
        }

        public BooleanQuery GetBooleanQuery()
        {
            return _boolQuery;
        }

        public Sort GetSort()
        {
            return _sort;
        }

        public bool GetContainsWildCard()
        {
            return _containsWildCard;
        }

        public ISearchProvider<TLuceneEntity> Next()
        {
            return new SearchProvider<TLuceneEntity>(_env, _analyzerFactory, _subfolder, this);
        }

        protected int _skip;
        protected int _pageSize;

        private static char[] specialChars = { '+', '-', '&', '|', '!', '(', ')', '{', '}', '[', ']', '^', '"', '~', '*', '?', ':', '\\', '/', '.', ',', '>', '<', '؟', '،', '،', '؛', ':', ';', '\'', '{', '}', '~', '$', '_', '=' };
    }

}
