// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Internal.IO;

using System;
using System.Collections.Generic;
using System.Text;

internal interface IFile
{
    public string Extension { get; }

    public IFolder? ParentFolder { get; }
}
