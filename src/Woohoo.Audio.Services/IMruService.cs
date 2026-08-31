// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Services;

using System.Collections.Immutable;

public interface IMruService
{
    event EventHandler<EventArgs> ItemsChanged;

    void AddItem(MruItem item);

    void ClearItems();

    ImmutableArray<MruItem> GetItems();

    void RemoveItem(string itemMoniker);

    void AddOrUpdateItem(MruItem item);

    MruItem? FindItem(string itemMoniker);
}
