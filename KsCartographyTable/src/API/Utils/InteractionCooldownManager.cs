
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace Kaisentlaia.KsCartographyTableMod.API.Utils
{
  public class InteractionCooldownManager
  {
    private Dictionary<string, long> lastInteractionTimes = new();
    private const long InteractionCooldownMs = 500;
    private const long EntryExpirationMs = 60000;
    private ICoreAPI api;

    public InteractionCooldownManager(ICoreAPI api) => this.api = api;

    public bool CanInteract(IPlayer player)
    {
      string playerKey = player.PlayerUID;
      long currentTime = api.World.ElapsedMilliseconds;

      if (lastInteractionTimes.TryGetValue(playerKey, out long lastTime))
      {
        if (currentTime - lastTime < InteractionCooldownMs)
          return false;
      }

      CleanupExpiredEntries(currentTime);
      lastInteractionTimes[playerKey] = currentTime;
      return true;
    }

    private void CleanupExpiredEntries(long currentTime)
    {
      var keysToRemove = lastInteractionTimes
          .Where(kvp => currentTime - kvp.Value > EntryExpirationMs)
          .Select(kvp => kvp.Key)
          .ToList();

      foreach (var key in keysToRemove)
        lastInteractionTimes.Remove(key);
    }
  }
}