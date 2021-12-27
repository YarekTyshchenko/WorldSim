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
            File.AppendAllLines(file, new List<string> {$"{amount}" });
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var stations = new List<Station>
            {
                new Station(new Point(43, 66), Producer: true),
                new Station(new Point(342, 35), Consumer: true),
            };
            var sim = new WorldSim(stations);
            var n = 0;
            Plot.Clear("food");
            Plot.Clear("fuel");
            while (n++ < 100)
            {
                sim.Run();
                //Task.Delay(100).Wait();
            }
        }
    }

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

        public void Run()
        {
            var maxFood = 100;
            var contracts = new List<Contract>();
            // Loop through all stations and generate contracts
            foreach (var station in stations)
            {
                // Do we need to create a contract?
                if (station.Food < maxFood)
                {
                    var price = station.CalculatePrice(station.Food, maxFood, 1, 100);
                    var contract = new Contract(station, maxFood - station.Food, price);
                    contracts.Add(contract);
                    Console.Error.WriteLine($"Contract created {contract}");
                }
            }
            // each captain
            foreach (var captain in captains)
            {
                // First a contract
                var contract = contracts.OrderByDescending(s => s.PricePerItem).FirstOrDefault();
                if (contract == null) continue;

                var producer = stations.Find(s => s.Producer);
                if (producer == null) continue;

                // Is it worth me doing this contract?
                var amountLoaded = contract.Amount >= 10
                    ? 10
                    : contract.Amount;
                var distance = captain.Position.Distance(producer.Position)
                    + producer.Position.Distance(contract.Location.Position);
                var fuelPrice = contract.Location.GetFuelPrice();
                var fuelCost = distance * fuelPrice;
                var contractPay = amountLoaded * contract.PricePerItem;
                if (fuelCost > contractPay)
                {
                    Console.Error.WriteLine($"Not worth burning {fuelCost} of fuel for {contractPay} pay");
                    continue;
                }
                else
                {
                    Console.Error.WriteLine($"It is worth burning {fuelCost} fuel for {contractPay} pay");
                }

                // Go to producer
                contracts.Remove(contract);
                captain.GoTo(producer.Position);
                captain.Load(producer, amountLoaded);
                // Refuel
                //captain.Refuel(producer);
                // go to consumer
                captain.GoTo(contract.Location.Position);
                // Sell to consumer
                captain.Unload(contract);
                // Refuel
                captain.Refuel(contract.Location);
            }

            // All stations consume their food
            foreach (var station in stations)
            {
                // Turn Food into Fuel
                if (station.Food > 0 && station.Fuel < 1000 && station.Producer)
                {
                    station.Food -= 1;
                    station.Fuel += 1;
                    Console.Error.WriteLine($"Station has {station.Food} Food and {station.Fuel} Fuel");
                }
                Plot.Food(station.Food);
                Plot.Line("fuel", station.Fuel);
            }
        }
    }

    public record Contract(
        Station Location,
        int Amount,
        double PricePerItem);

    public class Station
    {
        public Point Position { get; }
        public bool Producer { get; }
        public bool Consumer { get; }

        public int Fuel = 10;
        public int Food = 10;

        public Station(
            Point Position,
            bool Producer = false,
            bool Consumer = false)
        {
            this.Position = Position;
            this.Producer = Producer;
            this.Consumer = Consumer;
        }

        public int BuyFuel(int amount)
        {
            Fuel -= amount;
            return amount;
        }

        public double Unload(int amount, double contractPricePerItem)
        {
            Food += amount;
            return amount * contractPricePerItem;
        }

        public double GetFuelPrice()
        {
            return CalculatePrice(Fuel, 1000, 1, 10);
        }

        public double CalculatePrice(int amount, int maxAmount, int minPrice, int maxPrice)
        {
            var adj = (double) (maxAmount - amount) / maxAmount;
            return (maxPrice - minPrice) * adj + minPrice;
        }
    }

    public class Captain
    {
        public Point Position;
        private double cash;
        private int fuel;
        private int maxFuel = 1000;
        private int loadedCargo = 0;

        public Captain(Point position, int cash, int fuel)
        {
            this.Position = position;
            this.cash = cash;
            this.fuel = fuel;
        }


        public void GoTo(Point pos)
        {
            var n = this.Position.Distance(pos);
            this.Position = pos;
            this.fuel -= (int) Math.Floor(n);
            Console.Error.WriteLine($"Captain went to position {pos} costing {n} fuel, remaining {this.fuel}");
        }

        public void Refuel(Station station)
        {
            var want = maxFuel - fuel;
            var price = station.GetFuelPrice();
            var bought = station.BuyFuel(want);
            this.cash -= bought * price;
            Console.Error.WriteLine($"Captain refuelled for {want} cash, in bank {this.cash}");
            this.fuel += bought;
        }

        public void Load(Station producer, int contractAmount)
        {
            loadedCargo = contractAmount;
            Console.Error.WriteLine($"Captain loaded {contractAmount} cargo");
        }

        public void Unload(Contract contract)
        {
            var n = contract.Location.Unload(loadedCargo, contract.PricePerItem);
            this.cash += n;
            Console.Error.WriteLine($"Captain unloaded {loadedCargo} cargo for {n} cash, in bank {cash}");
            loadedCargo = 0;
        }
    }
}
