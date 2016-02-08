﻿using System;
using Lending.Cqrs.Query;
using Lending.Domain.AddBookToLibrary;
using Lending.Domain.Model;
using Lending.Domain.RemoveBookFromLibrary;
using Lending.ReadModels.Relational.BookAdded;
using Lending.ReadModels.Relational.SearchForBook;
using NUnit.Framework;
using static Tests.DefaultTestData;

namespace Tests.Commands
{
    /// <summary>
    /// https://github.com/joshilewis/lending/issues/9
    /// As a User I want to Add Books to my Library so that my Books can be searched by Linked Libraries
    /// </summary>
    public class AddBookToLibraryTests : FixtureWithEventStoreAndNHibernate
    {

        /// <summary>
        /// GIVEN Library1 is Open
        /// WHEN Library1 Adds Book1
        /// THEN HTTP201 is returned 
        /// AND Book1 appears in Library1's Books
        /// </summary>
        [Test]
        public void AddingNewBookToLibraryShouldSucceed()
        {
            GivenCommand(OpenLibrary1).IsPOSTedTo("/libraries");
            WhenCommand(AddBook1ToLibrary).IsPOSTedTo($"/libraries/{Library1Id}/books/add");
            Then(Http201Created);
            AndEventsSavedForAggregate<Library>(Library1Id, Library1Opened, Book1AddedToUser1Library);
            AndGETTo<BookSearchResult[]>($"/libraries/{Library1Id}/books/").Returns(new[]
            {
                new BookSearchResult(OpenLibrary1.AggregateId, OpenLibrary1.Name, AddBook1ToLibrary.Title,
                    AddBook1ToLibrary.Author, AddBook1ToLibrary.Isbn),
            });
        }

        /// <summary>
        /// GIVEN Library1 is Open AND Book1 is Added to Library1
        /// WHEN Library1 Adds Book1
        /// THEN HTTP400 is returned because Book1 is already in Library1
        /// AND Book1 appears only once in Library1's Books
        /// </summary>
        [Test]
        public void AddingDuplicateBookToLibraryShouldFail()
        {
            GivenCommand(OpenLibrary1).IsPOSTedTo("/libraries");
            GivenCommand(AddBook1ToLibrary).IsPOSTedTo($"/libraries/{Library1Id}/books/add");
            WhenCommand(AddBook1ToLibrary).IsPOSTedTo($"/libraries/{Library1Id}/books/add");
            Then(Http400Because(Library.BookAlreadyInLibrary));
            AndEventsSavedForAggregate<Library>(Library1Id, Library1Opened, Book1AddedToUser1Library);
            AndGETTo<BookSearchResult[]>($"/libraries/{Library1Id}/books/").Returns(new[]
            {
                new BookSearchResult(OpenLibrary1.AggregateId, OpenLibrary1.Name, AddBook1ToLibrary.Title,
                    AddBook1ToLibrary.Author, AddBook1ToLibrary.Isbn),
            });
        }

        /// <summary>
        /// GIVEN Library1 is Open and Library1 Adds and Removes Book1 
        /// WHEN Library Adds Book1
        /// THEN HTTP201 is returned
        /// AND Book1 appears only once in Library1's Books
        /// </summary>
        [Test]
        public void AddingPreviouslyRemovedBookToLibraryShouldSucceed()
        {
            GivenCommand(OpenLibrary1).IsPOSTedTo("/libraries");
            GivenCommand(AddBook1ToLibrary).IsPOSTedTo($"/libraries/{Library1Id}/books/add");
            GivenCommand(User1RemovesBookFromLibrary).IsPOSTedTo($"/libraries/{Library1Id}/books/remove");
            WhenCommand(AddBook1ToLibrary).IsPOSTedTo($"/libraries/{Library1Id}/books/add");
            Then(Http201Created);
            AndEventsSavedForAggregate<Library>(Library1Id, Library1Opened, Book1AddedToUser1Library, Book1RemovedFromLibrary, Book1AddedToUser1Library);
            AndGETTo<BookSearchResult[]>($"/libraries/{Library1Id}/books/").Returns(new[]
            {
                new BookSearchResult(OpenLibrary1.AggregateId, OpenLibrary1.Name, AddBook1ToLibrary.Title,
                    AddBook1ToLibrary.Author, AddBook1ToLibrary.Isbn),
            });
        }

        [Test]
        public void UnauthorizedAddBookAddBookShouldFail()
        {
            GivenCommand(OpenLibrary1).IsPOSTedTo("/libraries");
            WhenCommand(UnauthorizedAddBookToLibrary).IsPOSTedTo($"/libraries/{Library1Id}/books/add");
            Then(Http403BecauseUnauthorized(UnauthorizedAddBookToLibrary.UserId, Library1Id, typeof (Library)));
            AndEventsSavedForAggregate<Library>(Library1Id, Library1Opened);
            AndGETTo<BookSearchResult[]>($"/libraries/{Library1Id}/books/").Returns(new BookSearchResult[] {});
        }

    }
}
