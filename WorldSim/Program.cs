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
            var stations = new List<Station>
            {
                // Raw
                // new Mine(RandomPoint()),
                new Collector(RandomPoint()),
                new Collector(RandomPoint()),
                new Collector(RandomPoint()),
                new Farm(RandomPoint()),

                // Produced
                // new Factory(RandomPoint()),
                new Refinery(RandomPoint()),
                new Refinery(RandomPoint()),
                new Refinery(RandomPoint()),
                new Refinery(RandomPoint()),
                // new Shipyard(RandomPoint()),
            };
            var sim = new WorldSim(stations);
            var n = 0;
            while (n++ < 1000)
            {
                sim.Run();
                Task.Delay(100).Wait();
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
            };
        }

        const int maxAmount = 10000;
        public void Run()
        {
            var contracts = new List<Contract>();
            // Loop through all stations and generate contracts
            foreach (var station in stations)
            {
                if (station is not Farm)
                {
                    var foodContract = new Contract(station, Product.Food);
                    contracts.Add(foodContract);
                    if (station is Refinery refinery)
                    {
                        var outputContract = new Contract(refinery, Product.Gas);
                        contracts.Add(outputContract);
                    }

                }
            }
            Console.Error.WriteLine($"Created {contracts.Count} Contracts");
            Console.Error.WriteLine(contracts.Join());

            var totalAvailableFuel = stations
                .Select(s => s is Refinery refinery ? refinery : null)
                .Where(s => s != null)
                .Select(s => s!.Fuel).Sum();

            // each captain
            foreach (var captain in captains)
            {
                captain.idleDays++;
                // Find most profitable route
                var foundRoute = contracts
                    .SelectMany(contract => stations
                        .Where(producer => contract.Product == producer.GetOutputProduct())
                        .Select(p => new {
                            available = p.GetAvailableOutput(),
                            position = p.Position,
                            contract,
                            producer = p,
                            distanceTotal = captain.Position.Distance(p.Position) + p.Position.Distance(contract.Destination.Position),
                            buyCost = p.AskPrice() * p.GetAvailableOutput(),
                            sellPayout = contract.Destination.BidPrice(contract.Product) * p.GetAvailableOutput(),
                        }))
                    .Where(x =>
                        x.contract.Destination.Money > x.sellPayout)
                    //.Where(x => x.sellPayout - x.buyCost > 0)
                    .OrderByDescending(x => x.sellPayout - x.buyCost)
                    .FirstOrDefault();

                Console.Error.WriteLine($"Chosen {foundRoute}");
                if (foundRoute == null) continue;
                var contract = foundRoute.contract;
                var producer = foundRoute.producer;

                var amountLoaded = foundRoute.available;
                var distance = captain.Position.Distance(producer.Position)
                               + producer.Position.Distance(contract.Destination.Position);
                var fuelCost = distance * Product.Fuel.ProductToPrice();
                var contractPay = amountLoaded * producer.GetOutputProduct().ProductToPrice();
                if (foundRoute.sellPayout - foundRoute.buyCost - fuelCost < 0)
                {
                    Console.Error.WriteLine($"Not worth {foundRoute.buyCost} for {foundRoute.sellPayout} pay to pickup {contract.Product}");
                    continue;
                }
                else
                {
                    Console.Error.WriteLine($"It is worth {foundRoute.buyCost} for {foundRoute.sellPayout} pay to pickup {contract.Product}");
                }

                contracts.Remove(contract);
                if (producer.Position != captain.Position)
                {
                    // Go to producer
                    captain.GoTo(producer);
                    // Refuel
                    captain.Refuel(producer);
                }
                captain.Load(producer, amountLoaded);

                // go to consumer
                captain.GoTo(contract.Destination);
                // Sell to consumer
                captain.Unload(contract);
                // Refuel
                captain.Refuel(contract.Destination);
            }

            // All stations consume their food
            foreach (var station in stations)
            {
                station.Step();
                Console.WriteLine($"Station {station} Food {station.Food}, Output: {station.GetAvailableOutput()}, Input: {station.GetAvailableInput()}, Cash: {station.Money}");
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
                Console.WriteLine($"Captain {captain} has {captain.Money} in wallet");
            }

            // captains = captains.Where(x => x.idleDays < 10).ToList();
            Console.WriteLine($"Total fuel available {totalAvailableFuel}");
        }
    }

    public class Captain
    {
        public Point Position;
        public double Fuel;

        public double Money;
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
            //var n = 1;
            this.Position = st.Position;
            //this.Fuel -= n;
            this.Fuel -= n;
            Console.Error.WriteLine($"Captain went to {st} costing {n} fuel, remaining {this.Fuel}");
        }

        public void Refuel(Station refinery)
        {
            if (refinery is not Refinery) return;
            var want = (int)(1000 - Fuel);
            var (bought, cost) = refinery.BuyOutput(want);
            Money -= cost;
            Fuel += bought;
            Console.Error.WriteLine($"Refuelled at {refinery} for {bought} fuel, at a cost of {cost}");
        }

        public void Load(Station producer, int contractAmount)
        {
            var (available, cost) = producer.BuyOutput(contractAmount);
            Money -= cost;
            loadedAmount = available;
            loadedProduct = producer.GetOutputProduct();

            Console.Error.WriteLine($"Captain loaded {available} {loadedProduct} cargo");
        }

        public void Unload(Contract contract)
        {
            var payment = contract.Destination.DeliverInput(loadedProduct, loadedAmount);
            this.Money += payment;
            Console.Error.WriteLine($"Captain unloaded {loadedAmount} {loadedProduct} cargo at {contract.Destination} for {payment} cash, in bank {this.Money}");
            //Console.Error.WriteLine($"Target station now has {contract.Destination.Food} Food, {contract.Destination.Money} Money");
            loadedAmount = 0;
        }
    }
}

