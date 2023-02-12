using System;
using System.Drawing;
using WorldSim;

namespace WorldSim
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Threading.Tasks;

    class Program
    {
        public static readonly Random Random = new Random(42);

        static void Main(string[] args)
        {
            Station Farm(string name)
            {
                var f = new Station
                {
                    Name = name,
                    Position = RandomPoint(),
                    Production = new Production(Product.Fuel, Product.Food.Many(6)),
                };
                f.inputs[Product.Fuel] = 100;
                return f;
            }

            Station Collector(string name) => new Station
            {
                Name = name,
                Position = RandomPoint(),
                Production = new Production(Product.Food, Product.Gas.Many(3))
            };

            Station Refinery(string name) => new Station
            {
                Name = name,
                Position = RandomPoint(),
                Production = new Production(new Ratio(Product.Food, Product.Gas), Product.Fuel.Many(3)),
            };

            Station Mine(string name) => new Station
            {
                Name = name,
                Position = RandomPoint(),
                Production = new Production(new Ratio(Product.Food, Product.Gas), Product.Metal.Many(1)),
            };

            Station Factory(string name) => new Station
            {
                Name = name,
                Position = RandomPoint(),
                Production = new Production(new Ratio(Product.Food, Product.Metal, Product.Gas), Product.Parts.Many(3)),
            };
            Station Shipyard(string name) => new Station
            {
                Name = name,
                Position = RandomPoint(),
                Production = new Production(new Ratio(Product.Food.Many(10), Product.Parts.Many(10), Product.Gas.Many(10)), Product.Ship.Many(1)),
            };


            var stations = new List<Station>
            {
                // Raw
                Mine("Mine A"),
                Collector("Collector A"),
                // Collector("Collector B"),
                // Collector("Collector C"),
                Farm("Farm A"),

                // Produced
                Factory("Factory A"),
                Refinery("Refinery A"),
                // Refinery("Refinery B"),
                // Refinery("Refinery C"),
                // Refinery("Refinery D"),
                Shipyard("Shipyard A"),
            };
            var sim = new WorldSim(stations);
            var n = 0;
            while (n++ < 1000)
            {
                sim.Run(Random);
                Task.Delay(1000).Wait();
            }
        }

        public static Point RandomPoint() => new Point(Random.Next(0, 100), Random.Next(0, 100));
    }
}

