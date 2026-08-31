// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.CueToolsDatabase.Web.Models;

public sealed record class CTDBResponseMetaImage
{
    public string Uri { get; init; } = string.Empty;

    public string Uri150 { get; init; } = string.Empty;

    public int Height { get; init; }

    public int Width { get; init; }

    public bool Primary { get; init; }
}
