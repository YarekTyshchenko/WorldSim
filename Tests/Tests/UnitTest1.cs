using System;
using Xunit;

namespace Tests
{
    using System.Drawing;
    using System.Globalization;
    using System.Linq;
    using WorldSim;
    using Xunit.Abstractions;

    public class UnitTest1
    {
        private readonly ITestOutputHelper testOutputHelper;

        public UnitTest1(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Test1()
        {
            // var prices = Enumerable.Range(0, 100);
            // foreach (var amount in prices)
            // {
            //     var price = Station.LogPrice(amount, 100);
            //     var text = $"{amount}: {price}";
            //     testOutputHelper.WriteLine(text);
            //     Console.WriteLine(text);
            // }
        }

        [Fact]
        public void Test2()
        {
            var p = new Production(new Ratio(Product.Gas, Product.Food), Product.Fuel.Many(10));
            Console.Error.WriteLine(p.ToString());
            testOutputHelper.WriteLine(p.ToString());
            p = new Production(new Ratio(), Product.Food.Many(10));
            Console.Error.WriteLine(p.ToString());
            testOutputHelper.WriteLine(p.ToString());
        }

        [Fact]
        public void Test3()
        {
            var n = new Station
            {
                Money = 1000,
                Position = new Point(100, 100),
                Production = new Production(new Ratio(Product.Gas, Product.Food), Product.Fuel.Many(10)),
            };
            //n.Production.Output.Items.Select(x => x.Product);
            n.Step();
        }
    }
}
