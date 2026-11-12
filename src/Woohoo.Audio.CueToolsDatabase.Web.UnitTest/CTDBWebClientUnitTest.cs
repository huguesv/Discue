// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.CueToolsDatabase.Web.UnitTest;

using System.Net;
using RichardSzalay.MockHttp;
using Woohoo.Audio.CueToolsDatabase.Web.UnitTest.Infrastructure;

public class CTDBWebClientUnitTest
{
    [Fact]
    public async Task QueryAsync_ShouldReturnMetadata()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        var xml = Encoding.UTF8.GetString(DataResources.CTDB_F0AFF5FD9DF98547028431DD1DFC74BE);

        mockHandler.When("http://db.cuetools.net/lookup2.php")
            .WithQueryString([
                new KeyValuePair<string, string>("version", "3"),
                new KeyValuePair<string, string>("ctdb", "1"),
                new KeyValuePair<string, string>("fuzzy", "1"),
                new KeyValuePair<string, string>("metadata", "extensive"),
                new KeyValuePair<string, string>("toc", "0:21906:39281:56437:74243:88808:100823:115032:132582:148007:168265:192424:208167:224237:241505:261364:283698:303479"),
            ])
            .Respond(HttpStatusCode.OK, "application/xml", xml);

        // Act
        var response = await new CTDBWebClient(new MockHttpClientFactory(mockHandler)).QueryAsync(
            toc: "0:21906:39281:56437:74243:88808:100823:115032:132582:148007:168265:192424:208167:224237:241505:261364:283698:303479",
            CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Entries.Should().ContainSingle();
        response.Metadatas.Should().HaveCount(3);
        response.Metadatas.Should().AllSatisfy(m => m.Tracks.Should().HaveCount(17));
    }
}
