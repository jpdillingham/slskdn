// <copyright file="ContentVerificationService.cs" company="slskd Team">
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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;
    using Soulseek;

    /// <summary>
    ///     Service for verifying file content identity across multiple Soulseek sources.
    /// </summary>
    public class ContentVerificationService : IContentVerificationService
    {
        /// <summary>
        ///     Size of verification chunk for non-FLAC files (32KB).
        /// </summary>
        public const int VerificationChunkSize = 32768;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ContentVerificationService"/> class.
        /// </summary>
        /// <param name="soulseekClient">The Soulseek client.</param>
        public ContentVerificationService(ISoulseekClient soulseekClient)
        {
            Client = soulseekClient;
        }

        private ISoulseekClient Client { get; }
        private ILogger Log { get; } = Serilog.Log.ForContext<ContentVerificationService>();

        /// <inheritdoc/>
        public async Task<ContentVerificationResult> VerifySourcesAsync(
            ContentVerificationRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new ContentVerificationResult
            {
                Filename = request.Filename,
                FileSize = request.FileSize,
            };

            Log.Information(
                "Verifying {Count} sources for {Filename} ({Size} bytes)",
                request.CandidateUsernames.Count,
                request.Filename,
                request.FileSize);

            // Verify all candidates in parallel
            var verificationTasks = new List<Task<(string Username, string Hash, VerificationMethod Method, long TimeMs, string Error)>>();

            foreach (var username in request.CandidateUsernames)
            {
                verificationTasks.Add(VerifySingleSourceAsync(
                    username,
                    request.Filename,
                    request.FileSize,
                    request.TimeoutMs,
                    cancellationToken));
            }

            var verificationResults = await Task.WhenAll(verificationTasks);

            // Group results by hash
            foreach (var (username, hash, method, timeMs, error) in verificationResults)
            {
                if (error != null)
                {
                    result.FailedSources.Add(new FailedSource
                    {
                        Username = username,
                        Reason = error,
                    });
                    continue;
                }

                if (!result.SourcesByHash.TryGetValue(hash, out var sources))
                {
                    sources = new List<VerifiedSource>();
                    result.SourcesByHash[hash] = sources;
                }

                sources.Add(new VerifiedSource
                {
                    Username = username,
                    ContentHash = hash,
                    Method = method,
                    VerificationTimeMs = timeMs,
                });
            }

            Log.Information(
                "Verification complete: {HashGroups} hash groups, {Failed} failed, best group has {BestCount} sources",
                result.SourcesByHash.Count,
                result.FailedSources.Count,
                result.BestSources.Count);

            return result;
        }

        /// <inheritdoc/>
        public async Task<string> GetContentHashAsync(
            string username,
            string filename,
            long fileSize,
            CancellationToken cancellationToken = default)
        {
            var (_, hash, _, _, error) = await VerifySingleSourceAsync(
                username,
                filename,
                fileSize,
                30000,
                cancellationToken);

            return error == null ? hash : null;
        }

        private async Task<(string Username, string Hash, VerificationMethod Method, long TimeMs, string Error)> VerifySingleSourceAsync(
            string username,
            string filename,
            long fileSize,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Determine how much data we need
                var isFlac = filename.EndsWith(".flac", StringComparison.OrdinalIgnoreCase);
                var bytesNeeded = isFlac ? FlacStreamInfoParser.MinimumBytesNeeded : VerificationChunkSize;

                // Don't verify files smaller than our chunk size
                if (fileSize < bytesNeeded)
                {
                    return (username, null, default, stopwatch.ElapsedMilliseconds, "File too small for verification");
                }

                Log.Debug(
                    "Requesting {Bytes} bytes from {Username} for {Filename} (FLAC: {IsFlac})",
                    bytesNeeded,
                    username,
                    filename,
                    isFlac);

                // Download the verification chunk
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutMs);

                using var memoryStream = new MemoryStream(bytesNeeded);

                await Client.DownloadAsync(
                    username: username,
                    remoteFilename: filename,
                    outputStreamFactory: () => Task.FromResult<Stream>(memoryStream),
                    size: bytesNeeded,
                    startOffset: 0,
                    cancellationToken: cts.Token,
                    options: new TransferOptions(
                        maximumLingerTime: 1000,
                        disposeOutputStreamOnCompletion: false));

                var data = memoryStream.ToArray();

                // Compute hash based on file type
                string hash;
                VerificationMethod method;

                if (isFlac && FlacStreamInfoParser.TryParse(data, out var streamInfo))
                {
                    hash = streamInfo.AudioMd5Hex;
                    method = VerificationMethod.FlacStreamInfoMd5;

                    Log.Debug(
                        "FLAC verification for {Username}: MD5={Hash}, SampleRate={SampleRate}, Channels={Channels}",
                        username,
                        hash,
                        streamInfo.SampleRate,
                        streamInfo.Channels);
                }
                else
                {
                    // Fall back to SHA256 of content
                    using var sha256 = SHA256.Create();
                    var hashBytes = sha256.ComputeHash(data);
                    hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
                    method = VerificationMethod.ContentSha256;

                    Log.Debug(
                        "Content verification for {Username}: SHA256={Hash} (first {Bytes} bytes)",
                        username,
                        hash.Substring(0, 16) + "...",
                        data.Length);
                }

                stopwatch.Stop();
                return (username, hash, method, stopwatch.ElapsedMilliseconds, null);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                Log.Warning("Verification timeout for {Username} on {Filename}", username, filename);
                return (username, null, default, stopwatch.ElapsedMilliseconds, "Timeout");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.Warning(ex, "Verification failed for {Username} on {Filename}: {Message}", username, filename, ex.Message);
                return (username, null, default, stopwatch.ElapsedMilliseconds, ex.Message);
            }
        }
    }
}

