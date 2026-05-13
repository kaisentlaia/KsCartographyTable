using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using System.Collections.Generic;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public static class MapColorOverlay
    {
        // Overlay color #3e3226 (62, 50, 38) with 0.5 alpha
        private static readonly int OverlayR = 62;
        private static readonly int OverlayG = 50;
        private static readonly int OverlayB = 38;
        private static readonly float OverlayAlpha = 0.5f;

        /// <summary>
        /// Applies a 50% transparency overlay of #3e3226 to all pixels in a map piece
        /// </summary>
        public static MapPieceDB ApplyColorOverlay(MapPieceDB mapPiece)
        {
            for (int i = 0; i < mapPiece.Pixels.Length; i++)
            {
                mapPiece.Pixels[i] = BlendPixel(mapPiece.Pixels[i]);
            }
            return mapPiece;
        }

        /// <summary>
        /// Blends a single pixel with the overlay color using alpha compositing
        /// </summary>
        private static int BlendPixel(int originalPixel)
        {
            // Extract BGRA components
            byte origB = (byte)((originalPixel >> 16) & 0xFF);
            byte origG = (byte)((originalPixel >> 8) & 0xFF);
            byte origR = (byte)(originalPixel & 0xFF);

            // Apply overlay with 50% alpha
            byte newR = (byte)(origR * (1 - OverlayAlpha) + OverlayR * OverlayAlpha);
            byte newG = (byte)(origG * (1 - OverlayAlpha) + OverlayG * OverlayAlpha);
            byte newB = (byte)(origB * (1 - OverlayAlpha) + OverlayB * OverlayAlpha);
            byte newA = 255;

            // Reconstruct in BGRA format
            return (newA << 24) | (newB << 16) | (newG << 8) | newR;
        }

        /// <summary>
        /// Helper to process multiple map pieces at once
        /// </summary>
        public static Dictionary<FastVec2i, MapPieceDB> ApplyOverlayToPieces(Dictionary<FastVec2i, MapPieceDB> pieces)
        {
            foreach (var piece in pieces)
            {
                ApplyColorOverlay(piece.Value);
            }

            return pieces;
        }
    }
}