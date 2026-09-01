// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.CueToolsDatabase.Web;

using Woohoo.Audio.CueToolsDatabase.Web.Models;

internal static class CTDBSerialization
{
    public static CTDBResponse? Deserialize(TextReader textReader)
    {
        var settings = new XmlReaderSettings { IgnoreWhitespace = true };
        using var reader = XmlReader.Create(textReader, settings);

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "ctdb")
            {
                return ReadResponse(reader);
            }
        }

        return null;
    }

    private static CTDBResponse ReadResponse(XmlReader reader)
    {
        var status = reader.GetAttribute("status") ?? string.Empty;
        var updateUrl = reader.GetAttribute("updateurl") ?? string.Empty;
        var updateMsg = reader.GetAttribute("updatemsg") ?? string.Empty;
        var message = reader.GetAttribute("message") ?? string.Empty;
        var npar = ReadInt(reader, "npar") ?? 0;

        var entries = new List<CTDBResponseEntry>();
        var metadatas = new List<CTDBResponseMeta>();

        if (!reader.IsEmptyElement)
        {
            reader.Read(); // Advance to first child of <ctdb>

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName == "entry")
                    {
                        var entry = ReadEntry(reader);
                        entries.Add(entry);
                    }
                    else if (reader.LocalName == "metadata")
                    {
                        var meta = ReadMetadata(reader);
                        metadatas.Add(meta);
                    }
                    else
                    {
                        reader.Skip(); // Skip unknown ctdb child
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        var response = new CTDBResponse
        {
            Status = status,
            UpdateUrl = updateUrl,
            UpdateMsg = updateMsg,
            Message = message,
            Npar = npar,
            Entries = [.. entries],
            Metadatas = [.. metadatas],
        };

        return response;
    }

    private static CTDBResponseEntry ReadEntry(XmlReader reader)
    {
        var entry = new CTDBResponseEntry
        {
            Crc32 = reader.GetAttribute("crc32") ?? string.Empty,
            HasParity = reader.GetAttribute("hasparity") ?? string.Empty,
            Parity = reader.GetAttribute("parity") ?? string.Empty,
            Syndrome = reader.GetAttribute("syndrome") ?? string.Empty,
            TrackCrcs = reader.GetAttribute("trackcrcs") ?? string.Empty,
            Toc = reader.GetAttribute("toc") ?? string.Empty,
            Id = ReadInt(reader, "id") ?? 0,
            Confidence = ReadInt(reader, "confidence") ?? 0,
            Npar = ReadInt(reader, "npar") ?? 0,
            Stride = ReadInt(reader, "stride") ?? 0,
        };

        reader.Skip(); // Skip children if any, positions on next sibling

        return entry;
    }

    private static CTDBResponseMeta ReadMetadata(XmlReader reader)
    {
        var source = reader.GetAttribute("source") ?? string.Empty;
        var id = reader.GetAttribute("id") ?? string.Empty;
        var artist = reader.GetAttribute("artist") ?? string.Empty;
        var album = reader.GetAttribute("album") ?? string.Empty;
        var year = reader.GetAttribute("year") ?? string.Empty;
        var genre = reader.GetAttribute("genre") ?? string.Empty;
        var discNumber = reader.GetAttribute("discnumber") ?? string.Empty;
        var discCount = reader.GetAttribute("disccount") ?? string.Empty;
        var discName = reader.GetAttribute("discname") ?? string.Empty;
        var infoUrl = reader.GetAttribute("infourl") ?? string.Empty;
        var barcode = reader.GetAttribute("barcode") ?? string.Empty;
        string extra = string.Empty;

        var coverArts = new List<CTDBResponseMetaImage>();
        var tracks = new List<CTDBResponseMetaTrack>();
        var labels = new List<CTDBResponseMetaLabel>();
        var releases = new List<CTDBResponseMetaRelease>();

        if (reader.IsEmptyElement)
        {
            reader.Skip();
        }
        else
        {
            reader.Read(); // Advance to first child of <metadata>

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName == "extra")
                    {
                        // This reads the value AND moves the reader to the next node automatically
                        extra = reader.ReadElementContentAsString();
                    }
                    else if (reader.LocalName == "coverart")
                    {
                        var img = ReadCoverArt(reader);
                        coverArts.Add(img);
                    }
                    else if (reader.LocalName == "track")
                    {
                        var track = ReadTrack(reader);
                        tracks.Add(track);
                    }
                    else if (reader.LocalName == "label")
                    {
                        var label = ReadLabel(reader);
                        labels.Add(label);
                    }
                    else if (reader.LocalName == "release")
                    {
                        var release = ReadRelease(reader);
                        releases.Add(release);
                    }
                    else
                    {
                        reader.Skip(); // Skip unknown metadata child
                    }
                }
                else
                {
                    reader.Read(); // Advance past whitespace/text
                }
            }

            reader.Read(); // Consume </metadata>
        }

        var meta = new CTDBResponseMeta
        {
            Source = source,
            Id = id,
            Artist = artist,
            Album = album,
            Year = year,
            Genre = genre,
            DiscNumber = discNumber,
            DiscCount = discCount,
            DiscName = discName,
            InfoUrl = infoUrl,
            Barcode = barcode,
            Extra = extra,
            CoverArts = [.. coverArts],
            Tracks = [.. tracks],
            Labels = [.. labels],
            Releases = [.. releases],
        };

        return meta;
    }

    private static CTDBResponseMetaImage ReadCoverArt(XmlReader reader)
    {
        var img = new CTDBResponseMetaImage
        {
            Uri = reader.GetAttribute("uri") ?? string.Empty,
            Uri150 = reader.GetAttribute("uri150") ?? string.Empty,
            Height = ReadInt(reader, "height") ?? 0,
            Width = ReadInt(reader, "width") ?? 0,
            Primary = ReadBool(reader, "primary") ?? false,
        };

        reader.Skip();

        return img;
    }

    private static CTDBResponseMetaRelease ReadRelease(XmlReader reader)
    {
        var release = new CTDBResponseMetaRelease
        {
            Date = reader.GetAttribute("date") ?? string.Empty,
            Country = reader.GetAttribute("country") ?? string.Empty,
        };

        reader.Skip();

        return release;
    }

    private static CTDBResponseMetaLabel ReadLabel(XmlReader reader)
    {
        var label = new CTDBResponseMetaLabel
        {
            Name = reader.GetAttribute("name") ?? string.Empty,
            CatNo = reader.GetAttribute("catno") ?? string.Empty,
        };

        reader.Skip();

        return label;
    }

    private static CTDBResponseMetaTrack ReadTrack(XmlReader reader)
    {
        var name = reader.GetAttribute("name") ?? string.Empty;
        var artist = reader.GetAttribute("artist") ?? string.Empty;
        string extra = string.Empty;

        if (reader.IsEmptyElement)
        {
            reader.Skip();
        }
        else
        {
            reader.Read();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName == "extra")
                    {
                        extra = reader.ReadElementContentAsString();
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            reader.Read(); // Consume </track>
        }

        var track = new CTDBResponseMetaTrack
        {
            Name = name,
            Artist = artist,
            Extra = extra,
        };

        return track;
    }

    private static int? ReadInt(XmlReader reader, string attributeName)
    {
        if (int.TryParse(reader.GetAttribute(attributeName), out int attributeVal))
        {
            return attributeVal;
        }

        return null;
    }

    private static bool? ReadBool(XmlReader reader, string attributeName)
    {
        if (int.TryParse(reader.GetAttribute(attributeName), out int attributeVal))
        {
            return attributeVal != 0;
        }

        return null;
    }
}
