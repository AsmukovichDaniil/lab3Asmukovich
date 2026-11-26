using FluentAssertions;
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

    [Theory]
    [InlineData(999.99, CustomerTier.Regular, false, 0.00)]
    [InlineData(1000.00, CustomerTier.Regular, false, 0.00)]
    [InlineData(1000.01, CustomerTier.Regular, false, 0.10)]
    [InlineData(1000.01, CustomerTier.VIP, false, 0.15)]
    [InlineData(1000.01, CustomerTier.VIP, true, 0.20)]
    public void A_discount_rate_matches_the_order_amount_customer_tier_and_promotion(
        decimal subtotal, CustomerTier tier, bool promoApplies, decimal expectedRate)
    {
        var rules = new PricingRules();

        var rate = Pricing.DiscountRate(subtotal, tier, promoApplies, rules);

        rate.Should().Be(expectedRate,
            $"the documented threshold and {tier} rate with promo={promoApplies} produce that discount rate");
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(100, 50)]
    [InlineData(1500, 50)]
    [InlineData(2000, 50)]
    [InlineData(2000.01, 0)]
    [InlineData(2500, 0)]
    public void Shipping_cost_depends_on_the_amount(decimal amount, decimal expectedShipping)
    {
        var rules = new PricingRules();

        var shipping = Pricing.ShippingFor(amount, rules);

        shipping.Should().Be(expectedShipping,
            $"shipping is free only when the amount is over the 2,000 threshold");
    }

    [Fact]
    public void Adding_a_paper_clip_increases_the_subtotal_by_ten_cents()
    {
        var lines = StandardOrder();
        lines.Add(new OrderLine { ProductName = "Paper clip", UnitPrice = 0.10m, Quantity = 1 });

        var subtotal = Pricing.Subtotal(lines);

        subtotal.Should().Be(1200.10m, "the added paper clip costs 0.10");
    }

    [Fact]
    public void A_regular_order_total_includes_discount_shipping_and_vat()
    {
        var sut = Pricing.Calculate(StandardOrder(), CustomerTier.Regular, false, new PricingRules());

        sut.Should().Be(new OrderTotals(1200m, 120m, 50m, 1080m, 226m, 1356m),
            "the subtotal is 1,200, the regular discount is 120, shipping is 50, and VAT is 20% of 1,130");
    }

    [Theory]
    [InlineData(CustomerTier.Regular)]
    [InlineData(CustomerTier.VIP)]
    public void A_welcome_promo_gives_five_percent_below_the_discount_threshold_for_every_tier(CustomerTier tier)
    {
        var rules = new PricingRules();

        var rate = Pricing.DiscountRate(500m, tier, true, rules);

        rate.Should().Be(0.05m, "WELCOME adds five percent even below the order threshold, regardless of tier");
    }

    [Theory]
    [InlineData(2000.01, CustomerTier.Regular)]
    [InlineData(2100.00, CustomerTier.VIP)]
    public void Shipping_uses_the_net_amount_after_discount(double subtotal, CustomerTier tier)
    {
        var lines = new List<OrderLine> { new() { UnitPrice = (decimal)subtotal, Quantity = 1 } };
        var rules = new PricingRules();

        var totals = Pricing.Calculate(lines, tier, false, rules);

        totals.Shipping.Should().Be(50m,
            $"after the {tier} discount, the net amount is below the 2,000 free-shipping threshold");
    }
}
