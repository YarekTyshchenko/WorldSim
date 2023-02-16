namespace WorldSim;

public record Contract(
    Station Destination,
    Product Product,
    //decimal PayPerUnitInFuel,
    Ratio BarterFor);
