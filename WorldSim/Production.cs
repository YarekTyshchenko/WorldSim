// <copyright file="Production.cs" company="HARK">
// Copyright (c) K. All rights reserved.
// </copyright>

namespace WorldSim
{
    public enum Product
    {
        Gas,
        Food,
        Fuel,
    }

    public static class ProductExt
    {
        public static Portion Many(this Product product, int count) =>
            new(product, count);
    }

    public record Portion(Product Product, int Count = 1)
    {
        /// <inheritdoc />
        public override string ToString() =>
            Count == 1
                ? $"{Product}"
                : $"{Count} {Product}";

        public static implicit operator Portion(Product product) => new(product);
    };

    public record Ratio(params Portion[] Items)
    {
        /// <inheritdoc />
        public override string ToString()
        {
            return Items.Join(", ");
        }

        public static implicit operator Ratio(Portion portion) =>
            new(portion);
    };

    public record Production(Ratio Input, Ratio Output)
    {
        /// <inheritdoc />
        public override string ToString()
        {
            return $"({Input}) -> ({Output})";
        }
    };

    public class Foo
    {
        public void F()
        {
            var p = new Production(new Ratio(Product.Gas, Product.Food), Product.Fuel.Many(10));
        }
    }
}
