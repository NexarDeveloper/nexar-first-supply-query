package com.coding.way.nexar.models;

import java.util.List;

public record GraphQLResponse(
        Data data,
        Extensions extensions
) {
    public record Data(
            SupSearchMpn supSearchMpn
    ) {
    }

    public record SupSearchMpn(
            int hits,
            List<Result> results
    ) {
    }

    public record Result(
            Part part
    ) {
    }

    public record Part(
            List<Seller> sellers
    ) {
    }

    public record Seller(
            Company company,
            List<Offer> offers
    ) {
    }

    public record Company(
            String name
    ) {
    }

    public record Offer(
            List<Price> prices
    ) {
    }

    public record Price(
            int quantity,
            double price
    ) {
    }

    public record Extensions(
            String requestId
    ) {
    }
}
