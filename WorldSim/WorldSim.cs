// <copyright file="WorldSim.cs" company="HARK">
// Copyright (c) K. All rights reserved.
// </copyright>

namespace WorldSim
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Medallion;

    public class WorldSim
    {
        private List<Station> stations;
        private List<Captain> captains;
        private int nextCaptainName = 1;
        private string CapName() => (nextCaptainName++).ToString();

        public WorldSim(List<Station> stations)
        {
            this.stations = stations;
            foreach (var productA in Enum.GetValues<Product>())
            {
                // var inFuel = this.stations.CostToMake(productA);
                // Console.WriteLine($"1 {productA} costs {inFuel} Fuel");
                // foreach (var productB in Enum.GetValues<Product>())
                // {
                //     if (productA != productB)
                //     {
                //         Console.WriteLine($"{productA} --makes-> {productB}: Price {this.stations.CostOfAinB(productA, productB)}");
                //     }
                // }
            }
            this.captains = new List<Captain>
            {
                new Captain(stations, CapName(), Program.RandomPoint(), 10, new Wallet
                {
                    wallet = Enum.GetValues<Product>().ToDictionary(x => x, _ => 20m),
                }),
                // new Captain(stations, CapName(), Program.RandomPoint(), 10),
            };
        }

        public void Run(Random random)
        {
            var contracts = new List<Contract>();
            // Loop through all stations and generate contracts
            foreach (var station in stations)
            {
                contracts.AddRange(station.GenerateContracts());
            }
            Console.Error.WriteLine($"New Step --- Created {contracts.Count} Contracts");
            Console.Error.WriteLine(contracts.Join());

            // each captain
            foreach (var captain in captains)
            {
                captain.Act(contracts, stations, random);
            }

            // All stations consume their food
            foreach (var station in stations.Shuffled(random))
            {
                station.Step();
            }

            foreach (var station in stations)
            {
                Console.WriteLine($"Station {station.Name} {station.Production} Input: {station.inputs.Where(x => x.Value > 0).Join(",")}, Output: {station.outputs.Where(x => x.Value > 0).Join(",")}, ");
                Console.WriteLine(station.Wallet);

                // if (station.outputs[Product.Ship] > 0)
                // {
                //     station.outputs[Product.Ship] -= 1;
                //     captains.Add(new Captain(stations, CapName(), station.Position, 10));
                // }
            }
            foreach (var captain in captains)
            {
                Console.WriteLine($"Captain {captain} (idle for {captain.idleDays}) has {(int)captain.Fuel} in tank and {captain.Loaded} in hold {captain.Wallet}");
            }

            foreach (var product in Enum.GetValues<Product>())
            {
                Console.WriteLine($"Total {Enum.GetName(product)} {stations.Sum(x => x.outputs[product] + x.inputs[product])}");
            }

            Console.WriteLine($"Unshipped amount: {stations.Sum(x => x.outputs.Sum(o => o.Value))}");
            Console.WriteLine($"Total amount {stations.Sum(x => x.outputs.Concat(x.inputs).Sum(b => b.Value))}");

            // Total money
            Console.WriteLine($"Total Captains money: {captains.Select(x => x.Wallet).Combine()}");
            Console.WriteLine($"Total Stations money: {stations.Select(x => x.Wallet).Combine()}");
            Console.WriteLine($"Total Captains money {captains.Select(x => x.Wallet).Combine().wallet.Values.Sum()}");
            Console.WriteLine($"Total Stations money {stations.Select(x => x.Wallet).Combine().wallet.Values.Sum()}");
            Console.WriteLine($"Total Money {captains.Select(x => x.Wallet).Union(stations.Select(x => x.Wallet)).Combine()}");
        }
    }
}
