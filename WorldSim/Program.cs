using System;
using System.Drawing;
using WorldSim;

namespace WorldSim
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public static class Plot
    {
        public static void Food(int amount) => Line("food", amount);

        public static void Clear(string file) => File.Delete(file);

        public static void Line(string file, int amount)
        {
            File.AppendAllLines(file, new List<string> {$"{amount}"});
        }
    }

    class Program
    {
        public static readonly Random random = new Random(42);

        static void Main(string[] args)
        {
            Station Farm()
            {
                var f = new Station {Position = RandomPoint(), Money = 1000, Production = new Production(Product.Fuel, Product.Food.Many(3)),};
                f.inputs[Product.Fuel] = 1000;
                return f;
            }

            Station Collector() => new Station
            {
                Position = RandomPoint(),
                Money = 1000,
                Production = new Production(Product.Food, Product.Gas.Many(3))
            };

            Station Refinery() => new Station
            {
                Position = RandomPoint(),
                Money = 1000,
                Production = new Production(new Ratio(Product.Food, Product.Gas), Product.Fuel.Many(3)),
            };

            var stations = new List<Station>
            {
                // Raw
                // new Mine(RandomPoint()),
                Collector(),
                Collector(),
                Collector(),
                Farm(),

                // Produced
                // new Factory(RandomPoint()),
                Refinery(),
                Refinery(),
                Refinery(),
                Refinery(),
                // new Shipyard(RandomPoint()),
            };
            var sim = new WorldSim(stations);
            var n = 0;
            while (n++ < 10000)
            {
                sim.Run();
                Task.Delay(10).Wait();
            }
        }

        public static Point RandomPoint() => new Point(random.Next(0, 100), random.Next(0, 100));
    }

    public record Contract(
        Station Destination,
        //Station Source,
        Product Product);

    public class WorldSim
    {
        private List<Station> stations;
        private List<Captain> captains;

        public WorldSim(List<Station> stations)
        {
            this.stations = stations;
            this.captains = new List<Captain>
            {
                new Captain(Program.RandomPoint(), 1000),
                new Captain(Program.RandomPoint(), 1000),
            };
        }

        const int maxAmount = 10000;
        public void Run()
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

            var averageFuelPrice = refineries
                .Select(s => s.AskPrice(Product.Fuel))
                .Average();

            // each captain
            foreach (var captain in captains)
            {
                captain.idleDays++;
                // Find most profitable route
                var routes = contracts
                    .SelectMany(contract => stations
                        .Where(producer => producer.Production.Output.Items.Any(x => x.Product == contract.Product))
                        .Select(p =>
                        {
                            var bidPrice = contract.Destination.BidPrice(contract.Product);
                            var output = p.outputs[contract.Product];
                            var available = Math.Min(100, Math.Min(
                                output,
                                bidPrice > (decimal) 0.01 ? (int)Math.Floor(contract.Destination.Money / bidPrice) : output));
                            return new
                            {
                                available,
                                position = p.Position,
                                contract,
                                producer = p,
                                distanceTotal = captain.Position.Distance(p.Position) +
                                                p.Position.Distance(contract.Destination.Position),
                                buyCost = p.AskPrice(contract.Product) * available,
                                sellPayout = contract.Destination.BidPrice(contract.Product) * available,
                            };
                        }))
                    // Filter out contracts that can't pay
                    .Where(x =>
                        x.contract.Destination.Money > x.sellPayout)
                    // Filter out negative contracts
                    .Where(x => x.sellPayout - x.buyCost - (decimal)x.distanceTotal * averageFuelPrice > 0)
                    .OrderByDescending(x => x.sellPayout - x.buyCost);

                Console.Error.WriteLine($"Available Routes: {routes.Join()}");

                var foundRoute = routes.FirstOrDefault();
                Console.Error.WriteLine($"Chosen {foundRoute}");
                if (foundRoute == null) continue;
                var contract = foundRoute.contract;
                var producer = foundRoute.producer;

                var amountLoaded = foundRoute.available;
                var fuelCost = (decimal)foundRoute.distanceTotal * averageFuelPrice;
                if (foundRoute.sellPayout - foundRoute.buyCost - fuelCost > 0)
                {
                    Console.Error.WriteLine(
                        $"Route {foundRoute} is good. Fuel cost {fuelCost}");
                }
                else
                {
                    Console.Error.WriteLine(
                        $"Not worth {foundRoute}, fuel cost {fuelCost}");
                    continue;
                }

                contracts.Remove(contract);
                if (producer.Position != captain.Position)
                {
                    // Go to producer
                    captain.GoTo(producer);
                    // Refuel
                    captain.Refuel(producer);
                }
                captain.Load(producer, contract.Product, amountLoaded);

                // go to consumer
                captain.GoTo(contract.Destination);
                // Sell to consumer
                captain.Unload(contract);
                // Refuel
                captain.Refuel(contract.Destination);

                var nearestFuel = refineries
                    .Where(s => s.outputs[Product.Fuel] > 0)
                    .OrderByDescending(x => x.Position.Distance(captain.Position))
                    .FirstOrDefault();

                if (captain.Fuel < 1000 / 2 && nearestFuel != null)
                {
                    if (captain.Position != nearestFuel.Position)
                    {
                        captain.GoTo(nearestFuel);
                    }

                    captain.Refuel(nearestFuel);
                }
            }

            // All stations consume their food
            foreach (var station in stations)
            {
                station.Step();
                Console.WriteLine($"Station {station.Production} Input: {station.inputs.Where(x => x.Value > 0).Join(",")}, Output: {station.outputs.Where(x => x.Value > 0).Join(",")}, Cash: {station.Money}, Stock Cost: {station.StockCost}");
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
                Console.WriteLine($"Captain {captain} has {captain.Money} in wallet, {captain.Fuel} in tank");
            }

            // captains = captains.Where(x => x.idleDays < 10).ToList();
            Console.WriteLine($"Total fuel available {totalAvailableFuel}");
            Console.WriteLine($"Total cash in the world {stations.Sum(x => x.Money) + captains.Sum(x => x.Money)}");
        }
    }

    public class Captain
    {
        public Point Position;
        public double Fuel;

        public decimal Money = 1000;
        //private int maxFuel = 1000;
        private int loadedAmount = 0;
        public int idleDays = 0;
        private Product loadedProduct;

        public Captain(Point position, int fuel)
        {
            this.Position = position;
            this.Fuel = fuel;
        }


        public void GoTo(Station st)
        {
            idleDays = 0;
            var n = this.Position.Distance(st.Position);
            this.Position = st.Position;
            this.Fuel -= n;
            Console.Error.WriteLine($"Captain went to {st} costing {n} fuel, remaining {this.Fuel}");
        }

        public void Refuel(Station refinery)
        {
            if (refinery.Production.Output.Items.All(x => x.Product != Product.Fuel)) return;

            var want = (int)Math.Floor(1000 - Fuel);
            if (want < 1) return;

            var (bought, cost) = refinery.PickupOutput(Product.Fuel, want);
            Money -= cost;
            Fuel += bought;
            Console.Error.WriteLine($"Refuelled at {refinery} for {bought} fuel, at a cost of {cost}");
        }

        public void Load(Station producer, Product product, int contractAmount)
        {
            var (available, cost) = producer.PickupOutput(product, contractAmount);
            Money -= cost;
            loadedAmount = available;
            loadedProduct = product;

            Console.Error.WriteLine($"Captain loaded {available} {loadedProduct} cargo");
        }

        public void Unload(Contract contract)
        {
            var payment = contract.Destination.DeliverInput(loadedProduct, loadedAmount);
            this.Money += payment;
            Console.Error.WriteLine($"Captain unloaded {loadedAmount} {loadedProduct} cargo at {contract.Destination} for {payment} cash, in bank {this.Money}");
            loadedAmount = 0;
        }
    }
}

