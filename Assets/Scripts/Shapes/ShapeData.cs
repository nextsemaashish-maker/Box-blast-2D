using System.Collections.Generic;
using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// Represents a shape configuration formed by a set of block coordinates and a color.
    /// </summary>
    [System.Serializable]
    public class ShapeData
    {
        public string ShapeName;
        public Vector2Int[] Blocks;
        public Color BlockColor;
        public int Width;
        public int Height;

        public ShapeData(string name, Vector2Int[] blocks, Color color)
        {
            ShapeName = name;
            Blocks = blocks;
            BlockColor = color;

            int maxX = 0;
            int maxY = 0;
            for (int i = 0; i < blocks.Length; i++)
            {
                if (blocks[i].x > maxX) maxX = blocks[i].x;
                if (blocks[i].y > maxY) maxY = blocks[i].y;
            }
            Width = maxX + 1;
            Height = maxY + 1;
        }

        public Vector2 GetCenterOffset()
        {
            return new Vector2((Width - 1) * 0.5f, (Height - 1) * 0.5f);
        }
    }

    /// <summary>
    /// Catalog of classic Block Blast polyomino shapes.
    /// </summary>
    public static class ShapeCatalog
    {
        public static List<ShapeData> GetAllShapes()
        {
            List<ShapeData> list = new List<ShapeData>();

            Color red = SpriteFactory.BlockColors[0];
            Color orange = SpriteFactory.BlockColors[1];
            Color yellow = SpriteFactory.BlockColors[2];
            Color green = SpriteFactory.BlockColors[3];
            Color cyan = SpriteFactory.BlockColors[4];
            Color blue = SpriteFactory.BlockColors[5];
            Color purple = SpriteFactory.BlockColors[6];
            Color pink = SpriteFactory.BlockColors[7];

            // 1. Single Dot (1x1)
            list.Add(new ShapeData("Dot", new[] { new Vector2Int(0, 0) }, yellow));

            // 2. Dominoes (1x2, 2x1)
            list.Add(new ShapeData("I2_H", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }, cyan));
            list.Add(new ShapeData("I2_V", new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) }, cyan));

            // 3. Trominoes (1x3, 3x1)
            list.Add(new ShapeData("I3_H", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) }, blue));
            list.Add(new ShapeData("I3_V", new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }, blue));

            // Small Corners (2x2, 3 blocks)
            list.Add(new ShapeData("Corner_BL", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) }, green));
            list.Add(new ShapeData("Corner_BR", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) }, green));
            list.Add(new ShapeData("Corner_TL", new[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 1) }, green));
            list.Add(new ShapeData("Corner_TR", new[] { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1) }, green));

            // 4. Tetrominoes (1x4, 4x1)
            list.Add(new ShapeData("I4_H", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) }, red));
            list.Add(new ShapeData("I4_V", new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3) }, red));

            // 2x2 Square
            list.Add(new ShapeData("Square_2x2", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }, orange));

            // Tetromino L (3x2)
            list.Add(new ShapeData("L_3x2_1", new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 0) }, purple));
            list.Add(new ShapeData("L_3x2_2", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2) }, purple));
            list.Add(new ShapeData("L_2x3_1", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1) }, purple));
            list.Add(new ShapeData("L_2x3_2", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, 1) }, purple));

            // Tetromino T
            list.Add(new ShapeData("T_Up", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1) }, pink));
            list.Add(new ShapeData("T_Down", new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) }, pink));

            // Tetromino Z & S
            list.Add(new ShapeData("Z", new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(2, 0) }, yellow));
            list.Add(new ShapeData("S", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1) }, green));

            // 5. Pentaminoes & Special Shapes
            list.Add(new ShapeData("I5_H", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0) }, red));
            list.Add(new ShapeData("I5_V", new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4) }, red));

            // Big Corner 3x3 (5 blocks)
            list.Add(new ShapeData("BigCorner_BL", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }, cyan));
            list.Add(new ShapeData("BigCorner_BR", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2) }, cyan));
            list.Add(new ShapeData("BigCorner_TL", new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2) }, cyan));
            list.Add(new ShapeData("BigCorner_TR", new[] { new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(1, 2), new Vector2Int(0, 2) }, cyan));

            // 3x3 Big Block
            list.Add(new ShapeData("Square_3x3", new[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
                new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)
            }, orange));

            return list;
        }

        public static ShapeData GetRandomShape()
        {
            var shapes = GetAllShapes();
            return shapes[Random.Range(0, shapes.Count)];
        }
    }
}
