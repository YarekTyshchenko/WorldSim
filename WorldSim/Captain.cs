namespace WorldSim;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public class Captain
{
    private readonly List<Station> stations;
    public string Name;
    public Point Position;
    public decimal Fuel;

    private int maxFuel = 20;
    public int Capacity = 20;
    private int loadedAmount = 0;
    public int idleDays = 0;
    private Product loadedProduct;
    public Wallet Wallet = new();

    /// <inheritdoc />
    public override string ToString() => Name;

    public Captain(List<Station> stations, string name, Point position, int fuel)
    {
        this.stations = stations;
        this.Name = name;
        this.Position = position;
        this.Fuel = fuel;
    }

    public Portion Loaded => new(loadedProduct, loadedAmount);

    public decimal FuelCost(double distance) => (decimal)distance / 100;

    public void GoTo(Station st)
    {
        idleDays = 0;
        var n = FuelCost(this.Position.Distance(st.Position));
        this.Position = st.Position;
        this.Fuel -= n;
        Console.Error.WriteLine($"Captain {Name} went to {st} costing {n} fuel, remaining {this.Fuel}");
    }

    public int TryRefuel(
        Station refinery,
        List<Station> stations)
    {
        if (refinery.Production.Output.Items.All(x => x.Product != Product.Fuel)) return 0;

        var want = (int)Math.Floor(maxFuel - Fuel);
        if (want < 1) return 0;

        var bought = refinery.PickupOutput(Product.Fuel, want);
        Fuel += bought;
        Console.Error.WriteLine($"[{Name}] Refuelled at {refinery} for {bought} fuel");
        var costOfFuel = CostOfAinB(stations, new Portion(Product.Fuel, bought), Product.Fuel);
        Wallet.Pay(refinery.Wallet, Product.Fuel, costOfFuel * bought);
        return bought;
    }

    public int Load(Station producer, Product product, int contractAmount)
    {
        var available = producer.PickupOutput(product, contractAmount);
        loadedAmount = available;
        loadedProduct = product;
        Wallet.Pay(producer.Wallet, product, available);

        Console.Error.WriteLine($"Captain {Name} loaded {available} {loadedProduct} cargo");
        return loadedAmount;
    }

    public int Unload(Contract contract)
    {
        var delivered = contract.Destination.DeliverInput(loadedProduct, loadedAmount);
        //contract.Destination.Wallet.Pay(Wallet, new Portion(loadedProduct, delivered))
        var paidPortion = contract.PayPerUnit with { Count = contract.PayPerUnit.Count * loadedAmount };
        contract.Destination.Wallet.Pay(Wallet, contract.PayPerUnit.Product, contract.PayPerUnit.Count * loadedAmount);
        Console.Error.WriteLine($"Captain {Name} unloaded {delivered} {loadedProduct} cargo at {contract.Destination} and got paid {paidPortion}");
        loadedAmount -= delivered;
        if (loadedAmount > 0)
        {
            Console.Error.WriteLine($"Captain {Name} couldn't unload all cargo, remaining {loadedAmount}");
        }
        // loadedAmount = 0;
        return delivered;
    }

    public decimal CostOfAinB(List<Station> stations, Portion thingToCalculate, Product inThisProduct)
    {
        var producer = stations.FirstOrDefault(x => x.Production.Output.Items.Any(p => p.Product == thingToCalculate.Product))!;

        var portionOfOutput = producer.Production.Output.Items.FirstOrDefault(x => x.Product == thingToCalculate.Product)!.Count;

        // Look at all inputs, and estimate their value recursively
        var countOfAllInputs = producer.Production.Input.Items.Sum(input =>
        {
            if (input.Product == inThisProduct)
            {
                return (decimal)input.Count;
            }

            return CostOfAinB(stations, input, inThisProduct);
        });
        return countOfAllInputs / portionOfOutput * thingToCalculate.Count;
    }

    public void Act(
        List<Contract> contracts,
        List<Station> stations,
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
                    captain.TryRefuel(c.Destination, stations);
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
                    var distanceTotal = captain.Position.Distance(p.Position) +
                                        p.Position.Distance(contract.Destination.Position);
                    var inputStock = contract.Destination.inputs[contract.Product];
                    var output = p.outputs[contract.Product];
                    var inputSpace = contract.Destination.Capacity - inputStock;
                    var available = Math.Min(output, inputSpace);
                    var inputCost = CostOfAinB(stations, new Portion(contract.Product, available), Product.Fuel);
                    var fuelCost = FuelCost(distanceTotal);
                    var pay = CostOfAinB(stations, contract.PayPerUnit, Product.Fuel) * available;
                    return new
                    {
                        inputCost,
                        fuelCost,
                        pay,
                        contract.Product,
                        available,
                        position = p.Position,
                        contract,
                        producer = p,
                        distanceTotal = distanceTotal,
                        importance = output - inputStock
                    };
                }))
            // Filter out contracts that can't pay
            // .Where(x => x.contract.Destination.Money > x.sellPayout)
            // Filter out negative contracts
            // .Where(x => x.sellPayout - x.buyCost - (decimal)x.distanceTotal * averageFuelPrice > 0)
            // Filter out empty contracts
            .Where(x => x.available > 0)
            //.Where(x => x.importance > 0)
            .Where(x => x.pay > x.inputCost + x.fuelCost)
            .OrderByDescending(x => x.pay - x.inputCost - x.fuelCost);
            //.OrderByDescending(x => x.importance);
            // .ThenBy(x => x.distanceTotal);

        Console.Error.WriteLine($@"[{Name}] Available Routes: ");
        Console.WriteLine(routes.Select(x => new
        {
            item = new Portion(x.Product, x.available),
            from = x.producer,
            to = x.contract.Destination,
            pay = x.pay - x.inputCost - x.fuelCost,
            x.distanceTotal
        }).Join());

        // Pick the first one
        // var foundRoute = routes.FirstOrDefault();
        // Pick a random first one
        var foundRoute = routes.Where(x => x.available == routes.FirstOrDefault().available).RandomElement(random);
        // Console.Error.WriteLine($"[{Name}] Chosen {foundRoute}");
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
            captain.TryRefuel(producer, stations);
        }
        captain.Load(producer, contract.Product, amountLoaded);

        // go to consumer
        captain.GoTo(contract.Destination);
        // Sell to consumer
        captain.Unload(contract);
        // Refuel
        captain.TryRefuel(contract.Destination, stations);

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
            captain.TryRefuel(nearestFuel, stations);
        }
    }
}
