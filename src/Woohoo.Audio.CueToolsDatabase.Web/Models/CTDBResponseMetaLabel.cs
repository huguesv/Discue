// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.CueToolsDatabase.Web.Models;

public sealed record class CTDBResponseMetaLabel
{
    public string Name { get; init; } = string.Empty;

    public string CatNo { get; init; } = string.Empty;
}
