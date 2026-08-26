// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Storage;

using System;

public sealed record XPlatStorageItemProperties
{
    public DateTimeOffset? DateCreated { get; init; }

    public DateTimeOffset? DateModified { get; init; }

    public ulong? Size { get; init; }
}
