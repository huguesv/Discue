// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.LrcDatabase.Web.UnitTest;

using System;
using System.Net;
using RichardSzalay.MockHttp;
using Woohoo.Audio.LrcDatabase.Web.UnitTest.Infrastructure;

public partial class LrcLibWebClientUnitTest
{
    [Fact]
    public async Task QueryAsync_ShouldReturnLyrics()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        var json = """
            {
              "id": 3396226,
              "name":"Live",
              "trackName": "Live",
              "artistName": "Borislav",
              "albumName": "Baldur's Gate 3 (Original Game Soundtrack)",
              "duration": 233,
              "instrumental": false,
              "plainLyrics": "I feel your breath upon my neck\n...The clock won't stop and this is what we get\n",
              "syncedLyrics": "[00:17.12] I feel your breath upon my neck\n...[03:20.31] The clock won't stop and this is what we get\n[03:25.72] "
            }
            """;
        mockHandler.When("https://lrclib.net/api/get")
            .WithQueryString([
                new KeyValuePair<string, string>("artist_name", "Borislav"),
                new KeyValuePair<string, string>("track_name", "Live"),
                new KeyValuePair<string, string>("album_name", "Baldur's Gate 3 (Original Game Soundtrack)"),
                new KeyValuePair<string, string>("duration", "233"),
            ])
            .Respond(HttpStatusCode.OK, "application/json", json);

        // Act
        var response = await new LrcLibWebClient(new MockHttpClientFactory(mockHandler)).QueryAsync(
            albumTitle: "Baldur's Gate 3 (Original Game Soundtrack)",
            artistName: "Borislav",
            trackTitle: "Live",
            duration: TimeSpan.FromSeconds(233),
            allowExternalSources: true,
            CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Name.Should().Be("Live");
        response.ArtistName.Should().Be("Borislav");
        response.AlbumName.Should().Be("Baldur's Gate 3 (Original Game Soundtrack)");
        response.TrackName.Should().Be("Live");
        response.Duration.Should().BeApproximately(233.0, 0.1);
        response.Instrumental.Should().BeFalse();
        response.PlainLyrics.Should().Be("I feel your breath upon my neck\n...The clock won't stop and this is what we get\n");
        response.SyncedLyrics.Should().Be("[00:17.12] I feel your breath upon my neck\n...[03:20.31] The clock won't stop and this is what we get\n[03:25.72] ");
    }
}
