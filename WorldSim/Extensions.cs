// <copyright file="Extensions.cs" company="HARK">
// Copyright (c) K. All rights reserved.
// </copyright>

namespace WorldSim
{
    using System;
    using System.Drawing;

    public static class Extensions
    {
        public static double Distance(this Point a, Point b) =>
            Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
    }
}
