using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace BarPlay.Models;

public sealed record MediaPlaybackSnapshot(string Title, string Description, bool HasSession, bool IsPlaying, bool CanSkipPrevious, bool CanSkipNext, bool CanPlayPause, ImageSource? Thumbnail, long PositionTicks, long EndTimeTicks, bool HasTimeline);
