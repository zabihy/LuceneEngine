using LuceneEngine.Models.CacheModels;
using LuceneEngine.Models.ReportModels.GridModels;
using Lucene.Net.Documents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LuceneEngine.Core.Deserializers
{
    public class DeserializeAuthor : BaseDeserialize<AuthorCacheEntity>
    {
        public DeserializeAuthor(IEnumerable<Document> documents) : base(documents)
        {
            StringBuilder sb = new StringBuilder();

            var dict = new Dictionary<string, string>();

            foreach (var item in documents)
            {
                AuthorCacheEntity entity = new AuthorCacheEntity();

                sb.Clear();

                dict.Clear();

                foreach (var field in item.Fields)
                {

                    dict.Add(field.Name, field.GetStringValue());
                }

                var des = JsonConvert.DeserializeObject<AuthorCacheEntity>(JsonConvert.SerializeObject(dict));

                entity.AuthorId = des.AuthorId;
                entity.Name = des.Name;
                entity.IsDeleted = des.IsDeleted;
                entity.SourceId = des.SourceId;
                entity.University = des.University;

                this.Add(entity);
            }
        }
    }
    public class DeserializeArticleFull : BaseDeserialize<ArticleCacheEntity>
    {
        public DeserializeArticleFull(IEnumerable<Document> documents) : base(documents)
        {
            StringBuilder sb = new StringBuilder();

            var dict = new Dictionary<string, string>();

            foreach (var item in documents)
            {
                ArticleCacheEntity entity = new ArticleCacheEntity();

                sb.Clear();

                dict.Clear();

                string allAuthorIds = nameof(entity.AllAuthorIds).ToLower();
                string authorId = nameof(entity.AuthorId).ToLower();
                string authorName = nameof(entity.AuthorName).ToLower();
                string authors = nameof(entity.Authors).ToLower();
                string keyword = nameof(entity.Keyword).ToLower();

                foreach (var field in item.Fields)
                {
                    if (field.Name == allAuthorIds)
                    {
                        entity.AddToAllAuthorIds(ExtractLong(field));
                    }
                    else
                    {
                        if (field.Name == authorId)
                        {
                            entity.AddToAuthorId(ExtractLong(field));
                        }
                        else
                        {
                            if (field.Name == authorName)
                            {
                                entity.AddToAuthorName(field.GetStringValue());
                            }
                            else
                            {
                                if (field.Name == authors)
                                {
                                    var keyValue = field.GetStringValue().Split(':');

                                    long key = long.Parse(keyValue.FirstOrDefault());

                                    string value = keyValue.LastOrDefault();

                                    entity.AddToAuthors(key, value);
                                }
                                else
                                {
                                    if (field.Name == keyword)
                                    {
                                        entity.AddToKeyword(field.GetStringValue());
                                    }
                                    else if (!field.Name.ToLower().Contains("object"))
                                    {
                                        dict.Add(field.Name, field.GetStringValue());
                                    }
                                }
                            }
                        }
                    }
                }

                var des = JsonConvert.DeserializeObject<ArticleFlattenCacheEntity>(JsonConvert.SerializeObject(dict));

                entity.ArticleId = des.ArticleId;
                entity.ArticleTypeId = des.ArticleTypeId;
                entity.ArticleTypeName = des.ArticleTypeName;
                entity.ClassId = des.ClassId;
                entity.ClassName = des.ClassName;
                entity.ClassNameFa = des.ClassNameFa;
                entity.CrawlHistoryId = des.CrawlHistoryId;
                entity.DateCreated = DateTime.Now;
                entity.DateUpdated = DateTime.Now;
                entity.GrandParentSourceId = des.GrandParentSourceId;
                entity.GrandParentSourceName = des.GrandParentSourceName;
                entity.IsArticle = des.IsArticle;
                entity.IsDeleted = des.IsDeleted;
                entity.MainArticleTypeId = des.MainArticleTypeId;
                entity.ParentSourceId = des.ParentSourceId;
                entity.ParentSourceName = des.ParentSourceName;
                entity.PublicationDateText = des.PublicationDateText;
                entity.PublicationYear = des.PublicationYear;
                entity.SourceId = des.SourceId;
                entity.SourceName = des.SourceName;
                entity.Title = des.Title;
                entity.University = des.University;
                entity.UniversityId = des.UniversityId;
                entity.Abstract = des.Abstract;
                entity.Keywords = des.Keywords;
                entity.MagazineId = des.MagazineId;
                entity.MagazineName = des.MagazineName;
                entity.AdvisorId = des.AdvisorId;
                entity.AdvisorName = des.AdvisorName;
                entity.SupervisorId = des.SupervisorId;
                entity.SuperVisorName = des.SuperVisorName;

                this.Add(entity);
            }
        }
    }

    public class DeserializeArticleMinimal : BaseDeserialize<ArticleGridModel>
    {
        public DeserializeArticleMinimal(IEnumerable<Document> documents) : base(documents)
        {
            StringBuilder sb = new StringBuilder();

            var dict = new Dictionary<string, string>();

            foreach (var item in documents)
            {
                ArticleGridModel entity = new ArticleGridModel();

                sb.Clear();

                dict.Clear();

                string articleId = nameof(entity.ArticleId).ToLower();
                string articleTypeId = nameof(entity.ArticleTypeId).ToLower();
                string className = nameof(entity.ClassName).ToLower();
                string dateCreated = nameof(entity.DateCreated).ToLower();
                string dateUpdated = nameof(entity.DateUpdated).ToLower();
                string magazineId = nameof(entity.MagazineId).ToLower();
                string magazineName = nameof(entity.MagazineName).ToLower();
                string publicationYear = nameof(entity.PublicationYear).ToLower();
                string title = nameof(entity.Title).ToLower();




                foreach (var field in item.Fields)
                {
                    if (field.Name == articleId)
                    {
                        entity.ArticleId = ExtractLong(field);
                    }
                    else
                    {
                        if (field.Name == articleTypeId)
                        {
                            entity.ArticleTypeId = field.GetStringValue();
                        }
                        else
                        {
                            if (field.Name == className)
                            {
                                entity.ClassName = field.GetStringValue();
                            }
                            else if (field.Name == dateCreated)
                            {
                                entity.DateCreated = field.GetStringValue();
                            }
                            else if (field.Name == dateUpdated)
                            {
                                entity.DateUpdated = field.GetStringValue();
                            }
                            else if (field.Name == magazineId)
                            {
                                entity.MagazineId = ExtractLong(field);
                            }
                            else if (field.Name == magazineName)
                            {
                                entity.MagazineName = field.GetStringValue();
                            }
                            else if (field.Name == publicationYear)
                            {
                                entity.PublicationYear = ExtractInt(field);
                            }
                            else if (field.Name == title)
                            {
                                entity.Title = field.GetStringValue();
                            }
                        }
                    }
                }

                this.Add(entity);
            }
        }
    }

    public class DeserializeThesisMinimal : BaseDeserialize<ThesisGridModel>
    {
        public DeserializeThesisMinimal(IEnumerable<Document> documents) : base(documents)
        {
            StringBuilder sb = new StringBuilder();

            var dict = new Dictionary<string, string>();

            foreach (var item in documents)
            {
                ThesisGridModel entity = new ThesisGridModel();

                sb.Clear();

                dict.Clear();

                string articleId = nameof(entity.ArticleId).ToLower();
                string className = nameof(entity.ClassName).ToLower();
                string publicationYear = nameof(entity.PublicationYear).ToLower();
                string title = nameof(entity.Title).ToLower();
                string university = nameof(entity.University).ToLower();
                string universityId = nameof(entity.UniversityId).ToLower();

                foreach (var field in item.Fields)
                {
                    if (field.Name == articleId)
                    {
                        entity.ArticleId = ExtractLong(field);
                    }
                    else
                    {
                        if (field.Name == articleId)
                        {
                            entity.ArticleId = ExtractLong(field);
                        }
                        else
                        {
                            if (field.Name == className)
                            {
                                entity.ClassName = field.GetStringValue();
                            }
                            else if (field.Name == university)
                            {
                                entity.University = field.GetStringValue();
                            }
                            else if (field.Name == universityId)
                            {
                                entity.UniversityId = ExtractLong(field);
                            }
                            else if (field.Name == publicationYear)
                            {
                                entity.PublicationYear = ExtractInt(field);
                            }
                            else if (field.Name == title)
                            {
                                entity.Title = field.GetStringValue();
                            }
                        }
                    }
                }

                this.Add(entity);
            }
        }
    }

}
