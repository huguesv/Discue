// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Avalonia.Services.DesignTime;

using System;
using System.Collections.Immutable;
using Woohoo.Audio.Services;

internal class NullMruService : IMruService
{
#pragma warning disable CS0067 // The event is never used
    public event EventHandler<EventArgs>? ItemsChanged;
#pragma warning restore CS0067

    public void AddItem(MruItem item)
    {
    }

    public void AddOrUpdateItem(MruItem item)
    {
    }

    public void ClearItems()
    {
    }

    public MruItem? FindItem(string itemMoniker)
    {
        return null;
    }

    public ImmutableArray<MruItem> GetItems()
    {
        return [];
    }

    public void RemoveItem(string itemMoniker)
    {
    }
}
