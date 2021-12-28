// <copyright file="Station.cs" company="HARK">
// Copyright (c) K. All rights reserved.
// </copyright>

namespace WorldSim
{
    using System;
    using System.Drawing;

    public enum Product
    {
        Metal,
        Gas,
        Food,
        Fuel,
        Part,
        Ship,
    }

    public abstract class Station
    {
        public Point Position { get; }
        public int Food { get; set; }
        public int Fuel { get; set; }
        public double Cash { get; set; }
        public abstract Product GetOutputProduct();

        public Station(Point position)
        {
            this.Position = position;
        }

        public abstract void Step();

        // min price 1
        // max price 100
        // amount: 0 = price 100
        // amount 1 = price 90
        // amount 2 = price 80
        // amount 5 = price 50
        // amount 20 = price 10
        public static double LogPrice(int amountAvailable, int maxAmount)
        {
            return 1 * (1 / Math.Log((double)amountAvailable / maxAmount + 1.01));
            var r =  maxAmount / Math.Log(Math.Min(amountAvailable, 2));
            Console.Error.WriteLine($"Calculating price: {amountAvailable} available, {maxAmount} max, = {r}");
            return r;
        }


        public abstract int Load(int contractAmount);

        public virtual void Unload(Product product, int loadedCargo)
        {
            switch (product)
            {
                case Product.Food:
                    Food += loadedCargo;
                    break;
                case Product.Fuel:
                    Fuel += loadedCargo;
                    break;
            }
        }


        public int BuyFuel(int want)
        {
            var bought = Math.Min(want, Fuel);
            Fuel -= bought;
            return bought;
        }

        public int GetAvailableOutput() => this switch
        {
            Collector collector => collector.Gas,
            Factory factory => factory.Parts,
            Farm farm => farm.Food,
            Mine mine => mine.Metal,
            Refinery refinery => refinery.Fuel,
            Shipyard shipyard => shipyard.Ships,
            _ => throw new ArgumentOutOfRangeException()
        };

        public int? GetAvailableInput() => this switch
        {
            Factory factory => factory.Metal,
            Refinery refinery => refinery.Gas,
            Shipyard shipyard => shipyard.Parts,
            _ => null
        };
    }

    public class Mine : Station
    {

        public int Metal { get; set; }
        public override Product GetOutputProduct() => Product.Metal;

        public Mine(Point position) : base(position)
        {
        }


        public override void Step()
        {
            if (Food > 0)
            {
                Food -= 1;
                Metal += 10;
            }
        }

        public override int Load(int contractAmount)
        {
            var bought = Math.Min(contractAmount, Metal);
            Metal -= bought;
            return bought;
        }
    }

    public class Collector : Station
    {
        public int Gas { get; set; }
        public override Product GetOutputProduct() => Product.Gas;

        public override void Step()
        {
            if (Food > 0)
            {
                Food--;
                Gas += 10;
            }
        }

        /// <inheritdoc />
        public override int Load(int contractAmount)
        {
            var bought = Math.Min(contractAmount, Gas);
            Gas -= bought;
            return bought;
        }

        public Collector(Point position) : base(position)
        {
        }
    }

    public class Farm : Station
    {
        public override Product GetOutputProduct() => Product.Food;

        public override void Step()
        {
            Food += 10;
        }

        /// <inheritdoc />
        public override int Load(int contractAmount)
        {
            var bought = Math.Min(contractAmount, Food);
            Food -= bought;
            return bought;
        }

        /// <inheritdoc />
        public Farm(Point position) : base(position)
        {
        }
    }

    public class Factory : Station
    {
        public int Metal { get; set; }
        public int Parts { get; set; }
        public override Product GetOutputProduct() => Product.Part;


        public override void Step()
        {
            if (Food > 0 && Metal > 0)
            {
                Food--;
                Metal--;
                Parts += 10;
            }
        }

        /// <inheritdoc />
        public override int Load(int contractAmount)
        {
            var bought = Math.Min(contractAmount, Parts);
            Parts -= bought;
            return bought;
        }

        /// <inheritdoc />
        public override void Unload(Product product, int loadedCargo)
        {
            switch (product)
            {
                case Product.Metal:
                    Metal += loadedCargo;
                    break;
            }
            base.Unload(product, loadedCargo);
        }

        /// <inheritdoc />
        public Factory(Point position) : base(position)
        {
        }
    }

    public class Refinery : Station
    {
        public int Gas { get; set; }
        public override Product GetOutputProduct() => Product.Fuel;


        public override void Step()
        {
            if (Food > 0 && Gas > 0)
            {
                Food--;
                Gas--;
                Fuel += 10;
            }
        }

        /// <inheritdoc />
        public Refinery(Point position) : base(position)
        {
        }

        public override int Load(int contractAmount)
        {
            var bought = Math.Min(contractAmount, Fuel);
            Fuel -= bought;
            return bought;
        }

        /// <inheritdoc />
        public override void Unload(Product product, int loadedCargo)
        {
            switch (product)
            {
                case Product.Gas:
                    Gas += loadedCargo;
                    break;
            }
            base.Unload(product, loadedCargo);
        }
    }

    public class Shipyard : Station
    {
        public int Ships { get; set; }
        public int Parts { get; set; }
        public override Product GetOutputProduct() => Product.Ship;

        public override void Step()
        {
            if (Food > 0 && Parts > 100)
            {
                Food--;
                Parts -= 100;
                Ships++;
            }
        }

        /// <inheritdoc />
        public Shipyard(Point position) : base(position)
        {
        }

        public override int Load(int contractAmount)
        {
            var bought = Math.Min(contractAmount, Ships);
            Ships -= bought;
            return bought;
        }

        /// <inheritdoc />
        public override void Unload(Product product, int loadedCargo)
        {
            switch (product)
            {
                case Product.Part:
                    Parts += loadedCargo;
                    break;
            }
            base.Unload(product, loadedCargo);
        }
    }
}
