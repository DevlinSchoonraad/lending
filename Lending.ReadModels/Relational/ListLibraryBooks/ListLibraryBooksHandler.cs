﻿using System;
using System.Linq;
using Joshilewis.Cqrs.Query;
using Lending.ReadModels.Relational.BookAdded;
using Lending.ReadModels.Relational.SearchForBook;
using NHibernate;

namespace Lending.ReadModels.Relational.ListLibraryBooks
{
    public class ListLibraryBooksHandler : NHibernateQueryHandler<ListLibraryBooks, BookSearchResult[]>, IAuthenticatedQueryHandler<ListLibraryBooks, BookSearchResult[]>
    {
        public ListLibraryBooksHandler(Func<ISession> sessionFunc) : base(sessionFunc)
        {
        }

        public override BookSearchResult[] Handle(ListLibraryBooks query)
        {
            LibraryBook[] libraryBooks = Session.QueryOver<LibraryBook>()
                .JoinQueryOver(x => x.Library)
                .Where(x => x.AdministratorId == query.UserId)
                .List()
                .ToArray();

            return libraryBooks
                .Select(x => new BookSearchResult(x.Library.Id, x.LibraryName, x.Title, x.Author, x.Isbn))
                .ToArray();
        }
    }
}
