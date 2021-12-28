using System;
using Xunit;

namespace Tests
{
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
            var prices = Enumerable.Range(0, 100);
            foreach (var amount in prices)
            {
                var price = Station.LogPrice(amount, 100);
                var text = $"{amount}: {price}";
                testOutputHelper.WriteLine(text);
                Console.WriteLine(text);
            }
        }
    }
}
