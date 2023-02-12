// <copyright file="Extensions.cs" company="HARK">
// Copyright (c) K. All rights reserved.
// </copyright>

namespace WorldSim
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    public static class Extensions
    {
        public static double Distance(this Point a, Point b) =>
            Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));

        public static string Join<K, V>(this IDictionary<K, V> dict) =>
            string.Join("", dict);

        public static string Join<V>(this IEnumerable<V> list, string sep = "\n") =>
            string.Join(sep, list);

        public static T? RandomElement<T>(this IEnumerable<T> source,
            Random rng)
        {
            var current = default(T);
            var count = 0;
            foreach (var element in source)
            {
                count++;
                if (rng.Next(count) == 0)
                {
                    current = element;
                }
            }
            return count == 0 ? default : current;
        }
    }
}
