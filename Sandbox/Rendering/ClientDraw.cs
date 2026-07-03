using NyxDrawer;
using NyxRender;
using NyxAssets.Client;

namespace Sandbox;

/// <summary>Sandbox-only map helpers; Nyx thing drawing uses <see cref="AssetDrawer"/>.</summary>
internal static class ClientDraw
{
    public static void DrawMapFloor(
      ClientAssetBundle assets,
      AssetDrawer drawer,
      NyxGameMap.GameMap map,
      float camXf,
      float camYf,
      int winW,
      int winH,
      Position playerPos,
      Action<Position, float, float, int> drawCreaturesAtTile) =>
      MapFloorDrawer.Draw(assets, drawer, map, camXf, camYf, winW, winH, playerPos, drawCreaturesAtTile);
}
