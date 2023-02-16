// <copyright file="Extensions.cs" company="HARK">
// Copyright (c) K. All rights reserved.
// </copyright>

namespace WorldSim
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

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

        public static Wallet Combine(this IEnumerable<Wallet> wallets) =>
            Wallet.Combine(wallets);

        // Cost To Make Fuel
        // 1 Fuel is made from 0.1 Food + 0.1 Gas
        // Cost of 0.1 Food = 0.01 Fuel
        // Cost of 0.1 Gas = 0.01 Food = 0.001 Fuel
        // Total = 0.011
        public static decimal CostToMake(this List<Station> stations, Portion thingToCalculate, Product target = Product.Fuel)
        {
            var producer = stations.FirstOrDefault(x => x.Production.Output.Items.Any(p => p.Product == thingToCalculate.Product))!;

            var portionOfOutput = producer.Production.Output.Items.FirstOrDefault(x => x.Product == thingToCalculate.Product)!.Count;

            // Look at all inputs, and estimate their value recursively
            var countOfAllInputs = producer.Production.Input.Items.Sum(input =>
            {
                if (input.Product == target)
                {
                    return (decimal)input.Count;
                }

                return CostToMake(stations, input);
            });
            return countOfAllInputs / portionOfOutput * thingToCalculate.Count;
        }

        public static Ratio CostInOutput(this List<Station> stations, Portion input)
        {
            var consumer = stations.FirstOrDefault(x => x.Production.Input.Items.Any(i => i.Product == input.Product))!;
            return new Ratio(Product.Fuel);
        }

        // Refinery is selling Fuel at 1
        // To make 1 Fuel it uses 0.1 Food + 0.1 Gas
        // That means that 1 Food costs 5 Fuel, 1 Gas costs 5 Fuel
        // Maximum it should pay is 5 Fuel
        // Yet, 1 Gas costs 0.01 Fuel
        // Full price of output is divided across all the inputs
        //
        // Collector is selling Gas at X Fuel
        // To make 1 Gas it uses 0.1 Food
        // That means 1 Food costs 10 Gas
        // To make 1 Food Farm uses 0.1 Fuel
        // That means 1 Fuel costs 10 Food
        // 10 Food costs 100 Gas
        // Buy 1 Fuel for 100 Gas
        // Sell 1 Gas for 0.01 Fuel?
        //
        // Farm is selling Food at X fuel
        // To make 1 Food it uses 0.1 Fuel
        // That means 1 Fuel costs 10 Food
        // Max it should pay is 0.1 Fuel
        public static decimal MaxPrice(this List<Station> stations, Portion thingToCalculate)
        {
            return 0;
            // var target = Product.Fuel;
            // Station that uses our input
            // var station = stations.FirstOrDefault(x => x.Production.Input.Items.Any(p => p.Product == thingToCalculate.Product))!;

            // var totalInputCount
            // var portionOfInput = producer.Production.Output.Items.FirstOrDefault(x => x.Product == target)!.Count;
            //
            // // Look at all inputs, and estimate their value recursively
            // var countOfAllInputs = producer.Production.Input.Items.Sum(input =>
            // {
            //     if (input.Product == Product.Fuel)
            //     {
            //         return (decimal)input.Count;
            //     }
            //
            //     return CostToMake(stations, input);
            // });
            // return countOfAllInputs / portionOfOutput * thingToCalculate.Count;
        }

        // Max price of Food in Fuel
        // 1 Fuel makes 10 Food
        // 10 Food is 
        // -----
        // Minimum cost of item: Based on the value of the inputs it comes from
        // Cost VS Price, and Min vs Max
        // 1 Fuel makes 10 Food
        // 1 Food makes 10 Fuel
        // Captain can buy 50 Food for 5 Fuel, or 1 Fuel (min), and for 50 Fuel (max)
        // 1 Fuel costs 0.011 Fuel to make, but can sell from 0.011 > 1
        // 1 Food costs 0.1 to make, but can sell from 0.1 to 1? What is the upper limit
        // 1 Food makes 10 Fuel (with 1 Gas at 0.01), so max it can cost is 9.98 Fuel
        public static decimal CanMakeFuel(this List<Station> stations, Portion portion)
        {
            // How much fuel can this portion make, Find all who take this as input
            // Should probably average out all producers? Or use Min, as in, who can produce cheapest
            var consumer = stations
                .FirstOrDefault(x => x.Production.Input.Items.Any(p => p.Product == portion.Product))!;

            var portionOfOutput = consumer.Production.Output.Items;
            var portions = consumer.Production.Output.Items;
            // Divide the full cost among the portions
            var portionTotal = portions.Sum(x => x.Count);
            var sum = portions.Sum(x =>
            {
                var sum = x.Product is Product.Fuel ? x.Count : stations.CanMakeFuel(x);
                return sum / portionTotal;
            });
            return sum;
        }

        public static decimal CostOfAinB(this List<Station> stations, Portion thingToCalculate, Product inThisProduct)
        {
            var producer = stations.FirstOrDefault(x => x.Production.Output.Items.Any(p => p.Product == thingToCalculate.Product))!;

            var portionOfOutput = producer.Production.Output.Items.FirstOrDefault(x => x.Product == thingToCalculate.Product)!.Count;

            // Look at all inputs, and estimate their value recursively
            var countOfAllInputs = producer.Production.Input.Items.Sum(input =>
            {
                if (input.Product == inThisProduct)
                {
                    return (decimal)input.Count;
                }

                return CostOfAinB(stations, input, inThisProduct);
            });
            return countOfAllInputs / portionOfOutput * thingToCalculate.Count;
        }
    }
}
