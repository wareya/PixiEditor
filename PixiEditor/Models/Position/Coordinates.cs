﻿using SkiaSharp;
using System;
using System.Diagnostics;

namespace PixiEditor.Models.Position
{
    [DebuggerDisplay("({DebuggerDisplay,nq})")]
    public struct Coordinates
    {
        private string DebuggerDisplay => ToString();

        public Coordinates(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public static implicit operator Coordinates((int width, int height) tuple)
        {
            return new Coordinates(tuple.width, tuple.height);
        }

        public static Coordinates operator -(Coordinates coordiantes, int size)
        {
            return new Coordinates(coordiantes.X - size, coordiantes.Y - size);
        }

        public static bool operator ==(Coordinates c1, Coordinates c2)
        {
            return c2.X == c1.X && c2.Y == c1.Y;
        }

        public static bool operator !=(Coordinates c1, Coordinates c2)
        {
            return !(c1 == c2);
        }

        public override string ToString()
        {
            return $"{X}, {Y}";
        }

        public string ToString(IFormatProvider provider)
        {
            return $"{X.ToString(provider)}, {Y.ToString(provider)}";
        }

        public static explicit operator SKPoint(Coordinates coordinates)
        {
            return new SKPoint(coordinates.X, coordinates.Y);
        }

        public static explicit operator SKPointI(Coordinates coordinates)
        {
            return new SKPointI(coordinates.X, coordinates.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is Coordinates coords && this == coords;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }
}