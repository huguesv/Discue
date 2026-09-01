// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.CueToolsDatabase.Web.Models;

using System.Collections.Immutable;

public sealed record class CTDBResponse
{
    public string Status { get; init; } = string.Empty;

    public string UpdateUrl { get; init; } = string.Empty;

    public string UpdateMsg { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public int Npar { get; init; }

    public ImmutableArray<CTDBResponseEntry> Entries { get; init; } = [];

    public ImmutableArray<CTDBResponseMeta> Metadatas { get; init; } = [];
}
