using System;

namespace Basket.Core
{
    // Human-readable play-by-play (shots, blocks, steals) for debug UI and logs.
    public interface IMatchEventFeed
    {
        event Action<string> OnMatchEvent;
    }
}
