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
        static void Main(string[] args)
        {
            var random = new Random(42);
            Point RandomPoint() => new Point(random.Next(0, 100), random.Next(0, 100));

            var stations = new List<Station>
            {
                // Raw
                new Mine(RandomPoint()),
                new Collector(RandomPoint()),
                new Collector(RandomPoint()),
                new Collector(RandomPoint()),
                new Farm(RandomPoint()),
                new Farm(RandomPoint()),
                new Farm(RandomPoint()),

                // Produced
                new Factory(RandomPoint()),
                new Refinery(RandomPoint()),
                new Refinery(RandomPoint()),
                new Refinery(RandomPoint()),
                new Refinery(RandomPoint()),
                new Shipyard(RandomPoint()),
            };
            var sim = new WorldSim(stations);
            var n = 0;
            Plot.Clear("food");
            Plot.Clear("fuel");
            while (n++ < 1000)
            {
                sim.Run();
                Task.Delay(100).Wait();
            }
        }
    }

    public record Contract(
        Station Destination,
        Product Product,
        int Amount,
        double PricePerItem);

    public class WorldSim
    {
        private List<Station> stations;
        private List<Captain> captains;

        public WorldSim(List<Station> stations)
        {
            this.stations = stations;
            this.captains = new List<Captain>
            {
                new Captain(new Point(0, 0), 1000, 1000),
            };
        }

        const int maxAmount = 10000;
        public void Run()
        {
            var contracts = new List<Contract>();
            // Loop through all stations and generate contracts
            foreach (var station in stations)
            {
                // Food contracts
                if (station is not Farm && station.Food < maxAmount)
                {
                    var amount = maxAmount - station.Food;
                    var price = Station.LogPrice(station.Food, maxAmount);
                    var contract = new Contract(station, Product.Food, amount, price);
                    contracts.Add(contract);
                    Console.Error.WriteLine($"Contract created {contract}");
                }

                if (station is not Refinery && station.Fuel < maxAmount)
                {
                    var contract = new Contract(station, Product.Fuel, maxAmount - station.Fuel, Station.LogPrice(station.Fuel, maxAmount));
                    Console.Error.WriteLine($"Contract created {contract}");
                    contracts.Add(contract);
                }

                Contract? contract2 = null;
                switch (station)
                {
                    case Factory {Metal: < maxAmount} factory:
                        contract2 = new Contract(factory, Product.Metal, maxAmount - factory.Metal, Station.LogPrice(factory.Metal, maxAmount));
                        contracts.Add(contract2);
                        break;
                    case Refinery {Gas: < maxAmount} refinery:
                        contract2 = new Contract(refinery, Product.Gas, maxAmount - refinery.Gas, Station.LogPrice(refinery.Gas, maxAmount));
                        contracts.Add(contract2);
                        break;
                    case Shipyard {Parts: < maxAmount} shipyard:
                        contract2 = new Contract(shipyard, Product.Part, maxAmount - shipyard.Parts, Station.LogPrice(shipyard.Parts, maxAmount));
                        contracts.Add(contract2);
                        break;
                }

                if (contract2 != null)
                {
                    Console.Error.WriteLine($"Contract created {contract2}");
                }
            }

            var averageFuelPrice = stations
                .Select(s => Station.LogPrice(s.Fuel, 100))
                .Average();
            var totalAvailableFuel = stations.Select(s => s.Fuel).Sum();

            // each captain
            foreach (var captain in captains)
            {
                var maxCargo = 1000000;
                // Find most profitable route
                var stationToBuyAverageFuel = stations
                    .Where(s =>
                        Station.LogPrice(s.Fuel, 100) <= averageFuelPrice)
                    .OrderByDescending(s => captain.Position.Distance(s.Position))
                    .First();

                var distanceToBuyAverageFuel = stationToBuyAverageFuel.Position.Distance(captain.Position);

                // Emergency refuel
                if (captain.Fuel / 2 <= distanceToBuyAverageFuel
                    && stationToBuyAverageFuel.Fuel > distanceToBuyAverageFuel)
                {
                    captain.GoTo(stationToBuyAverageFuel);
                    captain.Refuel(stationToBuyAverageFuel);
                }
                var foundRoute = contracts
                    .SelectMany(contract => stations
                        .Where(s => contract.Product == s.GetOutputProduct())
                        .Select(p => new {
                            available = Math.Min(Math.Min(contract.Amount, p.GetAvailableOutput()), maxCargo),
                            position = p.Position,
                            contract,
                            producer = p,
                            distanceTotal = captain.Position.Distance(p.Position) + p.Position.Distance(contract.Destination.Position),
                        }))
                    .Where(x => x.distanceTotal < captain.Fuel / 2)
                    .OrderByDescending(x =>
                        x.available * x.contract.PricePerItem -
                        (x.distanceTotal + distanceToBuyAverageFuel) * averageFuelPrice)
                    .FirstOrDefault();

                Console.Error.WriteLine($"Chosen {foundRoute}");
                if (foundRoute == null) continue;
                var contract = foundRoute.contract;
                var producer = foundRoute.producer;

                var amountLoaded = foundRoute.available;
                var distance = captain.Position.Distance(producer.Position)
                               + producer.Position.Distance(contract.Destination.Position);
                var fuelPrice = Station.LogPrice(producer.Fuel, 100);
                var fuelCost = distance * fuelPrice;
                var contractPay = amountLoaded * contract.PricePerItem;
                if (fuelCost > contractPay)
                {
                    Console.Error.WriteLine($"Not worth burning {fuelCost} of fuel for {contractPay} pay to pickup {contract.Product}");
                    continue;
                }
                else
                {
                    Console.Error.WriteLine($"It is worth burning {fuelCost} fuel for {contractPay} pay to pickup {contract.Product}");
                }

                contracts.Remove(contract);
                if (contract.Destination.Position != captain.Position)
                {
                    // Go to producer
                    captain.GoTo(producer);
                    // Refuel
                    if (producer.Fuel > 0)
                    {
                        captain.Refuel(producer);
                    }
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
                Console.WriteLine($"Station {station} Food {station.Food} Fuel {station.Fuel}, Output: {station.GetAvailableOutput()}, Input: {station.GetAvailableInput()}, Cash: {station.Cash}");
            }
            foreach (var captain in captains)
            {
                Console.WriteLine($"Captain {captain} has {captain.Fuel} Fuel, {captain.Cash} Cash");
            }
            Console.WriteLine($"Average fuel cost is {averageFuelPrice}, Total fuel available {totalAvailableFuel}");
        }
    }

    public class Captain
    {
        public Point Position;
        public double Cash;
        public double Fuel;
        private int maxFuel = 1000;
        private int loadedCargo = 0;
        private Product loadedProduct;

        public Captain(Point position, int cash, int fuel)
        {
            this.Position = position;
            this.Cash = cash;
            this.Fuel = fuel;
        }


        public void GoTo(Station st)
        {
            var n = this.Position.Distance(st.Position);
            this.Position = st.Position;
            this.Fuel -= n;
            Console.Error.WriteLine($"Captain went to {st} costing {n} fuel, remaining {this.Fuel}");
        }

        public void Refuel(Station station)
        {
            var want = maxFuel - Fuel;
            var price = Station.LogPrice(station.Fuel, 100);
            var bought = station.BuyFuel((int) Math.Ceiling(want));
            this.Cash -= bought * price;
            station.Cash += bought * price;
            Console.Error.WriteLine($"Captain refuelled {bought} Fuel for {bought * price} cash, in bank {this.Cash}");
            this.Fuel += bought;
        }

        public void Load(Station producer, int contractAmount)
        {
            var available = producer.Load(contractAmount);
            loadedCargo = available;
            loadedProduct = producer switch
            {
                Collector collector => Product.Gas,
                Factory factory => Product.Part,
                Farm farm => Product.Food,
                Mine mine => Product.Metal,
                Refinery refinery => Product.Fuel,
                Shipyard shipyard => Product.Ship,
                _ => throw new ArgumentOutOfRangeException(nameof(producer), producer, null)
            };
            Console.Error.WriteLine($"Captain loaded {available} {loadedProduct} cargo");
        }
        
        public void Unload(Contract contract)
        {
            contract.Destination.Unload(loadedProduct, loadedCargo);
            var n = contract.PricePerItem * loadedCargo;
            this.Cash += n;
            contract.Destination.Cash -= n;
            Console.Error.WriteLine($"Captain unloaded {loadedCargo} {loadedProduct} cargo at {contract.Destination} for {n} cash, in bank {Cash}");
            Console.Error.WriteLine($"Target station now has {contract.Destination.Food} {contract.Destination.Fuel}");
            loadedCargo = 0;
        }
    }
}

