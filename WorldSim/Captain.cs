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
    private decimal loadedAmount = 0;
    public int idleDays = 0;
    private Product loadedProduct;
    public Wallet Wallet;
    public decimal Money = 0;

    /// <inheritdoc />
    public override string ToString() => Name;

    public Captain(List<Station> stations, string name, Point position, int fuel, Wallet wallet)
    {
        this.stations = stations;
        this.Name = name;
        this.Position = position;
        this.Fuel = fuel;
        this.Wallet = wallet;
    }

    public Portion Loaded => new(loadedProduct, loadedAmount);

    public decimal FuelCost(double distance) => 0; //(decimal)distance / 100;

    public void GoTo(Station st)
    {
        idleDays = 0;
        var n = FuelCost(this.Position.Distance(st.Position));
        this.Position = st.Position;
        this.Fuel -= n;
        Console.Error.WriteLine($"Captain {Name} went to {st} costing {n} fuel, remaining {this.Fuel}");
    }

    public decimal TryRefuel(
        Station refinery,
        List<Station> stations)
    {
        if (refinery.Production.Output.Items.All(x => x.Product != Product.Fuel)) return 0;

        var want = (int)Math.Floor(maxFuel - Fuel);
        if (want < 1) return 0;

        var bought = refinery.PickupOutput(Product.Fuel, want);
        Fuel += bought;
        Console.Error.WriteLine($"[{Name}] Refuelled at {refinery} for {bought} fuel");
        // var costOfFuel = stations.CostOfAinB(new Portion(Product.Fuel, bought), Product.Fuel);
        // Wallet.Pay(refinery.Wallet, Product.Fuel, costOfFuel * bought);
        return bought;
    }

    public decimal Load(Station producer, Product product, decimal contractAmount)
    {
        var available = producer.PickupOutput(product, contractAmount);
        loadedAmount = available;
        loadedProduct = product;
        var barterList = producer.Buy(product.Many(available));
        foreach (var (product1, count) in barterList.Items)
        {
            Wallet.Pay(producer.Wallet, product1, count);
        }

        producer.ReBalanceWallet();

        Console.Error.WriteLine($"Captain {Name} loaded {available} {loadedProduct} cargo, bartered for {barterList}");
        return loadedAmount;
    }

    public decimal Unload(Contract contract)
    {
        var delivered = contract.Destination.DeliverInput(loadedProduct, loadedAmount);
        //contract.Destination.Wallet.Pay(Wallet, new Portion(loadedProduct, delivered))
        var barterRatio = contract.BarterFor.Scale(loadedAmount);
        foreach (var (product, count) in barterRatio.Items)
        {
            contract.Destination.Wallet.Pay(Wallet, product, count);
        }
        contract.Destination.ReBalanceWallet();
        Console.Error.WriteLine($"Captain {Name} unloaded {delivered} {loadedProduct} cargo at {contract.Destination} and bartered for {barterRatio}");
        loadedAmount -= delivered;
        if (loadedAmount > 0)
        {
            Console.Error.WriteLine($"Captain {Name} couldn't unload all cargo, remaining {loadedAmount}");
        }
        // loadedAmount = 0;
        return delivered;
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
                    var outputStock = p.outputs[contract.Product];
                    var inputSpace = contract.Destination.Capacity - inputStock;
                    var available = Math.Min(Math.Min(outputStock, inputSpace), this.Capacity);
                    var currentValue = captain.Wallet.wallet.Values;
                    var inputCost = p.Buy(contract.Product.Many(available));
                    var afterPay = captain.Wallet
                        .Try(inputCost)
                        .Try(contract.BarterFor.Scale(available))
                        .wallet.Values;
                    // var inputCost = stations.CostToMake(contract.Product.Many(available));
                    // var fuelCost = FuelCost(distanceTotal);

                    // Create a metric that sums all the things in the wallet, and prefers a route
                    // that will leave it more SUM positive than it started
                    return new
                    {
                        //inputCost,
                        //fuelCost,
                        //pay = 1 * available,
                        contract.Product,
                        available,
                        position = p.Position,
                        contract,
                        producer = p,
                        distanceTotal = distanceTotal,
                        importance = outputStock - inputStock,
                        profit = afterPay.Sum() - currentValue.Sum(),
                        lowest = afterPay.Min()
                    };
                }))
            // Filter out contracts that can't pay
            // .Where(x => x.contract.Destination.Money > x.sellPayout)
            // Filter out negative contracts
            // .Where(x => x.sellPayout - x.buyCost - (decimal)x.distanceTotal * averageFuelPrice > 0)
            // Filter out empty contracts
            .Where(x => x.available > 0)
            .Where(x => x.lowest > 0)
            //.Where(x => x.importance > 0)
            // .Where(x => x.pay > x.inputCost + x.fuelCost)
            //.OrderByDescending(x => x.pay - x.inputCost - x.fuelCost);
            //.OrderByDescending(x => x.importance);
            //.OrderByDescending(x => x.available);
            .OrderByDescending(x => x.profit)
            .ThenByDescending(x => x.lowest)
            .ThenByDescending(x => x.importance);
        // .ThenBy(x => x.distanceTotal);

        Console.Error.WriteLine($@"[{Name}] Available Routes: ");
        Console.WriteLine(routes
            .Select(x => new
            {
                item = new Portion(x.Product, x.available),
                from = x.producer,
                to = x.contract.Destination,
                x.profit,
                x.importance,
                x.lowest,
                //pay = x.pay - x.inputCost - x.fuelCost,
                //x.fuelCost,
                //x.inputCost
            })
            //.OrderByDescending(x => x.inputCost)
            .Join());

        // Pick the first one
        var foundRoute = routes.FirstOrDefault();
        // Pick a random first one
        // var foundRoute = routes
        //     //.Where(x => x.pay > x.inputCost + x.fuelCost)
        //     .Where(x => x.available == routes.FirstOrDefault().available).RandomElement(random);
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

        // Accept
        contracts.Remove(contract);
        // Pay, half now, half when cargo is delivered?
        if (producer.Position != captain.Position)
        {
            // Go to producer
            captain.GoTo(producer);
            // TryRefuel if they have it
            //captain.TryRefuel(producer, stations);
        }
        captain.Load(producer, contract.Product, amountLoaded);

        // go to consumer
        captain.GoTo(contract.Destination);
        // Sell to consumer
        captain.Unload(contract);
        // Refuel
        //captain.TryRefuel(contract.Destination, stations);

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
