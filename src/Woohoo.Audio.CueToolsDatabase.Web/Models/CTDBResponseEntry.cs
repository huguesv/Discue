// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.CueToolsDatabase.Web.Models;

public sealed record class CTDBResponseEntry
{
    public long Id { get; init; }

    public string Crc32 { get; init; } = string.Empty;

    public int Confidence { get; init; }

    public int Npar { get; init; }

    public int Stride { get; init; }

    public string HasParity { get; init; } = string.Empty;

    public string Parity { get; init; } = string.Empty;

    public string Syndrome { get; init; } = string.Empty;

    public string TrackCrcs { get; init; } = string.Empty;

    public string Toc { get; init; } = string.Empty;
}
