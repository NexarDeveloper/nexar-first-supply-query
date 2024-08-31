package com.coding.way.nexar.demo;

import com.coding.way.nexar.models.Credentials;
import com.coding.way.nexar.nexar.NexarClient;
import com.fasterxml.jackson.databind.JsonNode;

import java.util.HashMap;
import java.util.Map;

public class MainDemo {

    public static void main(String[] args) throws Exception {
        NexarClient client = new NexarClient.Builder()
                .credentials(new Credentials("TO FILL", "TO FILL"))
                .OAuthTokenUrl("https://identity.nexar.com/connect/token")
                .build();

        String query = "query pricingByVolumeLevels {\n" +
                "    supSearchMpn(\n" +
                "        country: \"EURO\" \n" +
                "        currency: \"EUR\"\n" +
                "        q: \"GCM155R71H473KE02J\", limit: 5) {\n" +
                "        hits\n" +
                "        results {\n" +
                "            part {\n" +
                "                sellers {\n" +
                "                    company {\n" +
                "                        name\n" +
                "                    }\n" +
                "                    offers {\n" +
                "                        packaging\n" +
                "                        sku\n" +
                "                        prices {\n" +
                "                            quantity\n" +
                "                            price\n" +
                "                            currency\n" +
                "                            convertedCurrency\n" +
                "                            convertedPrice\n" +
                "                        }\n" +
                "                    }\n" +
                "                }\n" +
                "            }\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

        Map<String, Object> variables = new HashMap<>();
        JsonNode response = client.query(query, variables);
        System.out.println(response.toPrettyString());
    }
}
