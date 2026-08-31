// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Playback;

public sealed record class AudioPlayerDisc
{
    public required Guid Id { get; init; }

    public required string CTDBToc { get; init; }

    public string Name { get; init; } = string.Empty;
}
