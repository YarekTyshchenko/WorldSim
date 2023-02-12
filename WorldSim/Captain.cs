namespace WorldSim;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public class Captain
{
    public Point Position;
    public double Fuel;

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
        this.Position = st.Position;
        this.Fuel -= n;
        Console.Error.WriteLine($"Captain went to {st} costing {n} fuel, remaining {this.Fuel}");
    }

    public void Refuel(Station refinery)
    {
        if (refinery.Production.Output.Items.All(x => x.Product != Product.Fuel)) return;

        var want = (int)Math.Floor(1000 - Fuel);
        if (want < 1) return;

        var bought = refinery.PickupOutput(Product.Fuel, want);
        Fuel += bought;
        Console.Error.WriteLine($"Refuelled at {refinery} for {bought} fuel");
    }

    public void Load(Station producer, Product product, int contractAmount)
    {
        var available = producer.PickupOutput(product, contractAmount);
        loadedAmount = available;
        loadedProduct = product;

        Console.Error.WriteLine($"Captain loaded {available} {loadedProduct} cargo");
    }

    public void Unload(Contract contract)
    {
        contract.Destination.DeliverInput(loadedProduct, loadedAmount);
        Console.Error.WriteLine($"Captain unloaded {loadedAmount} {loadedProduct} cargo at {contract.Destination}");
        loadedAmount = 0;
    }

    public void Act(List<Contract> contracts,
        List<Station> stations,
        List<Station> refineries,
        Random random)
    {
        var captain = this;
        captain.idleDays++;
        // Find most profitable route
        var routes = contracts
            .SelectMany(contract => stations
                .Where(producer => producer.Production.Output.Items.Any(x => x.Product == contract.Product))
                .Select(p =>
                {
                    var inputStock = contract.Destination.inputs[contract.Product];
                    var output = p.outputs[contract.Product];
                    return new
                    {
                        available = output,
                        position = p.Position,
                        contract,
                        producer = p,
                        distanceTotal = captain.Position.Distance(p.Position) +
                                        p.Position.Distance(contract.Destination.Position),
                        importance = output - inputStock
                    };
                }))
            // Filter out contracts that can't pay
            // .Where(x => x.contract.Destination.Money > x.sellPayout)
            // Filter out negative contracts
            // .Where(x => x.sellPayout - x.buyCost - (decimal)x.distanceTotal * averageFuelPrice > 0)
            // Filter out empty contracts
            .Where(x => x.available > 0 && x.importance > 0)
            .OrderByDescending(x => x.importance);

        Console.Error.WriteLine($"Available Routes: {routes.Join()}");

        // Pick the first one
        // var foundRoute = routes.FirstOrDefault();
        // Pick a random first one
        var foundRoute = routes.Where(x => x.available == routes.FirstOrDefault().available).RandomElement(random);
        Console.Error.WriteLine($"Chosen {foundRoute}");
        if (foundRoute == null) return;
        var contract = foundRoute.contract;
        var producer = foundRoute.producer;

        var amountLoaded = foundRoute.available;
        // var fuelCost = (decimal)foundRoute.distanceTotal * averageFuelPrice;
        // if (foundRoute.sellPayout - foundRoute.buyCost - fuelCost > 0)
        // {
        //     Console.Error.WriteLine(
        //         $"Route {foundRoute} is good. Fuel cost {fuelCost}");
        // }
        // else
        // {
        //     Console.Error.WriteLine(
        //         $"Not worth {foundRoute}, fuel cost {fuelCost}");
        //     return
        // }

        contracts.Remove(contract);
        if (producer.Position != captain.Position)
        {
            // Go to producer
            captain.GoTo(producer);
            // Refuel
            // captain.Refuel(producer);
        }
        captain.Load(producer, contract.Product, amountLoaded);

        // go to consumer
        captain.GoTo(contract.Destination);
        // Sell to consumer
        captain.Unload(contract);
        // Refuel
        // captain.Refuel(contract.Destination);

        // var nearestFuel = refineries
        //     .Where(s => s.outputs[Product.Fuel] > 0)
        //     .OrderByDescending(x => x.Position.Distance(captain.Position))
        //     .FirstOrDefault();
        //
        // if (captain.Fuel < 1000 / 2 && nearestFuel != null)
        // {
        //     if (captain.Position != nearestFuel.Position)
        //     {
        //         captain.GoTo(nearestFuel);
        //     }
        //
        //     captain.Refuel(nearestFuel);
        // }
    }
}
