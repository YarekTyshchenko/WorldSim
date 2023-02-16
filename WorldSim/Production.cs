// <copyright file="Production.cs" company="HARK">
// Copyright (c) K. All rights reserved.
// </copyright>

namespace WorldSim
{
    using System.Linq;

    public enum Product
    {
        Gas,
        Food,
        Fuel,
        // Metal,
        // Parts,
        // Ship
    }

    public static class ProductExt
    {
        public static Portion Many(this Product product, decimal count) =>
            new(product, count);
    }

    public record Portion(Product Product, decimal Count = 1)
    {
        /// <inheritdoc />
        public override string ToString() =>
            Count == 1
                ? $"{Product}"
                : $"{Count} {Product}";

        public static implicit operator Portion(Product product) => new(product);
    }

    public record Ratio(params Portion[] Items)
    {
        /// <inheritdoc />
        public override string ToString() =>
            Items.Join(", ");

        public static implicit operator Ratio(Portion portion) =>
            new(portion);

        public static implicit operator Ratio(Product product) =>
            new(new Portion(product));

        public Ratio Scale(decimal loadedAmount)
        {
            return new Ratio(Items.Select(x => x with { Count = x.Count * loadedAmount }).ToArray());
        }
    }

    public record Production(Ratio Input, Ratio Output)
    {
        /// <inheritdoc />
        public override string ToString() =>
            $"({Input}) -> ({Output})";
    }
}
