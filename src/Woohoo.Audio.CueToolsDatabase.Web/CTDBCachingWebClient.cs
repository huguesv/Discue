// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.CueToolsDatabase.Web;

using System;
using System.Threading.Tasks;
using Woohoo.Audio.CueToolsDatabase.Web.Models;

public sealed class CTDBCachingWebClient : ICTDBWebClient
{
    private static readonly TimeSpan CacheExpirationAge = TimeSpan.FromDays(7);
    private readonly ICTDBWebClient innerClient;
    private readonly string cacheFolder;

    public CTDBCachingWebClient(string cacheFolder, ICTDBWebClient innerClient)
    {
        this.cacheFolder = cacheFolder;
        this.innerClient = innerClient;
    }

    public async Task<CTDBResponse?> QueryAsync(string toc, CancellationToken cancellationToken)
    {
        var cache = new CTDBResponseCache(this.cacheFolder, toc);

        if (cache.Exists &&
            cache.Age < CacheExpirationAge &&
            cache.TryRead(out var cachedResponse))
        {
            return cachedResponse;
        }

        try
        {
            var raw = await this.innerClient.QueryRawAsync(toc, cancellationToken);
            if (raw is null)
            {
                return null;
            }

            cache.WriteRaw(raw);

            var response = CTDBSerialization.Deserialize(new StringReader(raw));
            return response;
        }
        catch
        {
            // We could not get a response from the server.
            // Client/server may be offline or whatever.
            // Use the expired cache when available.
            if (cache.Exists &&
                cache.TryRead(out var expiredCachedResponse))
            {
                return expiredCachedResponse;
            }

            throw;
        }
    }

    public Task<string?> QueryRawAsync(string toc, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
