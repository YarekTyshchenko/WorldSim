namespace WorldSim;

using System;
using System.Collections.Generic;
using System.Linq;

public class Wallet
{
    public Dictionary<Product, decimal> wallet = Enum.GetValues<Product>().ToDictionary(x => x, _ => 0m);

    /// <inheritdoc />
    public override string ToString() => this.wallet.Select(x => x.ToString()).Join(",");

    public static Wallet Combine(IEnumerable<Wallet> wallets)
    {
        var w = new Wallet();
        foreach (var wallet in wallets)
        {
            w.Add(wallet);
        }
        return w;
    }

    public Wallet Try(Ratio ratio)
    {
        var w = new Wallet
        {
            wallet = wallet.ToDictionary(x => x.Key, x => x.Value)
        };
        foreach (var (product, count) in ratio.Items)
        {
            w.wallet[product] += count;
        }

        return w;
    }

    private void Add(Wallet w)
    {
        foreach (var (key, value) in w.wallet)
        {
            this.wallet[key] += value;
        }
    }

    public void Pay(Wallet target, Product product, decimal amount)
    {
        this.wallet[product] -= amount;
        target.wallet[product] += amount;
    }
}
