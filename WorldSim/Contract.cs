namespace WorldSim;

public record Contract(
    Station Destination,
    Product Product,
    Portion PayPerUnit);
