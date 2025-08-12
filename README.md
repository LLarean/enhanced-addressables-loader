# Enhanced Addressables Loader

[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/llarean/enhanced-addressables-loader/blob/main/LICENSE.md)
![stability-experimental](https://img.shields.io/badge/stability-experimental-orange.svg)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![CodeFactor](https://www.codefactor.io/repository/github/llarean/enhanced-addressables-loader/badge)](https://www.codefactor.io/repository/github/llarean/enhanced-addressables-loader)
![development](https://img.shields.io/badge/Development-Active-brightgreen.svg)

⚠️ **This project is currently in active development and has not been thoroughly tested. Use at your own risk in production environments.**

**Enhanced Addressables Loader** is an advanced Unity package for downloading Addressables content with enhanced features like progress tracking, cancellation support, and robust error handling.

Built to overcome limitations of Unity's default Addressables downloading workflow, this system provides a modern async/await API with comprehensive progress reporting and resource management.

## Features

- **Modern Async/Await API**: Full support for C# async patterns with UniTask and Task
- **Cancellation Support**: Cancel downloads at any time with CancellationToken
- **Detailed Progress Tracking**: Real-time progress reporting from 0.0 to 1.0
- **Robust Error Handling**: Graceful error recovery and detailed logging
- **Resource Management**: Automatic cleanup and proper disposal patterns
- **Performance Optimized**: Efficient memory usage and CPU optimization
- **Flexible Events System**: Multiple events for granular download tracking
- **Cross-Platform**: Works with Unity 2021.3+ on all platforms

## Requirements

- Unity 2021.3 or later
- Addressables Package (1.19.0+)
- UniTask Package (2.3.3+) - *Optional but recommended*

## Development Status

**This package is currently under active development:**

- ❌ **Not production-ready** - Use at your own risk
- ❌ **Limited testing** - Unit tests and integration tests are being developed
- ❌ **API may change** - Breaking changes may occur in future versions
- ✅ **Core functionality implemented** - Basic downloading features work
- ✅ **Documentation available** - Basic usage examples provided

## Installation

**Installation instructions are subject to change as the project is still in development**

### Unity Package Manager (Git URL)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click the `+` button and select `Add package from git URL`
3. Enter: `https://github.com/llarean/enhanced-addressables-loader.git`

### Via Packages/manifest.json

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.llarean.enhanced-addressables-loader": "https://github.com/llarean/enhanced-addressables-loader.git"
  }
}
```

### Scoped Registry (Future)

*Will be available once the package is stable and published*

## Quick Start

**Warning: This API is experimental and may change**

### Basic Usage

```csharp
using EnhancedAddressables;
using UnityEngine;
using System.Threading;

public class GameInitializer : MonoBehaviour
{
    private AddressablesLoader loader;
    private CancellationTokenSource cancellationTokenSource;

    async void Start()
    {
        loader = new AddressablesLoader();
        
        // Subscribe to progress updates
        loader.OnProgressChanged += OnDownloadProgress;
        loader.OnKeyStarted += OnKeyStarted;
        loader.OnKeyCompleted += OnKeyCompleted;
        
        // Create cancellation token
        cancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            // Start downloading with cancellation support
            bool success = await loader.LoadAllAsync(cancellationTokenSource.Token);
            
            if (success)
            {
                Debug.Log("All content downloaded successfully!");
                // Initialize game systems...
            }
            else
            {
                Debug.LogError("Download failed or was cancelled");
                // Handle failure...
            }
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("Download was cancelled by user");
        }
    }

    private void OnDownloadProgress(float progress)
    {
        Debug.Log($"Download progress: {progress:P2}");
        // Update UI progress bar...
    }

    private void OnKeyStarted(object key)
    {
        Debug.Log($"Started downloading: {key}");
    }

    private void OnKeyCompleted(object key, bool success)
    {
        Debug.Log($"Completed: {key} - Success: {success}");
    }

    void OnDestroy()
    {
        // Always dispose resources
        cancellationTokenSource?.Dispose();
        loader?.Dispose();
    }
}
```

### Advanced Usage

```csharp
public class AdvancedDownloadManager : MonoBehaviour
{
    private AddressablesLoader loader;
    
    async void Start()
    {
        loader = new AddressablesLoader();
        
        // Get total download size first
        long totalSize = await loader.GetTotalDownloadSizeAsync();
        Debug.Log($"Total download size: {FormatBytes(totalSize)}");
        
        if (totalSize > 0)
        {
            // Ask user for permission if download is large
            bool userConsent = await RequestUserConsent(totalSize);
            if (!userConsent) return;
            
            // Start download with timeout
            using var timeoutSource = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            bool success = await loader.LoadAllAsync(timeoutSource.Token);
        }
    }
    
    public void CancelDownload()
    {
        loader?.CancelDownload();
    }
    
    private string FormatBytes(long bytes)
    {
        const long MB = 1024 * 1024;
        return $"{bytes / (double)MB:F2} MB";
    }
}
```

## API Reference

### AddressablesLoader Class

#### Methods
- `LoadAllAsync(CancellationToken)` - Downloads all Addressable content
- `GetTotalDownloadSizeAsync(CancellationToken)` - Gets total download size
- `CancelDownload()` - Cancels current download
- `Dispose()` - Cleans up resources

#### Events
- `OnProgressChanged(float)` - Progress from 0.0 to 1.0
- `OnKeyStarted(object)` - Individual key download started
- `OnKeyCompleted(object, bool)` - Individual key download completed
- `OnCalculatingDownloadSize()` - Download size calculation started
- `OnDownloadSizeCalculated(long)` - Download size calculation completed

#### Properties
- `IsLoading` - Whether currently downloading

## Testing Status

**Current Testing Status: INCOMPLETE**

- [ ] Unit Tests - *In development*

## Troubleshooting

### Common Issues

**Download fails immediately?**
- Check internet connection
- Verify Addressables are properly configured
- Ensure Addressable content exists on server

**Memory leaks?**
- Always call `loader.Dispose()` when done
- Use `using` statements with CancellationTokenSource

**Performance issues?**
- Monitor download size before starting
- Consider downloading in batches for very large content

### Known Limitations

- Error handling may not cover all edge cases
- Performance not optimized for very large downloads (1000+ assets)
- Limited testing on mobile platforms
- UniTask integration needs more testing

## Contributing

**We welcome contributions, especially during this development phase!**

### How to Help

1. **Testing**: Try the package in different scenarios and report issues
2. **Bug Reports**: Use GitHub issues with detailed reproduction steps
3. **Feature Requests**: Suggest improvements or new features
4. **Code Review**: Review pull requests and provide feedback
5. **Documentation**: Help improve documentation and examples

### Development Setup

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Add tests if applicable
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

## Roadmap

### Short Term (v0.1.x)
- [ ] Complete unit test coverage
- [ ] Performance optimization
- [ ] Better error messages
- [ ] Mobile platform testing

### Medium Term (v0.2.x)
- [ ] Batch downloading support
- [ ] Download prioritization
- [ ] Offline mode detection
- [ ] Comprehensive documentation

### Long Term (v1.0.x)
- [ ] Production stability
- [ ] Performance benchmarks
- [ ] Integration examples
- [ ] Video tutorials

## License

This project is licensed under the MIT License - see the [LICENSE.md](https://github.com/LLarean/enhanced-addressables-loader?tab=MIT-1-ov-file) file for details.

## Acknowledgments

- Inspired by Unity's Addressables system limitations
- Built with modern C# async patterns
- Thanks to the UniTask community for async utilities

## Support

**Since this is an experimental project, support is limited:**

- **Bug Reports**: [GitHub Issues](https://github.com/llarean/enhanced-addressables-loader/issues)
- **Discussions**: [GitHub Discussions](https://github.com/llarean/enhanced-addressables-loader/discussions)
- **Email**: llarean@ya.ru *(for critical issues only)*

## Disclaimer

**USE AT YOUR OWN RISK**

This package is provided "as is" without warranty of any kind. The authors are not responsible for any damage or data loss that may occur from using this software. Always test thoroughly in a development environment before using in production.

---

<div align="center">

**🚧 Project Under Development 🚧**

This package is actively being developed. Star ⭐ the repository to follow progress!

**Help us make it better by contributing and reporting issues!**

</div>
