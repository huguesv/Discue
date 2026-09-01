// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Metadata;

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Woohoo.Audio.CueToolsDatabase.Web;

public sealed class MetadataProvider : IMetadataProvider
{
    private readonly CTDBCachingWebClient databaseClient;

    public MetadataProvider(IHttpClientFactory httpClientFactory)
    {
        this.databaseClient = new CTDBCachingWebClient(Path.Combine(Path.GetTempPath(), "Woohoo.Audio", "CTDBCache"), new CTDBWebClient(httpClientFactory));
    }

    public async Task<MetadataResult?> QueryAsync(int audioTrackCount, string toc, CancellationToken cancellationToken)
    {
        try
        {
            var response = await this.databaseClient.QueryAsync(
                toc,
                cancellationToken: CancellationToken.None);

            if (response is null)
            {
                return null;
            }

            var bestMetadata = response.Metadatas.FirstOrDefault(m => m.Tracks.Length == audioTrackCount);
            if (bestMetadata is null)
            {
                return null;
            }

            var bestArtMetadata = bestMetadata;
            if (bestArtMetadata?.CoverArts.Any(a => a.Primary) != true)
            {
                // The best metadata does not have primary cover art, try to
                // find another entry with matching album that has primary cover art.
                bestArtMetadata = response.Metadatas
                    .Where(m => m.Tracks.Length == audioTrackCount)
                    .Where(m => m.Album == bestMetadata.Album)
                    .FirstOrDefault(m => m.CoverArts.Any(a => a.Primary) == true);
            }

            return new MetadataResult
            {
                Album = new AlbumMetadata
                {
                    Title = bestMetadata?.Album ?? string.Empty,
                    Artist = bestMetadata?.Artist ?? string.Empty,
                    Year = bestMetadata?.Year ?? string.Empty,
                    Images = bestArtMetadata?.CoverArts.Select(img => new ArtMetadata
                    {
                        IsPrimary = img.Primary,
                        Url = img.Uri ?? string.Empty,
                        SmallUrl = img.Uri150 ?? string.Empty,
                    }).ToImmutableArray() ?? [],
                },
                Tracks = bestMetadata?.Tracks.Select(t => new TrackMetadata
                {
                    Name = t.Name ?? string.Empty,
                    Artist = t.Artist ?? string.Empty,
                }).ToImmutableArray() ?? [],
            };
        }
        catch
        {
            return null;
        }
    }
}
