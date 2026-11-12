// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Internal.IO;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;

/// <summary>
/// Stream that wraps the DeflateStream returned by ZipArchiveEntry to add seek functionality.
/// </summary>
internal class ZipEntrySeekableStream : Stream
{
    // https://github.com/dotnet/runtime/blob/main/src/libraries/System.IO.Compression/src/System/IO/Compression/DeflateZLib/DeflateStream.cs
    private readonly ZipArchiveEntry entry;
    private Stream? stream;
    private long position;

    public ZipEntrySeekableStream(ZipArchiveEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        this.entry = entry;
        this.stream = entry.Open();
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => this.entry.Length;

    public override long Position
    {
        get
        {
            return this.position;
        }

        set
        {
            if (this.position != value)
            {
                this.position = this.Seek(value, SeekOrigin.Begin);
            }
        }
    }

    public override void Close()
    {
        this.stream?.Close();
        this.stream = null;
    }

    public override void Flush()
    {
        this.EnsureNotDisposed();
        this.stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        this.EnsureNotDisposed();

        var bytesRead = this.stream.Read(buffer, offset, count);
        this.position += bytesRead;
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        this.EnsureNotDisposed();

        long newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => this.position + offset,
            SeekOrigin.End => this.Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };

        if (newPosition > this.position)
        {
            Advance(this.stream, newPosition - this.position);
            this.position = newPosition;
        }
        else if (newPosition < this.position)
        {
            // We can't rewind, so we need to get a new stream
            // and skip to the new position.
            this.stream.Close();
            this.stream = this.entry.Open();
            this.position = 0;
            if (newPosition > 0)
            {
                Advance(this.stream, newPosition);
                this.position = newPosition;
            }
        }

        return this.position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    private static void Advance(Stream s, long bytesToSkip)
    {
        byte[] buffer = new byte[4096];
        while (bytesToSkip > 0)
        {
            int bytesRead = s.Read(buffer, 0, (int)Math.Min(buffer.Length, bytesToSkip));
            if (bytesRead == 0)
            {
                break;
            }

            bytesToSkip -= bytesRead;
        }
    }

    [MemberNotNull(nameof(stream))]
    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(this.stream is null, this);
    }
}
