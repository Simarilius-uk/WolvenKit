﻿using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using WolvenKit.Common;
using WolvenKit.Common.Conversion;
using WolvenKit.Common.FNV1A;
using WolvenKit.Core.Interfaces;
using WolvenKit.RED4.Archive;
using WolvenKit.RED4.Archive.CR2W;
using WolvenKit.RED4.CR2W;
using WolvenKit.RED4.CR2W.JSON;

namespace WolvenKit.Modkit.Scripting;

/// <summary>
/// TODO
/// </summary>
public class WKitScripting
{
    protected readonly ILoggerService _loggerService;
    protected readonly IArchiveManager _archiveManager;
    protected readonly Red4ParserService _redParserService;

    public WKitScripting(ILoggerService loggerService, IArchiveManager archiveManager, Red4ParserService parserService)
    {
        _loggerService = loggerService;
        _archiveManager = archiveManager;
        _redParserService = parserService;
    }

    /// <summary>
    /// Gets a list of the files available in the game archives
    /// Note to myself: Don't use IEnumerable<T>
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerable GetArchiveFiles()
    {
        foreach (var archive in _archiveManager.Archives.Items)
        {
            foreach (var (key, file) in archive.Files)
            {
                yield return file as FileEntry;
            }
        }
    }

    /// <summary>
    /// Loads a file from the base archives using either a file path or hash
    /// </summary>
    /// <param name="path">The path of the file to retrieve</param>
    /// <returns></returns>
    [Description("GetFileFromBase")]
    public virtual IGameFile? GetFileFromBase(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        if (!ulong.TryParse(path, out var hash))
        {
            hash = FNV1A64HashAlgorithm.HashString(path);
        }

        return GetFileFromBase(hash);
    }

    /// <summary>
    /// Loads a file from the base archives using either a file path or hash
    /// </summary>
    /// <param name="hash">The hash of the file to retrieve</param>
    /// <returns></returns>
    public virtual IGameFile? GetFileFromBase(ulong hash)
    {
        var file = _archiveManager.Lookup(hash);
        return file.HasValue ? file.Value : null;
    }

    /// <summary>
    /// Creates a json representation of the specifed game file.
    /// </summary>
    /// <param name="gameFile">The gameFile which should be converted</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    [Description("GameFileToJson")]
    public virtual string? GameFileToJson(IGameFile gameFile)
    {
        if (gameFile == null)
        {
            throw new ArgumentNullException(nameof(gameFile));
        }

        using var ms = new MemoryStream();
        gameFile.Extract(ms);
        ms.Position = 0;

        if (_redParserService.TryReadRed4File(ms, out var cr2w))
        {
            var dto = new RedFileDto(cr2w);
            return RedJsonSerializer.Serialize(dto);
        }

        return null;
    }

    /// <summary>
    /// Creates a CR2W game file from a json
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public virtual CR2WFile? JsonToCR2W(string json) => RedJsonSerializer.Deserialize<RedFileDto>(json)?.Data;

    /// <summary>
    /// Changes the extension of the provided string path
    /// </summary>
    /// <param name="path"></param>
    /// <param name="extension"></param>
    /// <returns></returns>
    public virtual string ChangeExtension(string path, string extension) => Path.ChangeExtension(path, extension);

    /// <summary>
    /// Check if file exists in the game archives
    /// </summary>
    /// <param name="path">file path to check</param>
    /// <returns></returns>
    public virtual bool FileExistsInArchive(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        if (!ulong.TryParse(path, out var hash))
        {
            hash = FNV1A64HashAlgorithm.HashString(path);
        }

        return FileExistsInArchive(hash);
    }

    /// <summary>
    /// Check if file exists in the game archives
    /// </summary>
    /// <param name="hash">hash value to be checked</param>
    /// <returns></returns>
    public virtual bool FileExistsInArchive(ulong hash) => hash != 0 && _archiveManager.Lookup(hash).HasValue;
}