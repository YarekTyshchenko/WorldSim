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
                    Production = new Production(Product.Fuel, Product.Food.Many(3)),
                };
                f.inputs[Product.Fuel] = 1000;
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

            var stations = new List<Station>
            {
                // Raw
                // new Mine(RandomPoint()),
                Collector("Collector A"),
                Collector("Collector B"),
                Collector("Collector C"),
                Farm("Farm A"),

                // Produced
                // new Factory(RandomPoint()),
                Refinery("Refinery A"),
                Refinery("Refinery B"),
                Refinery("Refinery C"),
                Refinery("Refinery D"),
                // new Shipyard(RandomPoint()),
            };
            var sim = new WorldSim(stations);
            var n = 0;
            while (n++ < 10000)
            {
                sim.Run(Random);
                Task.Delay(100).Wait();
            }
        }

        public static Point RandomPoint() => new Point(Random.Next(0, 100), Random.Next(0, 100));
    }
}

