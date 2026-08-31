// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.CueToolsDatabase.Web;

using System.Threading.Tasks;
using Woohoo.Audio.CueToolsDatabase.Web.Models;

public interface ICTDBWebClient
{
    Task<CTDBResponse?> QueryAsync(string toc, CancellationToken cancellationToken);

    Task<string?> QueryRawAsync(string toc, CancellationToken cancellationToken);
}
