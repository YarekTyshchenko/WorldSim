// <copyright file="WorldSim.cs" company="HARK">
// Copyright (c) K. All rights reserved.
// </copyright>

namespace WorldSim
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Medallion;

    public class WorldSim
    {
        private List<Station> stations;
        private List<Captain> captains;

        public WorldSim(List<Station> stations)
        {
            this.stations = stations;
            this.captains = new List<Captain>
            {
                new Captain(Program.RandomPoint(), 0),
                new Captain(Program.RandomPoint(), 0),
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
            Console.Error.WriteLine($"Created {contracts.Count} Contracts");
            Console.Error.WriteLine(contracts.Join());

            var refineries = stations
                .Where(s => s.Production.Output.Items.Any(x => x.Product == Product.Fuel))
                .ToList();
            var totalAvailableFuel = refineries
                .Select(x => x.outputs[Product.Fuel])
                .Sum();

            // var averageFuelPrice = refineries
            //     .Select(s => s.AskPrice(Product.Fuel))
            //     .Average();

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
                // if (station is Shipyard {Ships: > 1} shipyard)
                // {
                //     shipyard.Ships -= 1;
                //     if (captains.All(x => x.Position != shipyard.Position))
                //     {
                //         captains.Add(new Captain(shipyard.Position, 1000, 1000));
                //     }
                // }
            }
            foreach (var captain in captains)
            {
                // if (captain.idleDays > 10)
                // {
                //     Console.Error.WriteLine($"Captain {captain} idle for more than 10 days, retiring");
                //     var shipyard = (Shipyard)stations.Find(x => x is Shipyard);
                //     shipyard.Ships += 1;
                // }
                Console.WriteLine($"Captain {captain} has {captain.Fuel} in tank");
            }

            // captains = captains.Where(x => x.idleDays < 10).ToList();
            Console.WriteLine($"Total fuel available {totalAvailableFuel}");
            // Console.WriteLine($"Total cash in the world {stations.Sum(x => x.Money) + captains.Sum(x => x.Money)}");
        }
    }
}
