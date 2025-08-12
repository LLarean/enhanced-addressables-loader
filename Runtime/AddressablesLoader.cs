using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

namespace EnhancedAddressables
{
    /// <summary>
    /// Advanced Addressables content loader with progress tracking, cancellation support, and improved error handling
    /// </summary>
    public class AddressablesLoader : IDisposable
    {
        private readonly List<AsyncOperationHandle> _activeOperations = new List<AsyncOperationHandle>();
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isLoading;

        /// <summary>
        /// Called when the download percentage changes
        /// </summary>
        /// <remarks>Progress goes from 0.0 to 1.0</remarks>
        public event Action<float> OnProgressChanged;

        /// <summary>
        /// Called when a specific key starts downloading
        /// </summary>
        public event Action<object> OnKeyStarted;

        /// <summary>
        /// Called when a specific key completes downloading
        /// </summary>
        public event Action<object, bool> OnKeyCompleted;

        /// <summary>
        /// Called when download size calculation begins
        /// </summary>
        public event Action OnCalculatingDownloadSize;

        /// <summary>
        /// Called when download size calculation completes
        /// </summary>
        public event Action<long> OnDownloadSizeCalculated;

        /// <summary>
        /// Gets whether the loader is currently downloading content
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// Downloads all resources marked as Addressable
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if all downloads completed successfully</returns>
#if UNITASK_SUPPORT
        public async UniTask<bool> LoadAllAsync(CancellationToken cancellationToken = default)
#else
        public async Task<bool> LoadAllAsync(CancellationToken cancellationToken = default)
#endif
        {
            if (_isLoading)
            {
                Debug.LogWarning("[AddressablesLoader] Already loading. Ignoring duplicate request.");
                return false;
            }

            _isLoading = true;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                var resourceLocator = await InitializeAddressablesAsync(_cancellationTokenSource.Token);
                if (resourceLocator == null)
                {
                    Debug.LogError("[AddressablesLoader] Failed to initialize Addressables");
                    return false;
                }

                OnCalculatingDownloadSize?.Invoke();
                var keysToDownload = await GetKeysRequiringDownloadAsync(resourceLocator.Keys.ToList(), _cancellationTokenSource.Token);
                
                var totalSize = keysToDownload.Sum(k => k.SizeBytes);
                OnDownloadSizeCalculated?.Invoke(totalSize);

                if (keysToDownload.Count == 0)
                {
                    Debug.Log("[AddressablesLoader] No Addressable content requires downloading");
                    OnProgressChanged?.Invoke(1.0f);
                    return true;
                }

                Debug.Log($"[AddressablesLoader] Starting download of {keysToDownload.Count} keys. Total size: {FormatBytes(totalSize)}");
                return await DownloadKeysAsync(keysToDownload, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[AddressablesLoader] Download was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressablesLoader] Download failed: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                _isLoading = false;
                ReleaseActiveOperations();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Cancels the current download operation
        /// </summary>
        public void CancelDownload()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                Debug.Log("[AddressablesLoader] Download cancellation requested");
            }
        }

        /// <summary>
        /// Gets the total download size for all Addressable content
        /// </summary>
        /// <returns>Download size in bytes</returns>
#if UNITASK_SUPPORT
        public async UniTask<long> GetTotalDownloadSizeAsync(CancellationToken cancellationToken = default)
#else
        public async Task<long> GetTotalDownloadSizeAsync(CancellationToken cancellationToken = default)
#endif
        {
            try
            {
                var resourceLocator = await InitializeAddressablesAsync(cancellationToken);
                var keys = resourceLocator?.Keys.ToList();
                
                if (keys == null || keys.Count == 0)
                    return 0;

                var downloadSize = await Addressables.GetDownloadSizeAsync(keys).Task;
                return downloadSize;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressablesLoader] Failed to get download size: {ex.Message}");
                return 0;
            }
        }

#if UNITASK_SUPPORT
        private async UniTask<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator> InitializeAddressablesAsync(CancellationToken cancellationToken)
#else
        private async Task<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator> InitializeAddressablesAsync(CancellationToken cancellationToken)
#endif
        {
            var initOperation = Addressables.InitializeAsync();
            _activeOperations.Add(initOperation);

            try
            {
#if UNITASK_SUPPORT
                await initOperation.WithCancellation(cancellationToken);
                return initOperation.Result;
#else
                while (!initOperation.IsDone && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Yield();
                }
                
                cancellationToken.ThrowIfCancellationRequested();
                return initOperation.Result;
#endif
            }
            finally
            {
                _activeOperations.Remove(initOperation);
            }
        }

#if UNITASK_SUPPORT
        private async UniTask<List<DownloadKeyInfo>> GetKeysRequiringDownloadAsync(List<object> allKeys, CancellationToken cancellationToken)
#else
        private async Task<List<DownloadKeyInfo>> GetKeysRequiringDownloadAsync(List<object> allKeys, CancellationToken cancellationToken)
#endif
        {
            var keysToDownload = new List<DownloadKeyInfo>();
            
            foreach (var key in allKeys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var downloadSize = await Addressables.GetDownloadSizeAsync(key).Task;
                if (downloadSize > 0)
                {
                    keysToDownload.Add(new DownloadKeyInfo
                    {
                        Key = key,
                        SizeBytes = downloadSize
                    });
                }
            }

            Debug.Log($"[AddressablesLoader] Found {keysToDownload.Count} keys requiring download. Total size: {FormatBytes(keysToDownload.Sum(k => k.SizeBytes))}");
            return keysToDownload;
        }

#if UNITASK_SUPPORT
        private async UniTask<bool> DownloadKeysAsync(List<DownloadKeyInfo> keysToDownload, CancellationToken cancellationToken)
#else
        private async Task<bool> DownloadKeysAsync(List<DownloadKeyInfo> keysToDownload, CancellationToken cancellationToken)
#endif
        {
            var totalSizeBytes = keysToDownload.Sum(k => k.SizeBytes);
            long downloadedBytes = 0;
            bool allSucceeded = true;

            OnProgressChanged?.Invoke(0.0f);

            for (int i = 0; i < keysToDownload.Count; i++)
            {
                var keyInfo = keysToDownload[i];
                cancellationToken.ThrowIfCancellationRequested();

                OnKeyStarted?.Invoke(keyInfo.Key);
                
                Debug.Log($"[AddressablesLoader] Downloading key {i + 1}/{keysToDownload.Count}: {keyInfo.Key} ({FormatBytes(keyInfo.SizeBytes)})");
                
                var success = await DownloadSingleKeyAsync(keyInfo, downloadedBytes, totalSizeBytes, cancellationToken);
                
                OnKeyCompleted?.Invoke(keyInfo.Key, success);
                
                if (!success)
                {
                    allSucceeded = false;
                    Debug.LogError($"[AddressablesLoader] Failed to download key: {keyInfo.Key}");
                    // Continue with other downloads instead of failing completely
                }
                else
                {
                    downloadedBytes += keyInfo.SizeBytes;
                }
            }

            OnProgressChanged?.Invoke(1.0f);
            Debug.Log($"[AddressablesLoader] Download completed. Success: {allSucceeded}");
            return allSucceeded;
        }

#if UNITASK_SUPPORT
        private async UniTask<bool> DownloadSingleKeyAsync(DownloadKeyInfo keyInfo, long previousDownloadedBytes, long totalSizeBytes, CancellationToken cancellationToken)
#else
        private async Task<bool> DownloadSingleKeyAsync(DownloadKeyInfo keyInfo, long previousDownloadedBytes, long totalSizeBytes, CancellationToken cancellationToken)
#endif
        {
            var downloadOperation = Addressables.DownloadDependenciesAsync(keyInfo.Key, false);
            _activeOperations.Add(downloadOperation);

            try
            {
                while (!downloadOperation.IsDone && !cancellationToken.IsCancellationRequested)
                {
                    var currentKeyProgress = downloadOperation.PercentComplete;
                    var currentKeyBytes = (long)(keyInfo.SizeBytes * currentKeyProgress);
                    var totalProgress = totalSizeBytes > 0 ? (float)(previousDownloadedBytes + currentKeyBytes) / totalSizeBytes : 1.0f;
                    
                    OnProgressChanged?.Invoke(Mathf.Clamp01(totalProgress));
                    
#if UNITASK_SUPPORT
                    await UniTask.Yield();
#else
                    await Task.Yield();
#endif
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (downloadOperation.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"[AddressablesLoader] Successfully downloaded: {keyInfo.Key} ({FormatBytes(keyInfo.SizeBytes)})");
                    return true;
                }
                else
                {
                    Debug.LogError($"[AddressablesLoader] Download failed for key: {keyInfo.Key}. Status: {downloadOperation.Status}");
                    if (downloadOperation.OperationException != null)
                    {
                        Debug.LogError($"[AddressablesLoader] Exception: {downloadOperation.OperationException.Message}");
                    }
                    return false;
                }
            }
            finally
            {
                _activeOperations.Remove(downloadOperation);
                downloadOperation.Release();
            }
        }

        private void ReleaseActiveOperations()
        {
            foreach (var operation in _activeOperations)
            {
                if (operation.IsValid())
                {
                    operation.Release();
                }
            }
            _activeOperations.Clear();
        }

        private static string FormatBytes(long bytes)
        {
            const long kb = 1024;
            const long mb = kb * 1024;
            const long gb = mb * 1024;

            if (bytes >= gb)
                return $"{bytes / (double)gb:F2} GB";
            if (bytes >= mb)
                return $"{bytes / (double)mb:F2} MB";
            if (bytes >= kb)
                return $"{bytes / (double)kb:F2} KB";
            
            return $"{bytes} bytes";
        }

        public void Dispose()
        {
            CancelDownload();
            ReleaseActiveOperations();
            _cancellationTokenSource?.Dispose();
        }

        private struct DownloadKeyInfo
        {
            public object Key;
            public long SizeBytes;
        }
    }
}