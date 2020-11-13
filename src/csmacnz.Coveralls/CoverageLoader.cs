﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Beefeater;
using csmacnz.Coveralls.Data;
using csmacnz.Coveralls.Parsers;
using csmacnz.Coveralls.Ports;
using JetBrains.Annotations;

namespace csmacnz.Coveralls
{
    public class CoverageLoader
    {
        private readonly IFileLoader _fileLoader;

        public CoverageLoader(IFileLoader fileLoader)
        {
            _fileLoader = fileLoader;
        }

        public Result<List<CoverageFile>, LoadCoverageFilesError> LoadCoverageFiles(
            CoverageMode mode,
            PathProcessor pathProcessor,
            string modeInput,
            bool useRelativePaths)
        {
            var loadResult = LoadCoverageData(mode, modeInput);
            if (loadResult.Successful)
            {
                List<FileCoverageData> coverageData = loadResult.Value;

                var files = BuildCoverageFiles(pathProcessor, useRelativePaths, coverageData);

                return files;
            }
            else
            {
                return loadResult.Error;
            }
        }

        private Result<List<FileCoverageData>, LoadCoverageFilesError> LoadCoverageData(CoverageMode mode, string modeInput)
        {
            Option<List<FileCoverageData>> result;
            switch (mode)
            {
                case CoverageMode.MonoCov:
                    result = LoadData(modeInput, MonoCoverParser.GenerateSourceFiles);
                    break;
                case CoverageMode.ReportGenerator:
                    result = LoadData(modeInput, ReportGeneratorParser.GenerateSourceFiles);
                    break;
                case CoverageMode.Chutzpah:
                    result = LoadData(modeInput, ChutzpahJsonParser.GenerateSourceFiles);
                    break;
                case CoverageMode.LCov:
                    result = LoadData(modeInput, LcovParser.GenerateSourceFiles);
                    break;
                case CoverageMode.NCover:
                    result = LoadData(modeInput, NCoverParser.GenerateSourceFiles);
                    break;
                case CoverageMode.OpenCover:
                    result = LoadData(modeInput, OpenCoverParser.GenerateSourceFiles);
                    break;
                case CoverageMode.DynamicCodeCoverage:
                    result = LoadData(modeInput, DynamicCodeCoverageParser.GenerateSourceFiles);
                    break;
                case CoverageMode.ExportCodeCoverage:
                    result = LoadData(modeInput, ExportCodeCoverageParser.GenerateSourceFiles);
                    break;
                default:
                    return LoadCoverageFilesError.ModeNotSupported;
            }

            if (!result.HasValue)
            {
                return LoadCoverageFilesError.InputFileNotFound;
            }

            List<FileCoverageData> coverageData = (List<FileCoverageData>)result;

            if (coverageData == null)
            {
                return LoadCoverageFilesError.UnknownFilesMissingError;
            }

            return coverageData;
        }

        private Option<List<FileCoverageData>> LoadData(string directory, [NotNull] Func<Dictionary<string, XDocument>, List<FileCoverageData>> generateFunc)
        {
            if (generateFunc == null)
            {
                throw new ArgumentNullException(nameof(generateFunc));
            }

            var folderFiles = _fileLoader.GetFiles(directory);
            if (!folderFiles.HasValue)
            {
                return Option<List<FileCoverageData>>.None;
            }

            var documents = LoadXDocuments(folderFiles);

            return generateFunc(documents);
        }

        private Option<List<FileCoverageData>> LoadData(string modeInput, Func<string[], List<FileCoverageData>> generateFunc)
        {
            if (generateFunc == null)
            {
                throw new ArgumentNullException(nameof(generateFunc));
            }

            var lines = _fileLoader.TryReadAllLinesFromFile(modeInput);

            if (!lines.HasValue)
            {
                return Option<List<FileCoverageData>>.None;
            }

            return generateFunc((string[])lines);
        }

        private Option<List<FileCoverageData>> LoadData(string modeInput, Func<XDocument, List<FileCoverageData>> generateFunc)
        {
            if (generateFunc == null)
            {
                throw new ArgumentNullException(nameof(generateFunc));
            }

            var document = _fileLoader.TryLoadFile(modeInput);

            if (!document.HasValue)
            {
                return Option<List<FileCoverageData>>.None;
            }

            return generateFunc(XDocument.Parse((string)document));
        }

        private Dictionary<string, XDocument> LoadXDocuments(Option<FileInfo[]> folderFiles)
        {
            return ((FileInfo[])folderFiles).Where(f => f.Name.EndsWith(".xml", StringComparison.Ordinal))
                .ToDictionary(f => f.Name, f => XDocument.Parse((string)_fileLoader.TryLoadFile(f.FullName)));
        }

        private List<CoverageFile> BuildCoverageFiles(PathProcessor pathProcessor, bool useRelativePaths, List<FileCoverageData> coverageData)
        {
            // This needs attention, since this is optional if source is on public github
            var files = coverageData.Select(coverageFileData =>
            {
                var coverageBuilder = new CoverageFileBuilder(coverageFileData);

                var path = coverageFileData.FullPath;
                if (useRelativePaths)
                {
                    path = pathProcessor.ConvertPath(coverageFileData.FullPath);
                }

                path = PathProcessor.UnixifyPath(path);
                coverageBuilder.SetPath(path);

                if (!coverageBuilder.HasSource())
                {
                    var readAllText = _fileLoader.TryLoadFile(coverageFileData.FullPath);
                    if (readAllText.HasValue)
                    {
                        coverageBuilder.AddSource((string)readAllText);
                    }
                }

                var coverageFile = coverageBuilder.CreateFile();
                return coverageFile;
            }).ToList();
            return files;
        }
    }
}
