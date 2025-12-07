// <copyright file="MultiSourceDownloadService.cs" company="slskd Team">
//     Copyright (c) slskd Team. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU Affero General Public License as published
//     by the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU Affero General Public License for more details.
//
//     You should have received a copy of the GNU Affero General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace slskd.Transfers.MultiSource
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;
    using Soulseek;
    using IODirectory = System.IO.Directory;
    using IOPath = System.IO.Path;
    using FileStream = System.IO.FileStream;
    using FileMode = System.IO.FileMode;
    using FileAccess = System.IO.FileAccess;
    using Stream = System.IO.Stream;

    /// <summary>
    ///     Experimental multi-source download service.
    /// </summary>
    public class MultiSourceDownloadService : IMultiSourceDownloadService
    {
        /// <summary>
        ///     Default chunk size for parallel downloads (1MB).
        /// </summary>
        public const int DefaultChunkSize = 1024 * 1024;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultiSourceDownloadService"/> class.
        /// </summary>
        /// <param name="soulseekClient">The Soulseek client.</param>
        /// <param name="contentVerificationService">The content verification service.</param>
        public MultiSourceDownloadService(
            ISoulseekClient soulseekClient,
            IContentVerificationService contentVerificationService)
        {
            Client = soulseekClient;
            ContentVerification = contentVerificationService;
        }

        private ISoulseekClient Client { get; }
        private IContentVerificationService ContentVerification { get; }
        private ILogger Log { get; } = Serilog.Log.ForContext<MultiSourceDownloadService>();
        private ConcurrentDictionary<Guid, MultiSourceDownloadStatus> ActiveDownloads { get; } = new();

        /// <inheritdoc/>
        public async Task<ContentVerificationResult> FindVerifiedSourcesAsync(
            string filename,
            long fileSize,
            string excludeUsername = null,
            CancellationToken cancellationToken = default)
        {
            // Extract just the filename for searching
            var searchTerm = IOPath.GetFileNameWithoutExtension(filename);

            Log.Information("Searching for alternative sources: {SearchTerm}", searchTerm);

            // Search for the file
            var searchResults = new List<SearchResponse>();
            var searchOptions = new SearchOptions(
                filterResponses: true,
                minimumResponseFileCount: 1,
                responseLimit: 50);

            try
            {
                await Client.SearchAsync(
                    SearchQuery.FromText(searchTerm),
                    responseHandler: (response) => searchResults.Add(response),
                    options: searchOptions,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Search failed: {Message}", ex.Message);
            }

            // Find exact matches (same filename, same size)
            var originalFilename = IOPath.GetFileName(filename);
            var candidates = new List<string>();

            foreach (var response in searchResults)
            {
                if (excludeUsername != null && response.Username.Equals(excludeUsername, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var file in response.Files)
                {
                    var responseFilename = IOPath.GetFileName(file.Filename);
                    if (responseFilename.Equals(originalFilename, StringComparison.OrdinalIgnoreCase) &&
                        file.Size == fileSize)
                    {
                        if (!candidates.Contains(response.Username))
                        {
                            candidates.Add(response.Username);
                        }
                    }
                }
            }

            Log.Information("Found {Count} candidate sources with exact match", candidates.Count);

            if (candidates.Count == 0)
            {
                return new ContentVerificationResult
                {
                    Filename = filename,
                    FileSize = fileSize,
                };
            }

            // Verify sources
            return await ContentVerification.VerifySourcesAsync(
                new ContentVerificationRequest
                {
                    Filename = filename,
                    FileSize = fileSize,
                    CandidateUsernames = candidates,
                },
                cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<MultiSourceDownloadResult> DownloadAsync(
            MultiSourceDownloadRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new MultiSourceDownloadResult
            {
                Id = request.Id,
                Filename = request.Filename,
                OutputPath = request.OutputPath,
            };

            var stopwatch = Stopwatch.StartNew();

            var status = new MultiSourceDownloadStatus
            {
                Id = request.Id,
                Filename = request.Filename,
                FileSize = request.FileSize,
                State = MultiSourceDownloadState.Downloading,
            };
            ActiveDownloads[request.Id] = status;

            try
            {
                if (request.Sources.Count == 0)
                {
                    result.Error = "No verified sources provided";
                    result.Success = false;
                    return result;
                }

                // Calculate chunks
                var chunks = CalculateChunks(request.FileSize, request.Sources.Count);
                status.TotalChunks = chunks.Count;

                Log.Information(
                    "Starting multi-source download: {Filename} ({Size} bytes) from {Sources} sources in {Chunks} chunks",
                    request.Filename,
                    request.FileSize,
                    request.Sources.Count,
                    chunks.Count);

                // Create temp directory for chunks
                var tempDir = IOPath.Combine(IOPath.GetTempPath(), "slskdn-multidownload", request.Id.ToString());
                IODirectory.CreateDirectory(tempDir);

                // Download chunks in parallel
                var chunkTasks = new List<Task<ChunkResult>>();
                var sourceIndex = 0;

                foreach (var chunk in chunks)
                {
                    var source = request.Sources[sourceIndex % request.Sources.Count];
                    var chunkPath = IOPath.Combine(tempDir, $"chunk_{chunk.Index:D4}.bin");

                    chunkTasks.Add(DownloadChunkAsync(
                        source.Username,
                        request.Filename,
                        chunk.StartOffset,
                        chunk.EndOffset,
                        chunkPath,
                        status,
                        cancellationToken));

                    sourceIndex++;
                }

                var chunkResults = await Task.WhenAll(chunkTasks);
                result.Chunks = chunkResults.ToList();
                result.SourcesUsed = request.Sources.Count;

                // Check if all chunks succeeded
                var failedChunks = chunkResults.Where(c => !c.Success).ToList();
                if (failedChunks.Any())
                {
                    Log.Warning("{Count} chunks failed, attempting recovery...", failedChunks.Count);

                    // TODO: Retry failed chunks with different sources
                    result.Error = $"{failedChunks.Count} chunks failed to download";
                    result.Success = false;
                    status.State = MultiSourceDownloadState.Failed;
                    return result;
                }

                // Assemble chunks
                status.State = MultiSourceDownloadState.Assembling;
                Log.Information("Assembling {Count} chunks into final file", chunks.Count);

                await AssembleChunksAsync(tempDir, chunks.Count, request.OutputPath, cancellationToken);

                // Verify final file
                status.State = MultiSourceDownloadState.VerifyingFinal;
                var finalHash = await ComputeFileHashAsync(request.OutputPath, cancellationToken);
                result.FinalHash = finalHash;

                if (request.ExpectedHash != null && !finalHash.Equals(request.ExpectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Warning(
                        "Final hash mismatch! Expected: {Expected}, Got: {Actual}",
                        request.ExpectedHash,
                        finalHash);
                    result.Error = "Final hash verification failed";
                    result.Success = false;
                    status.State = MultiSourceDownloadState.Failed;
                    return result;
                }

                // Cleanup temp files
                try
                {
                    IODirectory.Delete(tempDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }

                stopwatch.Stop();
                result.TotalTimeMs = stopwatch.ElapsedMilliseconds;
                result.BytesDownloaded = request.FileSize;
                result.Success = true;
                status.State = MultiSourceDownloadState.Completed;

                Log.Information(
                    "Multi-source download complete: {Filename} in {Time}ms ({Speed:F2} MB/s) using {Sources} sources",
                    request.Filename,
                    result.TotalTimeMs,
                    (request.FileSize / 1024.0 / 1024.0) / (result.TotalTimeMs / 1000.0),
                    result.SourcesUsed);

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Multi-source download failed: {Message}", ex.Message);
                result.Error = ex.Message;
                result.Success = false;
                status.State = MultiSourceDownloadState.Failed;
                return result;
            }
            finally
            {
                ActiveDownloads.TryRemove(request.Id, out _);
            }
        }

        /// <inheritdoc/>
        public MultiSourceDownloadStatus GetStatus(Guid downloadId)
        {
            return ActiveDownloads.TryGetValue(downloadId, out var status) ? status : null;
        }

        private List<(int Index, long StartOffset, long EndOffset)> CalculateChunks(long fileSize, int sourceCount)
        {
            var chunks = new List<(int Index, long StartOffset, long EndOffset)>();

            // Use smaller chunks if we have more sources
            var chunkSize = Math.Max(DefaultChunkSize, fileSize / Math.Max(sourceCount * 2, 4));
            var offset = 0L;
            var index = 0;

            while (offset < fileSize)
            {
                var endOffset = Math.Min(offset + chunkSize, fileSize);
                chunks.Add((index, offset, endOffset));
                offset = endOffset;
                index++;
            }

            return chunks;
        }

        private async Task<ChunkResult> DownloadChunkAsync(
            string username,
            string filename,
            long startOffset,
            long endOffset,
            string outputPath,
            MultiSourceDownloadStatus status,
            CancellationToken cancellationToken)
        {
            var result = new ChunkResult
            {
                Username = username,
                StartOffset = startOffset,
                EndOffset = endOffset,
            };

            var stopwatch = Stopwatch.StartNew();
            status.IncrementActiveChunks();

            try
            {
                var size = endOffset - startOffset;

                Log.Debug(
                    "Downloading chunk from {Username}: {Start}-{End} ({Size} bytes)",
                    username,
                    startOffset,
                    endOffset,
                    size);

                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

                await Client.DownloadAsync(
                    username: username,
                    remoteFilename: filename,
                    outputStreamFactory: () => Task.FromResult<Stream>(fileStream),
                    size: size,
                    startOffset: startOffset,
                    cancellationToken: cancellationToken,
                    options: new TransferOptions(
                        maximumLingerTime: 3000,
                        disposeOutputStreamOnCompletion: false));

                stopwatch.Stop();
                result.BytesDownloaded = size;
                result.TimeMs = stopwatch.ElapsedMilliseconds;
                result.Success = true;

                status.AddBytesDownloaded(size);
                status.IncrementCompletedChunks();

                Log.Debug(
                    "Chunk complete from {Username}: {Size} bytes in {Time}ms ({Speed:F2} KB/s)",
                    username,
                    size,
                    result.TimeMs,
                    result.SpeedBps / 1024.0);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.TimeMs = stopwatch.ElapsedMilliseconds;
                result.Error = ex.Message;
                result.Success = false;

                Log.Warning(ex, "Chunk download failed from {Username}: {Message}", username, ex.Message);
                return result;
            }
            finally
            {
                status.DecrementActiveChunks();
            }
        }

        private async Task AssembleChunksAsync(string tempDir, int chunkCount, string outputPath, CancellationToken cancellationToken)
        {
            var outputDir = IOPath.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                IODirectory.CreateDirectory(outputDir);
            }

            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

            for (int i = 0; i < chunkCount; i++)
            {
                var chunkPath = IOPath.Combine(tempDir, $"chunk_{i:D4}.bin");
                using var chunkStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read);
                await chunkStream.CopyToAsync(outputStream, cancellationToken);
            }
        }

        private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
        {
            using var sha256 = SHA256.Create();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
