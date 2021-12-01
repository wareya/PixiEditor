using PixiEditor.Models.DataHolders;
using PixiEditor.Models.Layers;
using PixiEditor.Models.Position;
using PixiEditor.Models.Tools.ToolSettings.Toolbars;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace PixiEditor.Models.Tools
{
    public abstract class ShapeTool : BitmapOperationTool
    {
        public static DoubleCords CalculateCoordinatesForShapeRotation(
            Coordinates startingCords,
            Coordinates secondCoordinates)
        {
            Coordinates currentCoordinates = secondCoordinates;

            if (startingCords.X > currentCoordinates.X && startingCords.Y > currentCoordinates.Y)
            {
                return new DoubleCords(
                    new Coordinates(currentCoordinates.X, currentCoordinates.Y),
                    new Coordinates(startingCords.X, startingCords.Y));
            }

            if (startingCords.X < currentCoordinates.X && startingCords.Y < currentCoordinates.Y)
            {
                return new DoubleCords(
                    new Coordinates(startingCords.X, startingCords.Y),
                    new Coordinates(currentCoordinates.X, currentCoordinates.Y));
            }

            if (startingCords.Y > currentCoordinates.Y)
            {
                return new DoubleCords(
                    new Coordinates(startingCords.X, currentCoordinates.Y),
                    new Coordinates(currentCoordinates.X, startingCords.Y));
            }

            if (startingCords.X > currentCoordinates.X && startingCords.Y <= currentCoordinates.Y)
            {
                return new DoubleCords(
                    new Coordinates(currentCoordinates.X, startingCords.Y),
                    new Coordinates(startingCords.X, currentCoordinates.Y));
            }

            return new DoubleCords(startingCords, secondCoordinates);
        }

        public ShapeTool()
        {
            RequiresPreviewLayer = true;
            Cursor = Cursors.Cross;
            Toolbar = new BasicShapeToolbar();
        }

        // TODO: Add cache for lines 31, 32 (hopefully it would speed up calculation)
        public abstract override void Use(Layer layer, List<Coordinates> coordinates, SKColor color);

        public static void ThickenShape(Layer layer, SKColor color, IEnumerable<Coordinates> shape, int thickness)
        {
            foreach (Coordinates item in shape)
            {
                ThickenShape(layer, color, item, thickness);
            }
        }

        protected static void ThickenShape(Layer layer, SKColor color, Coordinates coords, int thickness)
        {
            var dcords = CoordinatesCalculator.CalculateThicknessCenter(coords, thickness);
            CoordinatesCalculator.DrawRectangle(layer, color, dcords.Coords1.X, dcords.Coords1.Y, dcords.Coords2.X, dcords.Coords2.Y);
        }
    }
}
