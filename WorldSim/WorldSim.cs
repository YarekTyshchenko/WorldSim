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
            this.captains = new List<Captain>
            {
                new Captain(CapName(), Program.RandomPoint(), 10),
                new Captain(CapName(), Program.RandomPoint(), 10),
                new Captain(CapName(), Program.RandomPoint(), 10),
                new Captain(CapName(), Program.RandomPoint(), 10),
            };
        }

        public void Run(Random random)
        {
            var contracts = new List<Contract>();
            // Loop through all stations and generate contracts
            foreach (var station in stations)
            {
                contracts.AddRange(station.Production.Input.Items.Select(portion =>
                    new Contract(station, portion.Product)));
            }
            Console.Error.WriteLine($"New Step --- Created {contracts.Count} Contracts");
            Console.Error.WriteLine(contracts.Join());

            var refineries = stations
                .Where(s => s.Production.Output.Items.Any(x => x.Product == Product.Fuel))
                .ToList();

            // each captain
            foreach (var captain in captains.Shuffled(random))
            {
                captain.Act(contracts, stations, refineries, random);
            }

            // All stations consume their food
            foreach (var station in stations.Shuffled(random))
            {
                station.Step();
            }

            foreach (var station in stations)
            {
                Console.WriteLine($"Station {station.Production} Input: {station.inputs.Where(x => x.Value > 0).Join(",")}, Output: {station.outputs.Where(x => x.Value > 0).Join(",")}");

                if (station.outputs[Product.Ship] > 0)
                {
                    station.outputs[Product.Ship] -= 1;
                    captains.Add(new Captain(CapName(), station.Position, 10));
                }
            }
            foreach (var captain in captains)
            {
                Console.WriteLine($"Captain {captain} (idle for {captain.idleDays}) has {(int)captain.Fuel} in tank and {captain.Loaded} in hold");
            }

            foreach (var product in Enum.GetValues<Product>())
            {
                Console.WriteLine($"Total {Enum.GetName(product)} {stations.Sum(x => x.outputs[product] + x.inputs[product])}");
            }
        }
    }
}
