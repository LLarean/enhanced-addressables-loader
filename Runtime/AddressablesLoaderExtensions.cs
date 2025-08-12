using System;
using UnityEngine;

namespace EnhancedAddressables
{
    /// <summary>
    /// Extension methods for AddressablesLoader providing backward compatibility
    /// </summary>
    public static class AddressablesLoaderExtensions
    {
        /// <summary>
        /// Loads all Addressables with callback-based API for backward compatibility
        /// </summary>
        /// <param name="loader">The AddressablesLoader instance</param>
        /// <param name="loadCallback">Callback invoked when loading completes</param>
        [Obsolete("Use LoadAllAsync instead for better async/await support")]
        public static async void LoadAll(this AddressablesLoader loader, Action<bool> loadCallback)
        {
            var result = await loader.LoadAllAsync();
            loadCallback?.Invoke(result);
        }

        /// <summary>
        /// Converts progress from 0-1 range to 0-100 range
        /// </summary>
        /// <param name="progress">Progress value from 0.0 to 1.0</param>
        /// <returns>Progress percentage from 0 to 100</returns>
        public static int ToPercentage(this float progress)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(progress) * 100);
        }

        /// <summary>
        /// Converts progress from 0-100 range to 0-1 range
        /// </summary>
        /// <param name="percentage">Progress percentage from 0 to 100</param>
        /// <returns>Progress value from 0.0 to 1.0</returns>
        public static float ToProgress(this int percentage)
        {
            return Mathf.Clamp01(percentage / 100f);
        }
    }
}