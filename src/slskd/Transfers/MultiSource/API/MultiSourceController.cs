// <copyright file="MultiSourceController.cs" company="slskd Team">
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

namespace slskd.Transfers.MultiSource.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Asp.Versioning;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Serilog;
    using Soulseek;
    using IOPath = System.IO.Path;

    /// <summary>
    ///     Experimental multi-source download API.
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("0")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class MultiSourceController : ControllerBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MultiSourceController"/> class.
        /// </summary>
        /// <param name="multiSourceService">The multi-source download service.</param>
        /// <param name="soulseekClient">The Soulseek client.</param>
        public MultiSourceController(
            IMultiSourceDownloadService multiSourceService,
            ISoulseekClient soulseekClient)
        {
            MultiSource = multiSourceService;
            Client = soulseekClient;
        }

        private IMultiSourceDownloadService MultiSource { get; }
        private ISoulseekClient Client { get; }
        private ILogger Log { get; } = Serilog.Log.ForContext<MultiSourceController>();

        /// <summary>
        ///     Searches for files and returns candidates for multi-source download.
        /// </summary>
        /// <param name="searchText">The search query.</param>
        /// <returns>Search results with verification info.</returns>
        [HttpGet("search")]
        [Authorize(Policy = AuthPolicy.Any)]
        public async Task<IActionResult> Search([FromQuery] string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return BadRequest("Search text is required");
            }

            Log.Information("[MultiSource] Searching for: {SearchText}", searchText);

            var searchResults = new List<SearchResponse>();
            var searchOptions = new SearchOptions(
                filterResponses: true,
                minimumResponseFileCount: 1,
                responseLimit: 100);

            try
            {
                await Client.SearchAsync(
                    SearchQuery.FromText(searchText),
                    responseHandler: (response) => searchResults.Add(response),
                    options: searchOptions);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[MultiSource] Search failed: {Message}", ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }

            // Group files by filename + size (potential multi-source candidates)
            var fileGroups = new Dictionary<string, MultiSourceCandidate>();

            foreach (var response in searchResults)
            {
                foreach (var file in response.Files)
                {
                    var filename = IOPath.GetFileName(file.Filename);
                    var key = $"{filename}|{file.Size}";

                    if (!fileGroups.TryGetValue(key, out var candidate))
                    {
                        candidate = new MultiSourceCandidate
                        {
                            Filename = filename,
                            Size = file.Size,
                            Extension = IOPath.GetExtension(filename).ToLowerInvariant(),
                            Sources = new List<SourceInfo>(),
                        };
                        fileGroups[key] = candidate;
                    }

                    // Store full path for first occurrence
                    if (string.IsNullOrEmpty(candidate.FullPath))
                    {
                        candidate.FullPath = file.Filename;
                    }

                    candidate.Sources.Add(new SourceInfo
                    {
                        Username = response.Username,
                        FullPath = file.Filename,
                        HasFreeUploadSlot = response.HasFreeUploadSlot,
                        QueueLength = (int)response.QueueLength,
                        UploadSpeed = response.UploadSpeed,
                        BitRate = file.BitRate,
                        SampleRate = file.SampleRate,
                        BitDepth = file.BitDepth,
                    });
                }
            }

            // Sort by source count (most sources first) and return top candidates
            var candidates = fileGroups.Values
                .Where(c => c.Sources.Count >= 2) // Only files with multiple sources
                .OrderByDescending(c => c.Sources.Count)
                .ThenByDescending(c => c.Size)
                .Take(50)
                .ToList();

            Log.Information(
                "[MultiSource] Found {Total} files, {Candidates} with multiple sources",
                fileGroups.Count,
                candidates.Count);

            return Ok(new
            {
                query = searchText,
                totalFiles = fileGroups.Count,
                multiSourceCandidates = candidates.Count,
                candidates,
            });
        }

        /// <summary>
        ///     Verifies sources for a specific file.
        /// </summary>
        /// <param name="request">The verification request.</param>
        /// <returns>Verification results with sources grouped by content hash.</returns>
        [HttpPost("verify")]
        [Authorize(Policy = AuthPolicy.Any)]
        public async Task<IActionResult> VerifySources([FromBody] VerifyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Filename))
            {
                return BadRequest("Filename is required");
            }

            if (request.Usernames == null || request.Usernames.Count == 0)
            {
                return BadRequest("At least one username is required");
            }

            Log.Information(
                "[MultiSource] Verifying {Count} sources for {Filename}",
                request.Usernames.Count,
                request.Filename);

            var result = await MultiSource.FindVerifiedSourcesAsync(
                request.Filename,
                request.FileSize,
                cancellationToken: HttpContext.RequestAborted);

            return Ok(result);
        }

        /// <summary>
        ///     Starts a multi-source download.
        /// </summary>
        /// <param name="request">The download request.</param>
        /// <returns>Download result.</returns>
        [HttpPost("download")]
        [Authorize(Policy = AuthPolicy.Any)]
        public async Task<IActionResult> Download([FromBody] DownloadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Filename))
            {
                return BadRequest("Filename is required");
            }

            if (request.Sources == null || request.Sources.Count < 2)
            {
                return BadRequest("At least 2 verified sources are required");
            }

            var outputPath = IOPath.Combine(
                IOPath.GetTempPath(),
                "slskdn-test",
                IOPath.GetFileName(request.Filename));

            Log.Information(
                "[MultiSource] Starting download of {Filename} from {Count} sources",
                request.Filename,
                request.Sources.Count);

            var downloadRequest = new MultiSourceDownloadRequest
            {
                Filename = request.Filename,
                FileSize = request.FileSize,
                ExpectedHash = request.ExpectedHash,
                OutputPath = outputPath,
                Sources = request.Sources.Select(s => new VerifiedSource
                {
                    Username = s.Username,
                    ContentHash = s.ContentHash,
                    Method = s.Method,
                }).ToList(),
            };

            var result = await MultiSource.DownloadAsync(downloadRequest, HttpContext.RequestAborted);

            return Ok(result);
        }

        /// <summary>
        ///     One-click test: search, verify, and download.
        /// </summary>
        /// <param name="request">The test request.</param>
        /// <returns>Complete test results.</returns>
        [HttpPost("test")]
        [Authorize(Policy = AuthPolicy.Any)]
        public async Task<IActionResult> RunTest([FromBody] TestRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.SearchText))
            {
                return BadRequest("Search text is required");
            }

            var testResult = new TestResult
            {
                SearchText = request.SearchText,
                StartedAt = DateTime.UtcNow,
            };

            Log.Information("[MultiSource] Starting test for: {SearchText}", request.SearchText);

            // Step 1: Search
            var searchResults = new List<SearchResponse>();
            try
            {
                await Client.SearchAsync(
                    SearchQuery.FromText(request.SearchText),
                    responseHandler: (r) => searchResults.Add(r),
                    options: new SearchOptions(filterResponses: true, minimumResponseFileCount: 1, responseLimit: 100));

                testResult.SearchResponseCount = searchResults.Count;
            }
            catch (Exception ex)
            {
                testResult.Error = $"Search failed: {ex.Message}";
                return Ok(testResult);
            }

            // Step 2: Find best candidate (most sources, FLAC preferred)
            var candidates = new Dictionary<string, (string Filename, long Size, List<string> Users)>();

            foreach (var response in searchResults)
            {
                foreach (var file in response.Files)
                {
                    var fname = IOPath.GetFileName(file.Filename);
                    var key = $"{fname}|{file.Size}";

                    if (!candidates.TryGetValue(key, out var c))
                    {
                        c = (file.Filename, file.Size, new List<string>());
                        candidates[key] = c;
                    }

                    if (!c.Users.Contains(response.Username))
                    {
                        c.Users.Add(response.Username);
                    }
                }
            }

            var bestCandidate = candidates.Values
                .Where(c => c.Users.Count >= 2)
                .OrderByDescending(c => c.Filename.EndsWith(".flac", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenByDescending(c => c.Users.Count)
                .FirstOrDefault();

            if (bestCandidate.Filename == null)
            {
                testResult.Error = "No files with multiple sources found";
                return Ok(testResult);
            }

            testResult.SelectedFile = IOPath.GetFileName(bestCandidate.Filename);
            testResult.FileSize = bestCandidate.Size;
            testResult.CandidateSources = bestCandidate.Users.Count;

            Log.Information(
                "[MultiSource] Best candidate: {File} ({Size} bytes) with {Sources} sources",
                testResult.SelectedFile,
                testResult.FileSize,
                testResult.CandidateSources);

            // Step 3: Verify sources
            var verificationResult = await MultiSource.FindVerifiedSourcesAsync(
                bestCandidate.Filename,
                bestCandidate.Size,
                cancellationToken: HttpContext.RequestAborted);

            testResult.VerifiedSources = verificationResult.BestSources.Count;
            testResult.VerificationMethod = verificationResult.BestSources.FirstOrDefault()?.Method.ToString();
            testResult.ContentHash = verificationResult.BestHash;

            if (verificationResult.BestSources.Count < 2)
            {
                testResult.Error = $"Not enough verified sources (got {verificationResult.BestSources.Count})";
                return Ok(testResult);
            }

            // Step 4: Download
            var outputPath = IOPath.Combine(
                IOPath.GetTempPath(),
                "slskdn-test",
                $"multitest_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{IOPath.GetFileName(bestCandidate.Filename)}");

            var downloadRequest = new MultiSourceDownloadRequest
            {
                Filename = bestCandidate.Filename,
                FileSize = bestCandidate.Size,
                ExpectedHash = verificationResult.BestHash,
                OutputPath = outputPath,
                Sources = verificationResult.BestSources,
            };

            var downloadResult = await MultiSource.DownloadAsync(downloadRequest, HttpContext.RequestAborted);

            testResult.DownloadSuccess = downloadResult.Success;
            testResult.DownloadTimeMs = downloadResult.TotalTimeMs;
            testResult.BytesDownloaded = downloadResult.BytesDownloaded;
            testResult.SourcesUsed = downloadResult.SourcesUsed;
            testResult.OutputPath = downloadResult.OutputPath;
            testResult.FinalHash = downloadResult.FinalHash;
            testResult.CompletedAt = DateTime.UtcNow;

            if (downloadResult.TotalTimeMs > 0)
            {
                testResult.AverageSpeedMBps = (downloadResult.BytesDownloaded / 1024.0 / 1024.0) / (downloadResult.TotalTimeMs / 1000.0);
            }

            if (!downloadResult.Success)
            {
                testResult.Error = downloadResult.Error;
            }

            Log.Information(
                "[MultiSource] Test complete: {Success}, {Size} bytes in {Time}ms ({Speed:F2} MB/s)",
                testResult.DownloadSuccess,
                testResult.BytesDownloaded,
                testResult.DownloadTimeMs,
                testResult.AverageSpeedMBps);

            return Ok(testResult);
        }
    }

    /// <summary>
    ///     A candidate file for multi-source download.
    /// </summary>
    public class MultiSourceCandidate
    {
        /// <summary>Gets or sets the filename.</summary>
        public string Filename { get; set; }

        /// <summary>Gets or sets the full path from first source.</summary>
        public string FullPath { get; set; }

        /// <summary>Gets or sets the file size.</summary>
        public long Size { get; set; }

        /// <summary>Gets or sets the file extension.</summary>
        public string Extension { get; set; }

        /// <summary>Gets or sets the list of sources.</summary>
        public List<SourceInfo> Sources { get; set; }

        /// <summary>Gets the source count.</summary>
        public int SourceCount => Sources?.Count ?? 0;
    }

    /// <summary>
    ///     Information about a source.
    /// </summary>
    public class SourceInfo
    {
        /// <summary>Gets or sets the username.</summary>
        public string Username { get; set; }

        /// <summary>Gets or sets the full path.</summary>
        public string FullPath { get; set; }

        /// <summary>Gets or sets whether user has free upload slots.</summary>
        public bool HasFreeUploadSlot { get; set; }

        /// <summary>Gets or sets queue length.</summary>
        public int QueueLength { get; set; }

        /// <summary>Gets or sets upload speed.</summary>
        public int UploadSpeed { get; set; }

        /// <summary>Gets or sets bit rate.</summary>
        public int? BitRate { get; set; }

        /// <summary>Gets or sets sample rate.</summary>
        public int? SampleRate { get; set; }

        /// <summary>Gets or sets bit depth.</summary>
        public int? BitDepth { get; set; }
    }

    /// <summary>
    ///     Request to verify sources.
    /// </summary>
    public class VerifyRequest
    {
        /// <summary>Gets or sets the filename.</summary>
        public string Filename { get; set; }

        /// <summary>Gets or sets the file size.</summary>
        public long FileSize { get; set; }

        /// <summary>Gets or sets the usernames to verify.</summary>
        public List<string> Usernames { get; set; }
    }

    /// <summary>
    ///     Request to download from multiple sources.
    /// </summary>
    public class DownloadRequest
    {
        /// <summary>Gets or sets the filename.</summary>
        public string Filename { get; set; }

        /// <summary>Gets or sets the file size.</summary>
        public long FileSize { get; set; }

        /// <summary>Gets or sets the expected content hash.</summary>
        public string ExpectedHash { get; set; }

        /// <summary>Gets or sets the verified sources.</summary>
        public List<SourceRequest> Sources { get; set; }
    }

    /// <summary>
    ///     A source in a download request.
    /// </summary>
    public class SourceRequest
    {
        /// <summary>Gets or sets the username.</summary>
        public string Username { get; set; }

        /// <summary>Gets or sets the content hash.</summary>
        public string ContentHash { get; set; }

        /// <summary>Gets or sets the verification method.</summary>
        public VerificationMethod Method { get; set; }
    }

    /// <summary>
    ///     Request for a one-click test.
    /// </summary>
    public class TestRequest
    {
        /// <summary>Gets or sets the search text.</summary>
        public string SearchText { get; set; }
    }

    /// <summary>
    ///     Result of a multi-source test.
    /// </summary>
    public class TestResult
    {
        /// <summary>Gets or sets the search text.</summary>
        public string SearchText { get; set; }

        /// <summary>Gets or sets when the test started.</summary>
        public DateTime StartedAt { get; set; }

        /// <summary>Gets or sets when the test completed.</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>Gets or sets the search response count.</summary>
        public int SearchResponseCount { get; set; }

        /// <summary>Gets or sets the selected file.</summary>
        public string SelectedFile { get; set; }

        /// <summary>Gets or sets the file size.</summary>
        public long FileSize { get; set; }

        /// <summary>Gets or sets the number of candidate sources.</summary>
        public int CandidateSources { get; set; }

        /// <summary>Gets or sets the number of verified sources.</summary>
        public int VerifiedSources { get; set; }

        /// <summary>Gets or sets the verification method used.</summary>
        public string VerificationMethod { get; set; }

        /// <summary>Gets or sets the content hash.</summary>
        public string ContentHash { get; set; }

        /// <summary>Gets or sets whether download succeeded.</summary>
        public bool DownloadSuccess { get; set; }

        /// <summary>Gets or sets the download time in ms.</summary>
        public long DownloadTimeMs { get; set; }

        /// <summary>Gets or sets bytes downloaded.</summary>
        public long BytesDownloaded { get; set; }

        /// <summary>Gets or sets sources used.</summary>
        public int SourcesUsed { get; set; }

        /// <summary>Gets or sets the output path.</summary>
        public string OutputPath { get; set; }

        /// <summary>Gets or sets the final hash.</summary>
        public string FinalHash { get; set; }

        /// <summary>Gets or sets average speed in MB/s.</summary>
        public double AverageSpeedMBps { get; set; }

        /// <summary>Gets or sets the error message.</summary>
        public string Error { get; set; }
    }
}
