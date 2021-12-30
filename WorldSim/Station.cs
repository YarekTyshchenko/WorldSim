// <copyright file="Station.cs" company="HARK">
// Copyright (c) K. All rights reserved.
// </copyright>

namespace WorldSim
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

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

    public enum StationType
    {
        Farm,
        Collector,
        Refinery,
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

    public class GenericStation
    {
        public Point Position { get; init; }
        public decimal Money { get; set; }
        public decimal StockCost { get; set; }
        public Production Production { get; init; }
        private readonly IDictionary<Product, int> inputs = new Dictionary<Product, int>();
        private readonly IDictionary<Product, int> outputs = new Dictionary<Product, int>();

        public void Step()
        {
            // If we have all the inputs in correct ratios
            // TODO: Duplicate items will cause problems. GroupBy would fix that.
            if (!Production.Input.Items.All(i => inputs.ContainsKey(i.Product) && inputs[i.Product] > i.Count)) return;

            // Remove them, and add the outputs.
            foreach (var (product, count) in Production.Input.Items)
            {
                inputs[product] -= count;
            }

            foreach (var (product, count) in Production.Output.Items)
            {
                outputs[product] += count;
            }
        }

        public decimal BidPrice(Product product)
        {
            // Bid price is based on how much stock we have
            // (should also be based on how much cash we have)
            var stock = inputs[product];
            var price = new decimal(1);
            return price * (decimal)(1 - Math.Log(Math.Min(10, stock + 1), 10));
        }

        // Ask price is based on what we paid for the input
        public decimal AskPrice(Product product)
        {
            var outputPortion = Production.Output.Items.First(x => x.Product == product);
            var ins = Production.Input.Items.Min(x => inputs[x.Product] / x.Count * outputPortion.Count);
            var futureOutput = outputs[product] + ins; 
            return StockCost / futureOutput;
        }

        // DeliverInput
        public decimal DeliverInput(Product product, int count)
        {
            var totalMoney = this.BidPrice(product) * count;
            Money -= totalMoney;
            StockCost += totalMoney;
            inputs[product] += count;
            return totalMoney;
        }

        // PickupOutput
        public (int bought, decimal cost) PickupOutput(Product product, int count)
        {
            var bought = Math.Min(outputs[product], count);
            var cost = this.AskPrice(product) * bought;
            Money += cost;
            StockCost -= count;
            if (StockCost < 0) StockCost = 0;
            outputs[product] -= bought;
            return (bought, cost);
        }
    }

    public abstract class Station
    {
        public Point Position { get; }
        public int Food { get; set; }
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
        public abstract double BidPrice(Product product);

        // Adjust higher than ProductToPrice
        public abstract double AskPrice();
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
        public override double BidPrice(Product product)
        {
            return 0;
        }

        /// <inheritdoc />
        public override (int bought, double cost) BuyOutput(int contractAmount)
        {
            var bought = Math.Min(contractAmount, Food);
            var price = this.AskPrice() * bought;
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
            var x = 10;
            var a =  x * (1 - Math.Log(Math.Min(10, Food+1), 10));
            Console.Error.WriteLine($"Bid price for {product} is {a} on {this}");
            return a;
        }

        /// <inheritdoc />
        public override double AskPrice()
        {
            var futureGas = Food * 10 + Gas;
            if (futureGas == 0)
            {
                return 0.1;
            }

            Console.Error.WriteLine($"Total {this} buy cost is {TotalBuyCost} for {futureGas} Future gas: Ask price: {TotalBuyCost / futureGas:R}");
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
            var x = this.BidPrice(product);
            var price = x * amount;
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
            var x = product switch
            {
                Product.Food => 10,
                Product.Gas => 1,
            };
            var stock = product switch
            {
                Product.Gas => Gas,
                Product.Food => Food,
            };
            var a =  x * (1 - Math.Log(Math.Min(10, stock+1), 10));
            Console.Error.WriteLine($"Bid price for {product} is {a} on {this}");
            return a;
        }

        /// <inheritdoc />
        public override double AskPrice()
        {
            var futureFuel = Math.Min(Gas * 10, Food * 10) + Fuel;
            if (futureFuel == 0) return 0.11;

            Console.Error.WriteLine($"Total buy cost is {TotalBuyCost} for {futureFuel} Future gas: Ask price: {TotalBuyCost / futureFuel} on {this}");
            // Between 0.1 and Infinity
            return TotalBuyCost / futureFuel;

        }

        public override (int bought, double cost) BuyOutput(int contractAmount)
        {
            var bought = Math.Min(contractAmount, Fuel);
            var price = this.AskPrice() * bought;
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
