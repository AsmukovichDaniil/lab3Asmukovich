using OrderFlow.Domain;

namespace OrderFlow.Domain.Tests;

public class PricingTests
{
    private static List<OrderLine> StandardOrder() => new()
    {
        new OrderLine { ProductName = "Standing desk", UnitPrice = 499.00m, Quantity = 2 },
        new OrderLine { ProductName = "Office chair", UnitPrice = 189.00m, Quantity = 1 },
        new OrderLine { ProductName = "Stapler", UnitPrice = 12.90m, Quantity = 1 },
        new OrderLine { ProductName = "Paper clip", UnitPrice = 0.10m, Quantity = 1 },
    };

    [Fact]
    public void A_regular_customer_gets_a_ten_percent_discount_over_one_thousand()
    {
        var rules = new PricingRules();

        var rate = Pricing.DiscountRate(1200m, CustomerTier.Regular, false, rules);

        Assert.Equal(0.10m, rate);
    }

    [Fact]
    public void A_vip_customer_gets_a_fifteen_percent_discount_over_one_thousand()
    {
        var rules = new PricingRules();

        var rate = Pricing.DiscountRate(1200m, CustomerTier.VIP, false, rules);

        Assert.Equal(0.15m, rate);
    }

    [Fact]
    public void A_vip_welcome_order_gets_a_twenty_percent_discount_over_one_thousand()
    {
        var rules = new PricingRules();

        var rate = Pricing.DiscountRate(1200m, CustomerTier.VIP, true, rules);

        Assert.Equal(0.20m, rate);
    }

    [Fact]
    public void An_order_at_or_below_two_thousand_pays_fifty_for_shipping()
    {
        var rules = new PricingRules();

        var shipping = Pricing.ShippingFor(2000m, rules);

        Assert.Equal(50m, shipping);
    }

    [Fact]
    public void An_order_over_two_thousand_gets_free_shipping()
    {
        var rules = new PricingRules();

        var shipping = Pricing.ShippingFor(2000.01m, rules);

        Assert.Equal(0m, shipping);
    }

    [Fact]
    public void Adding_a_paper_clip_increases_the_subtotal_by_ten_cents()
    {
        var lines = StandardOrder();
        lines.Add(new OrderLine { ProductName = "Paper clip", UnitPrice = 0.10m, Quantity = 1 });

        var subtotal = Pricing.Subtotal(lines);

        Assert.Equal(1200.10m, subtotal);
    }

    [Fact]
    public void A_regular_order_total_includes_discount_shipping_and_vat()
    {
        var sut = Pricing.Calculate(StandardOrder(), CustomerTier.Regular, false, new PricingRules());

        Assert.Equal(new OrderTotals(1200m, 120m, 50m, 1080m, 226m, 1356m), sut);
    }
}
