using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RobloxFiles;
using RobloxFiles.DataTypes;

namespace Source2Roblox.Util
{
    public sealed class RobloxAssetResolver
    {
        private const string AssetsUrl = "https://apis.roblox.com/assets/v1/assets";
        private const string OperationsUrl = "https://apis.roblox.com/assets/v1/operations";
        private const int UploadIntervalMs = 510;
        private const int MaxUploadRetries = 5;
        private const int MaxPollRetries = 3;
        private const int BaseRetryDelayMs = 1000;

        private static readonly HttpClient Client = new HttpClient();
        private static readonly SemaphoreSlim UploadSlots = new SemaphoreSlim(8, 8);
        private static readonly object RateLock = new object();
        private static DateTime nextUploadStart = DateTime.MinValue;
        private static readonly Random Jitter = new Random();

        // Failed upload tracking
        private static readonly object FailureLock = new object();
        private static readonly List<string> FailedUploads = new List<string>();
        private static int SucceededUploads = 0;
        private static int AttemptedUploads = 0;

        private readonly string rootDir;
        private readonly string localAssetRoot;
        public RobloxAssetResolver(string rootDir, string localAssetRoot)
        {
            this.rootDir = rootDir;
            this.localAssetRoot = localAssetRoot;
        }

        /// <summary>Resets upload counters between conversion runs.</summary>
        public static void ResetCounters()
        {
            lock (FailureLock)
            {
                FailedUploads.Clear();
                SucceededUploads = 0;
                AttemptedUploads = 0;
            }
        }

        /// <summary>Emits a summary of upload results. Call after WaitForUploads.</summary>
        public static void EmitUploadSummary()
        {
            int attempted, succeeded;
            string[] failures;

            lock (FailureLock)
            {
                attempted = AttemptedUploads;
                succeeded = SucceededUploads;
                failures = FailedUploads.ToArray();
            }

            if (attempted == 0)
                return;

            int failed = failures.Length;

            if (failed == 0)
            {
                Program.Emit("raw", $"Upload complete: {succeeded}/{attempted} assets uploaded successfully.");
            }
            else
            {
                Program.Emit("warning", $"Upload complete: {succeeded}/{attempted} succeeded, {failed} failed.", new
                {
                    failedAssets = failures
                });

                foreach (string path in failures)
                    Program.Emit("raw", $"  FAILED: {path}");
            }
        }

        public void BindTexture(string localPath, List<Task> uploadPool, Instance target, string property)
        {
            Bind(localPath, "Image", "image/png", uploadPool, target, property);
        }

        public void BindTexture(string localPath, List<Task> uploadPool, Property targetProperty)
        {
            Bind(localPath, "Image", "image/png", uploadPool, targetProperty);
        }

        public void BindMesh(string localPath, List<Task> uploadPool, Instance target, string property)
        {
            if (!Program.UploadMeshes)
            {
                SetProperty(target, property, LocalAsset(localPath));
                return;
            }

            Bind(localPath, "Mesh", "model/x-file-mesh-data", uploadPool, target, property);
        }

        private void Bind(
            string localPath,
            string assetType,
            string contentType,
            List<Task> uploadPool,
            Instance target,
            string property
        )
        {
            string normPath = NormalizeLocalPath(localPath);
            bool isMesh = string.Equals(assetType, "Mesh", StringComparison.OrdinalIgnoreCase);

            bool isExcluded = false;
            if (!isMesh)
            {
                foreach (var excluded in Program.ExcludedTextures)
                {
                    if (normPath.IndexOf(excluded, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isExcluded = true;
                        break;
                    }
                }
            }

            bool uploadEnabled = isMesh ? Program.UploadMeshes : Program.UploadAssets;

            if (isExcluded || !uploadEnabled || !Program.HasRobloxUploadCredentials)
            {
                if (isExcluded)
                    Program.Emit("raw", $"Skipping cloud upload for excluded asset: {localPath}");

                SetProperty(target, property, LocalAsset(normPath));
                return;
            }

            // Enqueue the upload as a task into the pool for parallel execution
            var task = Task.Run(async () =>
            {
                string assetId;

                try
                {
                    assetId = await ResolveAsync(localPath, assetType, contentType).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Program.Emit("raw", $"Roblox rejected {localPath} after retries; using the local asset instead. {e.Message}");
                    TrackFailure(localPath);
                    SetProperty(target, property, LocalAsset(normPath));
                    return;
                }

                if (string.IsNullOrEmpty(assetId))
                {
                    // File was missing — fall back to local asset instead of leaving property empty
                    SetProperty(target, property, LocalAsset(normPath));
                    return;
                }

                RequireUploadedAsset(assetId, localPath);
                SetProperty(target, property, assetId);
                TrackSuccess();
            });

            uploadPool.Add(task);
        }

        private void Bind(
            string localPath,
            string assetType,
            string contentType,
            List<Task> uploadPool,
            Property targetProperty
        )
        {
            string normPath = NormalizeLocalPath(localPath);
            bool isMesh = string.Equals(assetType, "Mesh", StringComparison.OrdinalIgnoreCase);

            bool isExcluded = false;
            if (!isMesh)
            {
                foreach (var excluded in Program.ExcludedTextures)
                {
                    if (normPath.IndexOf(excluded, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isExcluded = true;
                        break;
                    }
                }
            }

            bool uploadEnabled = isMesh ? Program.UploadMeshes : Program.UploadAssets;

            if (isExcluded || !uploadEnabled || !Program.HasRobloxUploadCredentials)
            {
                if (isExcluded)
                    Program.Emit("raw", $"Skipping cloud upload for excluded asset: {localPath}");

                SetPropertyValue(targetProperty, LocalAsset(normPath));
                return;
            }

            // Enqueue the upload as a task into the pool for parallel execution
            var task = Task.Run(async () =>
            {
                string assetId;

                try
                {
                    assetId = await ResolveAsync(localPath, assetType, contentType).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Program.Emit("raw", $"Roblox rejected {localPath} after retries; using the local asset instead. {e.Message}");
                    TrackFailure(localPath);
                    SetPropertyValue(targetProperty, LocalAsset(normPath));
                    return;
                }

                if (string.IsNullOrEmpty(assetId))
                {
                    // File was missing — fall back to local asset instead of leaving property empty
                    SetPropertyValue(targetProperty, LocalAsset(normPath));
                    return;
                }

                RequireUploadedAsset(assetId, localPath);
                SetPropertyValue(targetProperty, assetId);
                TrackSuccess();
            });

            uploadPool.Add(task);
        }

        private Task<string> ResolveAsync(string localPath, string assetType, string contentType)
        {
            string normalizedPath = NormalizeLocalPath(localPath);
            string filePath = Path.Combine(rootDir, normalizedPath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(filePath))
            {
                Program.Emit("raw", $"Skipped missing upload file: {filePath}");
                return Task.FromResult<string>(null);
            }

            return UploadAsync(filePath, assetType, contentType);
        }

        private async Task<string> UploadAsync(
            string filePath,
            string assetType,
            string contentType
        )
        {
            string cachedId = AssetUploadCache.GetCachedAssetId(filePath, assetType, Program.RobloxCreatorType, Program.RobloxCreatorId);
            if (!string.IsNullOrEmpty(cachedId))
            {
                Program.Emit("raw", $"Using cached asset ID for {Path.GetFileName(filePath)}: {cachedId}");
                return cachedId;
            }

            Interlocked.Increment(ref AttemptedUploads);

            string assetId = await CreateAssetWithRetryAsync(filePath, assetType, contentType).ConfigureAwait(false);
            string formattedAssetId = "rbxassetid://" + assetId;

            AssetUploadCache.SetCachedAssetId(filePath, assetType, Program.RobloxCreatorType, Program.RobloxCreatorId, formattedAssetId);
            return formattedAssetId;
        }

        /// <summary>
        /// Wraps CreateAssetAsync with exponential backoff retry for transient HTTP errors.
        /// Retries on: 429 (rate limit), 500, 502, 503, 504 (server errors), and network exceptions.
        /// Respects Retry-After headers when present.
        /// </summary>
        private async Task<string> CreateAssetWithRetryAsync(string filePath, string assetType, string contentType)
        {
            Exception lastException = null;

            for (int attempt = 0; attempt < MaxUploadRetries; attempt++)
            {
                try
                {
                    return await CreateAssetAsync(filePath, assetType, contentType).ConfigureAwait(false);
                }
                catch (TransientHttpException tex)
                {
                    lastException = tex;
                    int delayMs = ComputeRetryDelay(attempt, tex.RetryAfterSeconds);
                    Program.Emit("raw", $"Upload attempt {attempt + 1}/{MaxUploadRetries} failed for {Path.GetFileName(filePath)} " +
                                       $"(HTTP {tex.StatusCode}). Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs).ConfigureAwait(false);
                }
                catch (HttpRequestException hrex)
                {
                    // Network-level failure (DNS, connection reset, etc.)
                    lastException = hrex;
                    int delayMs = ComputeRetryDelay(attempt, null);
                    Program.Emit("raw", $"Upload attempt {attempt + 1}/{MaxUploadRetries} failed for {Path.GetFileName(filePath)} " +
                                       $"(Network error: {hrex.Message}). Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs).ConfigureAwait(false);
                }
                catch (TaskCanceledException tcex) when (!tcex.CancellationToken.IsCancellationRequested)
                {
                    // HTTP timeout
                    lastException = tcex;
                    int delayMs = ComputeRetryDelay(attempt, null);
                    Program.Emit("raw", $"Upload attempt {attempt + 1}/{MaxUploadRetries} timed out for {Path.GetFileName(filePath)}. " +
                                       $"Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs).ConfigureAwait(false);
                }
                // Non-transient errors (400 Bad Request, 401 Unauthorized, 403 Forbidden, etc.)
                // are NOT retried — they propagate immediately.
            }

            throw new Exception($"Upload failed after {MaxUploadRetries} attempts for {Path.GetFileName(filePath)}: {lastException?.Message}", lastException);
        }

        private async Task<string> CreateAssetAsync(string filePath, string assetType, string contentType)
        {
            await UploadSlots.WaitAsync().ConfigureAwait(false);

            try
            {
                await WaitForRateSlotAsync().ConfigureAwait(false);

                string name = Path.GetFileNameWithoutExtension(filePath);
                object creator = Program.RobloxCreatorType == "group"
                    ? new { groupId = Program.RobloxCreatorId } as object
                    : new { userId = Program.RobloxCreatorId };
                string requestJson = JsonConvert.SerializeObject(new
                {
                    assetType,
                    displayName = TrimName(name),
                    description = "Generated by Source2Roblox",
                    creationContext = new { creator }
                });

                using (var form = new MultipartFormDataContent())
                using (var requestPart = new StringContent(requestJson))
                using (var stream = File.OpenRead(filePath))
                using (var filePart = new StreamContent(stream))
                using (var request = new HttpRequestMessage(HttpMethod.Post, AssetsUrl))
                {
                    requestPart.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    filePart.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    form.Add(requestPart, "request");
                    form.Add(filePart, "fileContent", Path.GetFileName(filePath));
                    request.Headers.Add("x-api-key", Program.RobloxApiKey);
                    request.Content = form;

                    var response = await Client.SendAsync(request).ConfigureAwait(false);
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        int code = (int)response.StatusCode;

                        if (IsTransientStatusCode(response.StatusCode))
                        {
                            double? retryAfter = ParseRetryAfter(response);
                            throw new TransientHttpException(code, response.ReasonPhrase, body, retryAfter);
                        }

                        // Non-transient: throw a regular exception (will NOT be retried)
                        throw new Exception($"{code} {response.ReasonPhrase}: {body}");
                    }

                    var json = JObject.Parse(body);
                    string operationPath = json.Value<string>("path");

                    if (string.IsNullOrWhiteSpace(operationPath))
                        throw new Exception("Roblox did not return an operation path.");

                    Program.Emit("raw", $"Queued {Path.GetFileName(filePath)} with Roblox.");
                    return await PollOperationAsync(operationPath).ConfigureAwait(false);
                }
            }
            finally
            {
                UploadSlots.Release();
            }
        }

        private static Task WaitForRateSlotAsync()
        {
            int delayMs = 0;

            lock (RateLock)
            {
                var now = DateTime.UtcNow;

                if (nextUploadStart > now)
                    delayMs = (int)Math.Ceiling((nextUploadStart - now).TotalMilliseconds);

                nextUploadStart = (delayMs > 0 ? nextUploadStart : now).AddMilliseconds(UploadIntervalMs);
            }

            return delayMs > 0 ? Task.Delay(delayMs) : Task.CompletedTask;
        }

        /// <summary>
        /// Polls a Roblox operation for completion. Retries transient HTTP errors during polling
        /// instead of immediately aborting (the asset was already submitted and may complete).
        /// </summary>
        private static async Task<string> PollOperationAsync(string operationPath)
        {
            string operationId = operationPath.Contains("/")
                ? operationPath.Substring(operationPath.LastIndexOf("/", StringComparison.Ordinal) + 1)
                : operationPath;

            int consecutiveTransientErrors = 0;

            for (int attempt = 0; attempt < 90; attempt++)
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, $"{OperationsUrl}/{operationId}"))
                {
                    request.Headers.Add("x-api-key", Program.RobloxApiKey);

                    HttpResponseMessage response;
                    string body;

                    try
                    {
                        response = await Client.SendAsync(request).ConfigureAwait(false);
                        body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException tce && !tce.CancellationToken.IsCancellationRequested))
                    {
                        // Network error or timeout during poll — retry after delay
                        consecutiveTransientErrors++;
                        if (consecutiveTransientErrors > MaxPollRetries)
                            throw new Exception($"Polling failed after {MaxPollRetries} consecutive network errors: {ex.Message}", ex);

                        int backoff = ComputeRetryDelay(consecutiveTransientErrors - 1, null);
                        Program.Emit("raw", $"Poll network error for operation {operationId}, retrying in {backoff}ms...");
                        await Task.Delay(backoff).ConfigureAwait(false);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        if (IsTransientStatusCode(response.StatusCode))
                        {
                            consecutiveTransientErrors++;
                            if (consecutiveTransientErrors > MaxPollRetries)
                                throw new Exception($"Polling failed after {MaxPollRetries} consecutive transient errors. Last: {(int)response.StatusCode} {response.ReasonPhrase}: {body}");

                            double? retryAfter = ParseRetryAfter(response);
                            int backoff = ComputeRetryDelay(consecutiveTransientErrors - 1, retryAfter);
                            Program.Emit("raw", $"Poll HTTP {(int)response.StatusCode} for operation {operationId}, retrying in {backoff}ms...");
                            await Task.Delay(backoff).ConfigureAwait(false);
                            continue;
                        }

                        // Non-transient error during poll — not recoverable
                        throw new Exception($"{(int)response.StatusCode} {response.ReasonPhrase}: {body}");
                    }

                    // Successful response — reset transient error counter
                    consecutiveTransientErrors = 0;

                    var json = JObject.Parse(body);

                    if (json.Value<bool?>("done") != true)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        continue;
                    }

                    var error = json["error"];
                    if (error != null && error.Type != JTokenType.Null)
                        throw new Exception(error.Value<string>("message") ?? error.ToString(Formatting.None));

                    string assetId = json["response"]?.Value<string>("assetId");

                    if (string.IsNullOrWhiteSpace(assetId))
                        throw new Exception("Roblox completed the upload without an assetId.");

                    return assetId;
                }
            }

            throw new TimeoutException("Roblox asset upload did not finish within 90 seconds.");
        }

        #region Retry Helpers

        /// <summary>Returns true for HTTP status codes that are safe to retry.</summary>
        private static bool IsTransientStatusCode(HttpStatusCode statusCode)
        {
            int code = (int)statusCode;
            return code == 429 || code == 500 || code == 502 || code == 503 || code == 504;
        }

        /// <summary>
        /// Parses the Retry-After response header, returning seconds to wait.
        /// Returns null if the header is absent or unparseable.
        /// </summary>
        private static double? ParseRetryAfter(HttpResponseMessage response)
        {
            var retryAfterHeader = response.Headers.RetryAfter;

            if (retryAfterHeader == null)
                return null;

            if (retryAfterHeader.Delta.HasValue)
                return retryAfterHeader.Delta.Value.TotalSeconds;

            if (retryAfterHeader.Date.HasValue)
            {
                var wait = retryAfterHeader.Date.Value - DateTimeOffset.UtcNow;
                return wait.TotalSeconds > 0 ? wait.TotalSeconds : 0;
            }

            return null;
        }

        /// <summary>
        /// Computes retry delay with exponential backoff + jitter.
        /// If a Retry-After header value is provided and is larger than the computed backoff, it takes precedence.
        /// </summary>
        private static int ComputeRetryDelay(int attemptIndex, double? retryAfterSeconds)
        {
            // Exponential backoff: 1s, 2s, 4s, 8s, 16s + jitter ±25%
            int exponentialMs = BaseRetryDelayMs * (1 << Math.Min(attemptIndex, 4));
            int jitterMs;
            lock (Jitter)
            {
                jitterMs = Jitter.Next(-(exponentialMs / 4), exponentialMs / 4 + 1);
            }
            int computedMs = exponentialMs + jitterMs;

            if (retryAfterSeconds.HasValue)
            {
                int retryAfterMs = (int)Math.Ceiling(retryAfterSeconds.Value * 1000);
                return Math.Max(computedMs, retryAfterMs);
            }

            return computedMs;
        }

        /// <summary>Custom exception for transient HTTP errors that should be retried.</summary>
        private class TransientHttpException : Exception
        {
            public int StatusCode { get; }
            public double? RetryAfterSeconds { get; }

            public TransientHttpException(int statusCode, string reason, string body, double? retryAfterSeconds)
                : base($"{statusCode} {reason}: {body}")
            {
                StatusCode = statusCode;
                RetryAfterSeconds = retryAfterSeconds;
            }
        }

        #endregion

        #region Tracking

        private static void TrackFailure(string localPath)
        {
            lock (FailureLock)
            {
                FailedUploads.Add(localPath);
            }
        }

        private static void TrackSuccess()
        {
            Interlocked.Increment(ref SucceededUploads);
        }

        #endregion

        #region Property Helpers

        private static void SetProperty(Instance target, string property, string value)
        {
            var prop = ResolveProperty(target, property);

            if (prop == null)
                throw new Exception($"Unknown property {property} in {target.ClassName}");

            SetPropertyValue(prop, value);
        }

        private static Property ResolveProperty(Instance target, string property)
        {
            var prop = target.GetProperty(property);

            if (prop != null)
                return prop;

            string alias = null;

            if (target is SurfaceAppearance)
            {
                switch (property)
                {
                    case "ColorMap":
                        alias = "ColorMapContent";
                        break;
                    case "NormalMap":
                        alias = "NormalMapContent";
                        break;
                    case "RoughnessMap":
                        alias = "RoughnessMapContent";
                        break;
                    case "MetalnessMap":
                        alias = "MetalnessMapContent";
                        break;
                }
            }
            else if (target is MeshPart && (property == "MeshId" || property == "MeshID"))
            {
                alias = "MeshContent";
            }

            return alias == null ? null : target.GetProperty(alias);
        }

        private static void SetPropertyValue(Property prop, string value)
        {
            object current = prop.Value;

            if (current is Content)
            {
                prop.Value = new Content(value);
                return;
            }

            if (current is ContentId)
            {
                prop.Value = new ContentId(value);
                return;
            }

            prop.Value = value;
        }

        #endregion

        private static void RequireUploadedAsset(string value, string localPath)
        {
            if (!value.StartsWith("rbxassetid://", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Upload did not produce a Roblox asset id for {localPath}.");
        }

        private string LocalAsset(string localPath)
        {
            return $"{localAssetRoot}/{localPath.Replace('\\', '/')}";
        }

        private static string NormalizeLocalPath(string localPath)
        {
            if (localPath.EndsWith(".vtf", StringComparison.OrdinalIgnoreCase))
                localPath = Path.ChangeExtension(localPath, ".png");

            return localPath.Replace('\\', '/');
        }

        private static string TrimName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "source2roblox_asset";

            return name.Length > 50 ? name.Substring(0, 50) : name;
        }

        public static async Task<(bool Success, string Message)> VerifyCredentialsAsync(string apiKey, string creatorType, string creatorId)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return (false, "Roblox API key cannot be empty.");

            if (string.IsNullOrWhiteSpace(creatorId))
                return (false, "Roblox Creator ID cannot be empty.");

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, $"{OperationsUrl}/test-probe-check"))
                {
                    request.Headers.Add("x-api-key", apiKey);
                    var response = await Client.SendAsync(request).ConfigureAwait(false);
                    int code = (int)response.StatusCode;

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                        return (false, "HTTP 401: Unauthorized. The provided Roblox API key is invalid.");

                    if (response.StatusCode == HttpStatusCode.Forbidden)
                        return (false, "HTTP 403: Forbidden. API key lacks permissions or has IP restrictions.");

                    if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest || response.IsSuccessStatusCode)
                    {
                        return (true, $"Roblox Open Cloud credentials verified successfully ({creatorType} ID: {creatorId}).");
                    }

                    return (false, $"Roblox returned HTTP {code}: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Network error testing Roblox API: {ex.Message}");
            }
        }
    }
}
