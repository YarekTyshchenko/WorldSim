// <copyright file="Station.cs" company="HARK">
// Copyright (c) K. All rights reserved.
// </copyright>

namespace WorldSim
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    // 1 Food => 10 Gas
    // 1 Gas + 1 Food => 10 Fuel
    
    // Cost:
    // 1 Gas = 0.1 Food
    // 1 Fuel = 0.11 Food
    
    // Buy Cost:
    // 1 Gas = Max 0.1
    // Sell Cost:
    // 1 Gas = Min 0.1
    // Behold = Margin
    public enum Product
    {
        Gas,
        Food,
        Fuel,
    }

    public static class Ext
    {
        public static double ProductToPrice(this Product product) => product switch
        {
            Product.Gas => 0.1,
            Product.Food => 1,
            Product.Fuel => 0.11,
            _ => throw new ArgumentOutOfRangeException(nameof(product), product, null)
        };
    }

    public abstract class Station
    {
        public Point Position { get; }
        public int Food { get; set; } = 10;
        public double Money { get; set; } = 1000;
        public abstract Product GetOutputProduct();

        public Station(Point position)
        {
            this.Position = position;
        }

        public abstract void Step();

        public abstract (int bought, double cost) BuyOutput(int contractAmount);

        public abstract double DeliverInput(Product product, int amount);

        public int GetAvailableOutput() => this switch
        {
            Collector collector => collector.Gas,
            Farm farm => farm.Food,
            Refinery refinery => refinery.Fuel,
            _ => throw new ArgumentOutOfRangeException()
        };

        public int? GetAvailableInput() => this switch
        {
            Collector collector => collector.Food,
            Refinery refinery => refinery.Gas,
            _ => null
        };

        // Adjust lower than ProductToPrice
        public virtual double BidPrice(Product product) => this switch
        {
            Collector collector => Product.Food.ProductToPrice(),
            Farm farm => 0,
            Refinery refinery => product switch
            {
                Product.Food => Product.Food.ProductToPrice(),
                Product.Gas  => Product.Gas.ProductToPrice(),
                _ => throw new ArgumentOutOfRangeException(nameof(product), product, null)
            },
            _ => throw new ArgumentOutOfRangeException()
        };

        // Adjust higher than ProductToPrice
        public virtual double AskPrice() => this switch
        {
            Collector collector => Product.Gas.ProductToPrice() + 0.1,
            Farm farm => Product.Food.ProductToPrice() + 0.1,
            Refinery refinery => Product.Fuel.ProductToPrice() + 0.1,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public class Farm : Station
    {
        public override Product GetOutputProduct() => Product.Food;

        public override void Step()
        {
            Food += 10;
        }

        /// <inheritdoc />
        public override double AskPrice()
        {
            return 0;
        }

        /// <inheritdoc />
        public override (int bought, double cost) BuyOutput(int contractAmount)
        {
            var bought = Math.Min(contractAmount, Food);
            var price = this.AskPrice() * contractAmount;
            Food -= bought;
            Money += price;
            return (bought, price);
        }

        /// <inheritdoc />
        public override double DeliverInput(Product product, int amount)
        {
            throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public Farm(Point position) : base(position)
        {
        }
    }

    public class Collector : Station
    {
        public int Gas { get; set; }
        // Total cost of all input stock
        // Used to work out how to price the output to ensure
        // profit is always made
        public double TotalBuyCost;
        public int DaysWithoutBuy;
        public int DaysWithoutSell;
        public override Product GetOutputProduct() => Product.Gas;

        // 1 Food => 10 Gas
        public override void Step()
        {
            DaysWithoutBuy++;
            DaysWithoutSell++;
            if (Food > 0)
            {
                Food--;
                Gas += 10;
            }
        }

        /// <inheritdoc />
        public override double BidPrice(Product product)
        {
            var x = base.BidPrice(product);
            var a =  x * (1 - Math.Log(Math.Min(1000, Food+1), 1000));
            Console.Error.WriteLine($"Bid price for {product} is {a} on {this}");
            return a;
        }

        /// <inheritdoc />
        public override double AskPrice()
        {
            var futureGas = Food * 10 + Gas;
            if (futureGas == 0)
            {
                return base.AskPrice();
            }

            Console.Error.WriteLine($"Total buy cost is {TotalBuyCost} for {futureGas} Future gas: Ask price: {TotalBuyCost / futureGas}");
            // Between 0.1 and Infinity
            return TotalBuyCost / futureGas;
        }

        // Cost of 1 Gas is 0.1
        // Ensure station always makes profit
        public override (int bought, double cost) BuyOutput(int contractAmount)
        {
            var bought = Math.Min(contractAmount, Gas);
            var price = this.AskPrice() * bought;
            // Subtract the money made from the Total buy cost of input
            TotalBuyCost -= price;
            
            DaysWithoutBuy = 0;
            Gas -= bought;
            Money += price;
            return (bought, price);
        }

        /// <inheritdoc />
        public override double DeliverInput(Product product, int amount)
        {
            var p = this.BidPrice(product);
            var price = this.BidPrice(product) * amount;
            TotalBuyCost += price;
            DaysWithoutSell = 0;
            switch (product)
            {
                case Product.Food:
                    Food += amount;
                    Money -= price;
                    return price;
            }

            throw new InvalidOperationException();
        }

        public Collector(Point position) : base(position)
        {
        }
    }

    public class Refinery : Station
    {
        public int Gas { get; set; }
        public int Fuel { get; set; }
        public double TotalBuyCost;
        public override Product GetOutputProduct() => Product.Fuel;


        public override void Step()
        {
            if (Food > 0 && Gas > 0)
            {
                Food--;
                Gas--;
                Fuel += 10;
            }
        }

        /// <inheritdoc />
        public Refinery(Point position) : base(position)
        {
        }

        /// <inheritdoc />
        public override double BidPrice(Product product)
        {
            var x = base.BidPrice(product);
            var stock = product switch
            {
                Product.Gas => Gas,
                Product.Food => Food,
            };
            var a =  x * (1 - Math.Log(Math.Min(1000, stock+1), 1000));
            Console.Error.WriteLine($"Bid price for {product} is {a} on {this}");
            return a;
        }

        /// <inheritdoc />
        public override double AskPrice()
        {
            var futureFuel = Math.Min(Gas * 10, Food * 10) + Fuel;
            if (futureFuel == 0) return base.AskPrice();

            Console.Error.WriteLine($"Total buy cost is {TotalBuyCost} for {futureFuel} Future gas: Ask price: {TotalBuyCost / futureFuel}");
            // Between 0.1 and Infinity
            return TotalBuyCost / futureFuel;

        }

        public override (int bought, double cost) BuyOutput(int contractAmount)
        {
            var bought = Math.Min(contractAmount, Fuel);
            var price = this.AskPrice() * contractAmount;
            TotalBuyCost -= price;
            Fuel -= bought;
            return (bought, price);
        }

        /// <inheritdoc />
        public override double DeliverInput(Product product, int amount)
        {
            var price = this.BidPrice(product) * amount;
            TotalBuyCost += price;
            switch (product)
            {
                case Product.Gas:
                    Gas += amount;
                    Money -= price;
                    return price;
                case Product.Food:
                    Food += amount;
                    Money -= price;
                    return price;
            }

            throw new InvalidOperationException();
        }
    }
}
