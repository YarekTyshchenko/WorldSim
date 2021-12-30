// <copyright file="Station.cs" company="HARK">
// Copyright (c) K. All rights reserved.
// </copyright>

namespace WorldSim
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    public class GenericStation
    {
        public Point Position { get; init; }
        public decimal Money { get; set; }
        public decimal StockCost { get; set; }
        public Production Production { get; init; }
        public readonly IDictionary<Product, int> inputs = Enum.GetValues<Product>().ToDictionary(x => x, _ => 0);
        public readonly IDictionary<Product, int> outputs = Enum.GetValues<Product>().ToDictionary(x => x, _ => 0);

        public void Step()
        {
            // If we have all the inputs in correct ratios
            // TODO: Duplicate items will cause problems. GroupBy would fix that.
            if (!Production.Input.Items.All(i => inputs.ContainsKey(i.Product) && inputs[i.Product] >= i.Count)) return;

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
            if (Production.Input.Items.Length == 0) return 0;
            var ins = Production.Input.Items.Min(x => inputs[x.Product] / x.Count * outputPortion.Count);
            var futureOutput = outputs[product] + ins;
            if (futureOutput == 0) return 0;
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
}
