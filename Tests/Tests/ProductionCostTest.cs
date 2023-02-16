// <copyright file="ProductionCostTest.cs" company="HARK">
// Copyright (c) HARK. All rights reserved.
// </copyright>

namespace Tests;

using System.Collections.Generic;
using System.Linq;
using WorldSim;
using Xunit;

public class ProductionCostTest
{
    private readonly List<Station> stations = MakeStations(
        new Production(Product.Food, Product.Gas.Many(10)),
        new Production(Product.Fuel, Product.Food.Many(10)),
        new Production(new Ratio(Product.Food, Product.Gas), Product.Fuel.Many(10)),
        new Production(new Ratio(Product.Food, Product.Gas), Product.Metal.Many(2)),
        new Production(new Ratio(Product.Food, Product.Metal, Product.Gas), Product.Parts.Many(3)),
        new Production(new Ratio(Product.Food.Many(20), Product.Parts.Many(5), Product.Gas.Many(5)), Product.Ship.Many(1)));

    [Theory]
    [InlineData(Product.Fuel, 0.011)]
    [InlineData(Product.Food, 0.1)]
    [InlineData(Product.Gas, 0.01)]
    [InlineData(Product.Metal, 0.055)]
    [InlineData(Product.Parts, 0.055)]
    //[InlineData(Product.Ship, 1.650)]
    public void Cost(Product product, decimal expectedCost)
    {
        var cost = stations.CostToMake(product);

        // 1 Fuel is made from 0.1 Food + 0.1 Gas
        // Cost of 1 Food = 0.1 Fuel
        // Cost of 1 Gas = 0.1 Food = 0.01 Fuel
        // Total = 0.11
        Assert.Equal(expectedCost, cost, 3);
    }

    [Fact]
    public void Cost2()
    {
        // Refinery is selling Fuel at 1
        // To make 1 Fuel it uses 0.1 Food + 0.1 Gas
        // That means that 1 Food costs 5 Fuel, 1 Gas costs 5 Fuel
        // Maximum it should pay is 5 Fuel
        // Yet, 1 Gas costs 0.01 Fuel
        // Assert.Equal(5, stations.MaxPrice(Product.Gas));
    }

    [Fact]
    public void CostInOutput()
    {
        // var r = stations.CostInOutput(Product.Food);
        // Assert.Equal(new Ratio());
    }

    [Fact]
    public void BarterCollector()
    {
        // Collector
        // 1 Food makes 10 Gas
        // To sustain, Collector can sell 10 Gas for 1 Food (and 2 Gas for 0.2 Food)
        var barterFor = stations[0].Buy(Product.Gas.Many(2));
        Assert.Collection(barterFor.Items, x =>
        {
            Assert.Equal(Product.Food, x.Product);
            Assert.Equal(0.2m, x.Count, 3);
        });
    }
    [Fact]
    public void BarterRefinery()
    {
        // 1 Food + 1 Gas makes 10 Fuel
        // To sustain, it can sell 1 Fuel for .1 Food + 0.1 Gas
        var barterFor = stations[2].Buy(Product.Fuel.Many(1));
        Assert.Collection(
            barterFor.Items,
            x =>
            {
                Assert.Equal(Product.Food, x.Product);
                Assert.Equal(0.1m, x.Count, 3);
            },
            x =>
            {
                Assert.Equal(Product.Gas, x.Product);
                Assert.Equal(0.1m, x.Count, 3);
            });
    }
    [Fact]
    public void BarterShipyard()
    {
        // 20 Food + 5 Gas + 5 Pars makes 1 Ship
        // 20 Food is 0.66 of Ship
        var barterFor = stations[5].Buy(Product.Ship.Many(2));
        Assert.Collection(
            barterFor.Items,
            x =>
            {
                Assert.Equal(Product.Food, x.Product);
                Assert.Equal(40, x.Count, 3);
            },
            x =>
            {
                Assert.Equal(Product.Parts, x.Product);
                Assert.Equal(10, x.Count, 3);
            },
            x =>
            {
                Assert.Equal(Product.Gas, x.Product);
                Assert.Equal(10, x.Count, 3);
            });
    }

    [Fact]
    public void CanMakeFuel()
    {
        var stations = MakeStations(new Production(Product.Fuel, Product.Food.Many(10)));
        var cost = stations.CostToMake(Product.Food);

        Assert.Equal(0.1m, cost, 2);
    }

    [Fact]
    public void MaxPrice()
    {
        var stations = MakeStations(new Production(Product.Fuel, Product.Food.Many(10)));
        var cost = stations.CostToMake(Product.Food);

        // 1 Food is made from 0.1 Fuel
        // What is the max price to charge for selling Food
        // Minimum is 0.1 Fuel, as that's how much it cost to produce
        // Is there a max? In loop of Fuel to make more fuel, max price is cost of output fuel
        // I will pay 10 Food for 1 Fuel max
        // I will pay 1 Food for 1 Fuel? At what point the price is stupid?
        Assert.Equal(0.1m, cost, 2);
    }

    private static List<Station> MakeStations(params Production[] production) =>
        production.Select(p => new Station
        {
            Production = p
        }).ToList();
}
