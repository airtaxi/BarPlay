# BarPlay

🌐 [한국어](README.ko.md)

BarPlay is a lightweight Windows taskbar widget that brings media playback information and controls directly to your Windows 11 taskbar.
It seamlessly integrates as a deskband, allowing you to view the currently playing media and control playback without switching windows.

[![Download from Microsoft Store](https://get.microsoft.com/images/en-US%20dark.svg)](https://apps.microsoft.com/detail/9N841L4XR28R)

![BarPlay Showcase](.github/Showcase.jpg)

## Features

- Seamlessly integrates into the Windows 11 taskbar as a deskband widget
- Displays current playing media thumbnail, title, and description at a glance
- Click the artwork to jump directly to the currently playing app
- Provides quick access to Previous, Play/Pause, and Next transport controls
- Interactive seek slider inside an expanded flyout menu
- Works universally with any app supporting Windows System Media Transport Controls (Spotify, Web Browsers, YouTube, etc.)
- Run at system startup (auto-start) option
- Compiled with NativeAOT for lightning-fast startup and minimal resource consumption
- Multilingual UI (English, Korean, Japanese, Chinese Simplified, Chinese Traditional)

## Libraries Used

- [Microsoft.WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK) - WinUI 3 app platform and Windows integration
- [Deskband11Lib](https://github.com/StartIsBack/Deskband11Lib) - Windows 11 taskbar widget integration
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM architecture and source generators
- [Microsoft.Extensions.DependencyInjection](https://github.com/dotnet/runtime) - Dependency injection container

## License

This project is distributed under the [MIT License](LICENSE.txt).

## Author

**Howon Lee** ([airtaxi](https://github.com/airtaxi))
