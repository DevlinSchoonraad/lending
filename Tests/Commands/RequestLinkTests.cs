﻿using System;
using System.Net;
using Lending.Cqrs.Exceptions;
using Lending.Cqrs.Query;
using Lending.Domain.Model;
using Lending.Domain.RequestLink;
using Lending.ReadModels.Relational.LinkAccepted;
using Lending.ReadModels.Relational.LinkRequested;
using NUnit.Framework;
using static Tests.DefaultTestData;

namespace Tests.Commands
{
    /// <summary>
    /// https://github.com/joshilewis/lending/issues/6
    /// </summary>
    [TestFixture]
    public class RequestLinkTests : FixtureWithEventStoreAndNHibernate
    {
        /// <summary>
        /// GIVEN Library1 is Open AND Library2 is Open AND they are not Linked AND there is an existing Link Request from Library1 to Library2
        /// WHEN Library1 requests to Link to Library2
        /// THEN HTTP400 is returned because a Link Request between Library1 and Library2 already exists
        /// AND the Link Request Sent to Library2 appears in Library1's Sent Link Requests
        /// AND the Link Request Received from Library1 appears in Library2's Received Link Requests
        /// AND nothing appears in Library1's Links
        /// AND nothing appears in Library2's Links
        /// </summary>
        [Test]
        public void RequestLinkFromLibraryWithPendingRequestShouldFail()
        {
            GivenCommand(OpenLibrary1).IsPOSTedTo("/libraries");
            GivenCommand(OpenLibrary2).IsPOSTedTo("/libraries");
            GivenCommand(Library1RequestsLinkToLibrary2).IsPOSTedTo($"/libraries/{Library1Id}/links/request");
            WhenCommand(Library1Requests2NdLinkToLibrary2).IsPOSTedTo($"/libraries/{Library1Id}/links/request");
            Then(Http400Because(Library.LinkAlreadyRequested));
            AndEventsSavedForAggregate<Library>(Library1Id, Library1Opened, LinkRequestedFrom1To2);
            AndEventsSavedForAggregate<Library>(Library2Id, Library2Opened, LinkRequestFrom1To2Received);
            AndGETTo<RequestedLink[]>($"/libraries/{Library1Id}/links/sent").As(Library1Id).Returns(new[]
            {
                new RequestedLink(Guid.Empty, OpenedLibrary1, OpenedLibrary2), 
            });
            AndGETTo<RequestedLink[]>($"/libraries/{Library1Id}/links/received").As(Library2Id).Returns(new[]
            {
                new RequestedLink(Guid.Empty, OpenedLibrary1, OpenedLibrary2),
            });
            AndGETTo<LibraryLink[]>($"/libraries/{Library1Id}/links/").As(Library1Id).Returns(new LibraryLink [] {});
            AndGETTo<LibraryLink[]>($"/libraries/{Library1Id}/links/").As(Library2Id).Returns(new LibraryLink[] {});
        }

        /// <summary>
        /// GIVEN Library1 is Open AND Library2 is Open AND they are not Linked AND there are no Link Requests between them
        /// WHEN Library1 Requests a Link to Library2
        /// THEN HTTP200 is returned
        /// AND the Link Request Sent to Library2 appears in Library1's Sent Link Requests
        /// AND the Link Request Received from Library1 appears in Library2's Received Link Requests
        /// AND nothing appears in Library1's Links
        /// AND nothing appears in Library2's Links
        /// </summary>
        [Test]
        public void RequestLinkForUnLinkedLibrarysShouldSucceed()
        {
            GivenCommand(OpenLibrary1).IsPOSTedTo("/libraries");
            GivenCommand(OpenLibrary2).IsPOSTedTo("/libraries");
            WhenCommand(Library1RequestsLinkToLibrary2).IsPOSTedTo($"/libraries/{Library1Id}/links/request");
            Then(Http200Ok);
            AndEventsSavedForAggregate<Library>(Library1Id, Library1Opened, LinkRequestedFrom1To2);
            AndEventsSavedForAggregate<Library>(Library2Id, Library2Opened, LinkRequestFrom1To2Received);
            AndGETTo<RequestedLink[]>($"/libraries/{Library1Id}/links/sent").As(Library1Id).Returns(new[]
            {
                new RequestedLink(Guid.Empty, OpenedLibrary1, OpenedLibrary2),
            });
            AndGETTo<RequestedLink[]>($"/libraries/{Library1Id}/links/received").As(Library2Id).Returns(new[]
            {
                new RequestedLink(Guid.Empty, OpenedLibrary1, OpenedLibrary2),
            });
            AndGETTo<LibraryLink[]>($"/libraries/{Library1Id}/links/").As(Library1Id).Returns(new LibraryLink[] { });
            AndGETTo<LibraryLink[]>($"/libraries/{Library1Id}/links/").As(Library2Id).Returns(new LibraryLink[] { });
        }

        /// <summary>
        /// GIVEN Library1 is Open AND Library2 is not Open
        /// WHEN Library1 Requests to Link to Library2
        /// THEN HTTP404 is returned because Library2 does not exist
        /// AND nothing appears in Library1's Sent Link Requests
        /// AND nothing appears in Library1's Links
        /// </summary>
        [Test]
        public void RequestLinkToNonExistentLibraryShouldFail()
        {
            GivenCommand(OpenLibrary1).IsPOSTedTo($"/libraries");
            WhenCommand(Library1RequestsLinkToLibrary2).IsPOSTedTo($"/libraries/{Library1Id}/links/request");
            Then(Http404BecauseTargetLibraryDoesNotExist);
            AndEventsSavedForAggregate<Library>(Library1Id, Library1Opened);
            AndGETTo<RequestedLink[]>($"/libraries/{Library1Id}/links/sent").As(Library1Id).Returns(new RequestedLink[] {});
            AndGETTo<LibraryLink[]>($"/libraries/{Library1Id}/links/").As(Library1Id).Returns(new LibraryLink[] {});
        }

        /// <summary>
        /// GIVEN Library1 is Open AND Library2 is Open AND they are not Linked AND there is an existing Link Request from Library2 to Library1
        /// WHEN Library1 Requests to Link to Library2
        /// THEN HTTP400 is returned because a Link Request was Sent from Library2 to Library1
        /// AND the Link Request Received from Library2 appears in Library1's Received Link Requests
        /// AND the Link Request Sent to Library1 appears in Library2's Sent Link Requests
        /// AND nothing appears in Library1's Links
        /// AND nothing appears in Library2's Links
        /// </summary>
        [Test]
        public void RequestLinkToLibraryWithPendingRequestShouldFail()
        {
            GivenCommand(OpenLibrary1).IsPOSTedTo($"/libraries");
            GivenCommand(OpenLibrary2).IsPOSTedTo($"/libraries");
            GivenCommand(Library2RequestsLinkToLibrary1).IsPOSTedTo($"/libraries/{Library2Id}/links/request");
            WhenCommand(Library1RequestsLinkToLibrary2).IsPOSTedTo($"/libraries/{Library1Id}/links/request");
            Then(Http400Because(Library.ReverseLinkAlreadyRequested));
            AndEventsSavedForAggregate<Library>(Library1Id, Library1Opened, LinkRequestFrom2To1Received);
            AndEventsSavedForAggregate<Library>(Library2Id, Library2Opened, LinkRequestedFrom2To1);
            AndGETTo<RequestedLink[]>($"/libraries/{Library1Id}/links/received").As(Library1Id).Returns(new[]
            {
                new RequestedLink(Guid.Empty, OpenedLibrary2, OpenedLibrary1),
            });
            AndGETTo<RequestedLink[]>($"/libraries/{Library1Id}/links/sent").As(Library2Id).Returns(new[]
            {
                new RequestedLink(Guid.Empty, OpenedLibrary2, OpenedLibrary1),
            });
            AndGETTo<LibraryLink[]>($"/libraries/{Library1Id}/links/").As(Library1Id).Returns(new LibraryLink[] {});
            AndGETTo<LibraryLink[]>($"/libraries/{Library1Id}/links/").As(Library2Id).Returns(new LibraryLink[] {});
        }

        /// <summary>
        /// GIVEN Library1 is Open AND Library2 is Open AND they are already Linked
        /// WHEN Library1 Requests to Link to Library2
        /// THEN HTTP400 because Library1 is already Linked to Library2
        /// AND nothing appears in Library1's Sent Link Requests
        /// AND nothing appears in Library2's Received Link Requests
        /// AND Library2 appears in Library1's Links
        /// AND Library1 appears in Library2's Links
        /// </summary>
        [Test]
        public void RequestLinkToLinkedLibrariesShouldFail()
        {
            GivenCommand(OpenLibrary1).IsPOSTedTo($"/libraries");
            GivenCommand(OpenLibrary2).IsPOSTedTo($"/libraries");
            GivenCommand(Library1RequestsLinkToLibrary2).IsPOSTedTo($"/libraries/{Library1Id}/links/request");
            GivenCommand(Library2AcceptsLinkFromLibrary1).IsPOSTedTo($"/libraries/{Library2Id}/links/accept");
            WhenCommand(Library1Requests2NdLinkToLibrary2).IsPOSTedTo($"/libraries/{Library1Id}/links/request");
            Then(Http400Because(Library.LibrariesAlreadyLinked));
            AndEventsSavedForAggregate<Library>(Library1Id, Library1Opened, LinkRequestedFrom1To2, LinkCompleted);
            AndEventsSavedForAggregate<Library>(Library2Id, Library2Opened, LinkRequestFrom1To2Received, LinkAccepted);
            AndGETTo<RequestedLink[]>($"/libraries/{Library1Id}/links/sent").As(Library1Id).Returns(new RequestedLink[]{});
            AndGETTo<RequestedLink[]>($"/libraries/{Library1Id}/links/received").As(Library2Id).Returns(new RequestedLink[]{});
            AndGETTo<LibraryLink[]>($"/libraries/{Library1Id}/links/").As(Library1Id)
                .Returns(new []
                {
                    new LibraryLink(Guid.Empty, OpenedLibrary1, OpenedLibrary2), 
                });
            AndGETTo<LibraryLink[]>($"/libraries/{Library1Id}/links/").As(Library2Id)
                .Returns(new []
                {
                    new LibraryLink(Guid.Empty, OpenedLibrary1, OpenedLibrary2),
                });

        }

        /// <summary>
        /// GIVEN Library1 is Open
        /// WHEN Library1 Requests a Link to Library1
        /// THEN HTTP400 because Library1 can't Link to itself
        /// AND nothing appears in Library1's Sent Link Requests
        /// AND nothing appears in Library1s Links
        /// </summary>
        [Test]
        public void RequestLinkToSelfShouldFail()
        {
            GivenCommand(OpenLibrary1).IsPOSTedTo($"/libraries");
            WhenCommand(Library1RequestsLinkToSelf).IsPOSTedTo($"/libraries/{Library1Id}/links/request");
            Then(Http400Because(RequestLinkHandler.CantConnectToSelf));
            AndEventsSavedForAggregate<Library>(Library1Id, Library1Opened);
            AndGETTo<RequestedLink[]>($"/libraries/{Library1Id}/links/sent").As(Library1Id).Returns(new RequestedLink[] { });
            AndGETTo<LibraryLink[]>($"/libraries/{Library1Id}/links/").As(Library1Id).Returns(new LibraryLink[]{ });
        }

        [Test]
        public void UnauthorizedRequestLinkShouldFail()
        {
            GivenCommand(OpenLibrary1).IsPOSTedTo($"/libraries");
            WhenCommand(UnauthorizedRequestLink).IsPOSTedTo($"/libraries/{Library1Id}/links/request");
            Then(Http403BecauseUnauthorized(UnauthorizedRequestLink.UserId, Library1Id, typeof (Library)));
            AndEventsSavedForAggregate<Library>(Library1Id, Library1Opened);
            AndGETTo<RequestedLink[]>($"/libraries/{Library1Id}/links/sent").As(Library1Id).Returns(new RequestedLink[] { });
            AndGETTo<LibraryLink[]>($"/libraries/{Library1Id}/links/").As(Library1Id).Returns(new LibraryLink[] { });
        }
    }

}
