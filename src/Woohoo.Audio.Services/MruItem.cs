// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Services;

using System;

public record class MruItem(string Moniker, string FullAlbumTitle, string AlbumArtUrl, DateTime LastUpdated);
