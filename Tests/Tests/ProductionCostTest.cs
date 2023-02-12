// <copyright file="ProductionCostTest.cs" company="HARK">
// Copyright (c) HARK. All rights reserved.
// </copyright>

namespace Tests;

using System.Collections.Generic;
using System.Drawing;
using WorldSim;
using Xunit;

public class ProductionCostTest
{
    [Fact]
    public void Cost()
    {
        var collector = new Station
        {
            Production = new Production(Product.Food.Many(5), Product.Gas.Many(3))
        };
        var farm = new Station
        {
            Production = new Production(Product.Fuel, Product.Food.Many(2)),
        };
        var refinery = new Station
        {
            Production = new Production(new Ratio(Product.Food, Product.Gas), Product.Fuel.Many(3))
        };
        var stations = new List<Station> { collector, farm, refinery };
        var c = new Captain("A", new Point(0, 0), 10);
        var cost = c.CostOfAinB(stations, Product.Fuel.Many(1), Product.Fuel);

        // Expected ratio:
        // 2 Fuel <- 1 Food
        //              <- 2 Food <- 1 Fuel = 0.5 Fuel
        //           1 Gas
        //              <- 1 Gas <- 1 Food = 0.5
        // 2 Fuel = 1 Fuel
        Assert.Equal(0.44m, cost, 2);
    }
}
