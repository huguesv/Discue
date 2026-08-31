// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.CueToolsDatabase.Web.Models;

using System.Collections.Immutable;

public sealed record class CTDBResponseMeta
{
    public string Source { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public string Artist { get; init; } = string.Empty;

    public string Album { get; init; } = string.Empty;

    public string Year { get; init; } = string.Empty;

    public string Genre { get; init; } = string.Empty;

    public string Extra { get; init; } = string.Empty;

    public string DiscNumber { get; init; } = string.Empty;

    public string DiscCount { get; init; } = string.Empty;

    public string DiscName { get; init; } = string.Empty;

    public string InfoUrl { get; init; } = string.Empty;

    public string Barcode { get; init; } = string.Empty;

    public ImmutableArray<CTDBResponseMetaImage> CoverArts { get; init; } = [];

    public ImmutableArray<CTDBResponseMetaTrack> Tracks { get; init; } = [];

    public ImmutableArray<CTDBResponseMetaLabel> Labels { get; init; } = [];

    public ImmutableArray<CTDBResponseMetaRelease> Releases { get; init; } = [];
}
