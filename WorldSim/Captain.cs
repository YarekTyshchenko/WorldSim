namespace WorldSim;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public class Captain
{
    public string Name;
    public Point Position;
    public double Fuel;

    private int maxFuel = 20;
    public int Capacity = 20;
    private int loadedAmount = 0;
    public int idleDays = 0;
    private Product loadedProduct;

    /// <inheritdoc />
    public override string ToString() => Name;

    public Captain(string name, Point position, int fuel)
    {
        this.Name = name;
        this.Position = position;
        this.Fuel = fuel;
    }

    public Portion Loaded => new(loadedProduct, loadedAmount);

    public void GoTo(Station st)
    {
        idleDays = 0;
        var n = this.Position.Distance(st.Position) / 100;
        this.Position = st.Position;
        this.Fuel -= n;
        Console.Error.WriteLine($"Captain {Name} went to {st} costing {n} fuel, remaining {this.Fuel}");
    }

    public void TryRefuel(Station refinery)
    {
        if (refinery.Production.Output.Items.All(x => x.Product != Product.Fuel)) return;

        var want = (int)Math.Floor(maxFuel - Fuel);
        if (want < 1) return;

        var bought = refinery.PickupOutput(Product.Fuel, want);
        Fuel += bought;
        Console.Error.WriteLine($"[{Name}] Refuelled at {refinery} for {bought} fuel");
    }

    public void Load(Station producer, Product product, int contractAmount)
    {
        var available = producer.PickupOutput(product, contractAmount);
        loadedAmount = available;
        loadedProduct = product;

        Console.Error.WriteLine($"Captain {Name} loaded {available} {loadedProduct} cargo");
    }

    public void Unload(Contract contract)
    {
        var delivered = contract.Destination.DeliverInput(loadedProduct, loadedAmount);
        Console.Error.WriteLine($"Captain {Name} unloaded {delivered} {loadedProduct} cargo at {contract.Destination}");
        loadedAmount -= delivered;
        if (loadedAmount > 0)
        {
            Console.Error.WriteLine($"Captain {Name} couldn't unload all cargo, remaining {loadedAmount}");
        }
        // loadedAmount = 0;
    }

    public void Act(List<Contract> contracts,
        List<Station> stations,
        List<Station> refineries,
        Random random)
    {
        var captain = this;
        if (loadedAmount > 0)
        {
            // Try to get rid
            var c = contracts
                .Where(c => c.Product == loadedProduct)
                .Where(c => c.Destination.inputs[c.Product] + loadedAmount < c.Destination.Capacity)
                .OrderBy(c => c.Destination.inputs[c.Product])
                .FirstOrDefault();
            if (c is not null)
            {
                contracts.Remove(c);
                if (c.Destination.Position != captain.Position)
                {
                    // Go to producer
                    captain.GoTo(c.Destination);
                    // Refuel
                    captain.TryRefuel(c.Destination);
                }
                // Sell to consumer
                captain.Unload(c);
            }
            else
            {
                idleDays++;
            }
            return;
        }

        // Find most important route
        var routes = contracts
            .SelectMany(contract => stations
                .Where(producer => producer.Production.Output.Items.Any(x => x.Product == contract.Product))
                .Select(p =>
                {
                    var inputStock = contract.Destination.inputs[contract.Product];
                    var output = p.outputs[contract.Product];
                    var inputSpace = contract.Destination.Capacity - inputStock;
                    return new
                    {
                        contract.Product,
                        available = Math.Min(output, inputSpace),
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
            // .ThenBy(x => x.distanceTotal);

        Console.Error.WriteLine($@"[{Name}] Available Routes: {routes.Select(x => new {
            x.available,
            x.Product,
            x.producer,
            x.importance,
            x.distanceTotal
        }).Join()}");

        // Pick the first one
        // var foundRoute = routes.FirstOrDefault();
        // Pick a random first one
        var foundRoute = routes.Where(x => x.available == routes.FirstOrDefault().available).RandomElement(random);
        Console.Error.WriteLine($"[{Name}] Chosen {foundRoute}");
        if (foundRoute == null)
        {
            idleDays++;
            return;
        }

        var contract = foundRoute.contract;
        var producer = foundRoute.producer;

        var amountLoaded = Math.Min(foundRoute.available, Capacity);
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
            // TryRefuel if they have it
            captain.TryRefuel(producer);
        }
        captain.Load(producer, contract.Product, amountLoaded);

        // go to consumer
        captain.GoTo(contract.Destination);
        // Sell to consumer
        captain.Unload(contract);
        // Refuel
        captain.TryRefuel(contract.Destination);

        var nearestFuel = stations
            .Where(s => s.outputs[Product.Fuel] > 0)
            .OrderByDescending(x => x.Position.Distance(captain.Position))
            .FirstOrDefault();

        if (captain.Fuel < maxFuel / 4 && nearestFuel != null)
        {
            if (captain.Position != nearestFuel.Position)
            {
                captain.GoTo(nearestFuel);
            }
            captain.TryRefuel(nearestFuel);
        }
    }
}
