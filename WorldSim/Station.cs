// <copyright file="Station.cs" company="HARK">
// Copyright (c) K. All rights reserved.
// </copyright>

namespace WorldSim
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    public class Station
    {
        public Point Position { get; init; }
        public Production Production { get; init; }
        public string Name { get; set; }
        public int Capacity = 100;
        // Allow Stations to "borrow" money from unsold stock? Does it work with price changes?
        public Wallet Wallet = new();
        public decimal Money = 0;
        public List<Station> Stations;

        /// <inheritdoc />
        public override string ToString() => Name;

        public readonly IDictionary<Product, decimal> inputs = Enum.GetValues<Product>().ToDictionary(x => x, _ => 0m);
        public readonly IDictionary<Product, decimal> outputs = Enum.GetValues<Product>().ToDictionary(x => x, _ => 0m);

        public void Step()
        {
            // var a = Production.Output.Items.Min(x => outputs[x.Product] / x.Count) >
            //         Production.Input.Items.Min(x => inputs[x.Product] / x.Count) / 4;
            // // If outputs are 4x larger than inputs, stop
            // if (a) return;
            // If we have all the inputs in correct ratios
            // TODO: Duplicate items will cause problems. GroupBy would fix that.
            if (!Production.Input.Items.All(i => inputs.ContainsKey(i.Product) && inputs[i.Product] >= i.Count)) return;

            if (outputs.Values.Max() >= Capacity)
            {
                return;
            }
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

        public IEnumerable<Contract> GenerateContracts()
        {
            // var costOfFuel = Stations.CostToMake(Product.Fuel);
            // Buy inputs for output at 0% profit (so exactly the amount I need to make the output)
            return Production.Input.Items.Select(portion =>
            {
                // This is buying a single input, but paying in output, also takes the other
                // inputs that weren't provided, to ensure the ratio is correct.
                // TODO: This doesn't take the output ratio into account. An inverse ratio will ruin everything
                // 100 Metal for 1 Ship
                return new Contract(
                    this,
                    portion.Product,
                    //portion.Product is Product.Fuel ? 1 : Stations.CostToMake(portion.Product),
                    new Ratio(Production.Input.Items
                        .Where(x => x.Product != portion.Product)
                        .Select(x => x with { Count = x.Count * -1 })
                        .Union(Production.Output.Items)
                        .ToArray())
                );
            });
        }

        // Input price
        public decimal BidPrice(Product product)
        {
            if (product == Product.Fuel)
            {
                return 1;
            }
            var costOfFuel = Stations.CostOfAinB(Product.Fuel, Product.Fuel);
            var upperBound = Stations.CostOfAinB(product, Product.Fuel);
            var lowerBound = upperBound * costOfFuel;

            return lowerBound;
            // How should Initial price be set? considering every station needs to set one 
            var stock = inputs[product];
            return stock;
            // var price = new decimal(2);
            //return price * (decimal)(1 - Math.Log(Math.Min(10, stock + 1), 10));
            // Console.Error.WriteLine($"[{this.Name}] Bid price for {product} is {price * (decimal)Math.Pow(0.5d, stock)}");
            // return price * (decimal) Math.Pow(0.5d, stock);
        }

        // Output price
        public decimal AskPrice(Product product)
        {
            if (product == Product.Fuel)
            {
                return 1;
            }
            var costOfFuel = Stations.CostOfAinB(Product.Fuel, Product.Fuel);
            var upperBound = Stations.CostOfAinB(product, Product.Fuel);
            var lowerBound = upperBound * costOfFuel;

            return upperBound;
            // return outputs[product];
//             var outputPortion = Production.Output.Items.First(x => x.Product == product);
//             if (Production.Input.Items.Length == 0) return 0;
//             var ins = Production.Input.Items.Min(x => inputs[x.Product] / x.Count * outputPortion.Count);
//             var futureOutput = outputs[product] + ins;
//             if (futureOutput == 0) return (decimal) 1;
//             if (StockCost == 0) return (decimal) 1;
// ;            return StockCost / futureOutput;
        }

        // DeliverInput

        public decimal DeliverInput(Product product, decimal count)
        {
            var delivered = Math.Min(Capacity - inputs[product], count);
            // var totalMoney = this.BidPrice(product) * count;
            // Money -= totalMoney;
            // StockCost += totalMoney;
            inputs[product] += delivered;
            return delivered;
            // return totalMoney;
        }

        // PickupOutput

        public decimal PickupOutput(Product product, decimal count)
        {
            var bought = Math.Min(outputs[product], count);
            // var cost = this.AskPrice(product) * bought;
            // Money += cost;
            // StockCost -= count;
            // if (StockCost < 0) StockCost = 0;
            outputs[product] -= bought;
            return bought;
        }

        // This method still seems correct. When you buy output, you pay in the correct ratio of the inputs
        public Ratio Buy(Portion portion)
        {
            var outputCount = Production.Output.Items.FirstOrDefault(x => x.Product == portion.Product)!.Count;
            return new Ratio(Production.Input.Items.Select(x => new Portion(x.Product, (decimal)x.Count / outputCount * portion.Count)).ToArray());
            // Buying output, I want to be paid in All Inputs
            // Buy 1 Gas, I want N fuel (to make 1 Gas)
            // 1 Fuel makes 10 Gas, so 1 Gas is 0.1 Fuel
            //
            // 2 Gas, 1 Food makes 6 Fuel
            // 1 Fuel = 1 Gas or 0.1 Food
            // GGGGFF

            // 20 Food + 5 Gas + 5 Pars makes 1 Ship
            // Food is 20 / 30 = 0.666 of a ship
            // 30 * 0.666
            // 10 Food is 0.33 of Ship
            // 1 Food is 0.033 of Ship
            // To sustain, it can buy 1 Food for 5 Ship
            // var inputCount = Production.Input.Items.Sum(x => x.Count);
            // var a = Production.Input.Items.Select(x => (product: x.Product, count: ((decimal)x.Count / inputCount) * inputCount * outputCount * portion.Count)).ToArray();
            //return a;
        }

        public void ReBalanceWallet()
        {
            // Take all inputs, and convert them into outputs using actual conversion ratio
            // 1 Food makes 10 Gas
            var canMake = Production.Input.Items.Select(x => Math.Floor(Wallet.wallet[x.Product] / x.Count)).Min();
            if (canMake >= 1)
            {
                // Subtract canMake from each input from wallet
                foreach (var (product, count) in Production.Input.Items)
                {
                    Console.WriteLine($"WALLET EXCHANGE: {Name} Subtracting {count * canMake} {product}");
                    Wallet.wallet[product] -= count * canMake;
                }
                // Add 1 * canMake of each output to wallet
                foreach (var (product, count) in Production.Output.Items)
                {
                    Console.WriteLine($"WALLET EXCHANGE: {Name} Adding {count * canMake} {product}");
                    Wallet.wallet[product] += count * canMake;
                }
            }
        }
    }
}
