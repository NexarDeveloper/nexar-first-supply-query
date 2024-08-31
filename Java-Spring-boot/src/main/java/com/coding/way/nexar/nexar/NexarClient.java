package com.coding.way.nexar.nexar;

import com.coding.way.nexar.models.Credentials;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.util.Assert;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.web.client.RestClient;

import java.nio.charset.StandardCharsets;
import java.time.Instant;
import java.util.Base64;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;

public class NexarClient {

    private String accessToken;
    private Instant tokenExpiration;
    private final Credentials credentials;
    private final String host;
    private final String path;
    private final String OAuthTokenUrl;
    private final ObjectMapper objectMapper;
    private static final String CLIENT_ID = "client_id";
    private static final String CLIENT_SECRET = "client_secret";
    private static final String GRANT_TYPE = "grant_type";

    private NexarClient(Builder builder) {
        this.assertArguments(builder);
        this.credentials = builder.credentials;
        this.host = builder.host;
        this.path = builder.path;
        this.OAuthTokenUrl = builder.OAuthTokenUrl;
        this.objectMapper = new ObjectMapper();
    }

    private void assertArguments(Builder builder) {
        Assert.notNull(builder.credentials, "Credentials is required");
        Assert.notNull(builder.credentials.clientId(), "Credentials client id is required");
        Assert.notNull(builder.credentials.clientSecret(), "Credentials client secret is required");
        Assert.notNull(builder.OAuthTokenUrl, "OAuthTokenUrl is required");
    }

    private JsonNode decodeJWT(String jwt) throws Exception {
        String[] parts = jwt.split("\\.");
        String payload = new String(Base64.getUrlDecoder().decode(parts[1]), StandardCharsets.UTF_8);
        return objectMapper.readTree(payload);
    }

    private synchronized String getAccessToken() throws Exception {
        if (Objects.isNull(tokenExpiration) || tokenExpiration.isBefore(Instant.now().minusSeconds(300))) {

            MultiValueMap<String, Object> parts = new LinkedMultiValueMap<>();

            parts.add(GRANT_TYPE, "client_credentials");
            parts.add(CLIENT_ID, credentials.clientId());
            parts.add(CLIENT_SECRET, credentials.clientSecret());

            var response = RestClient
                    .create()
                    .post()
                    .uri(OAuthTokenUrl)
                    .header("Content-Type", "application/x-www-form-urlencoded")
                    .body(parts)
                    .retrieve()
                    .body(JsonNode.class);

            accessToken = Objects.requireNonNull(response).get("access_token").asText();
            JsonNode decodedToken = decodeJWT(accessToken);
            tokenExpiration = Instant.ofEpochSecond(decodedToken.get("exp").asLong());
        }
        return accessToken;
    }

    public JsonNode query(String gqlQuery, Map<String, Object> variables) throws Exception {
        String token = getAccessToken();
        String graphqlUrl = "https://" + host + path;
        Map<String, Object> requestBody = new HashMap<>();
        requestBody.put("query", gqlQuery);
        requestBody.put("variables", variables);
        return RestClient
                .create()
                .post()
                .uri(graphqlUrl)
                .header("Authorization", "Bearer " + token)
                .body(requestBody)
                .retrieve()
                .body(JsonNode.class);
    }

    public static class Builder {
        private Credentials credentials;
        private String host = "api.nexar.com";
        private String path = "/graphql";
        private String OAuthTokenUrl;

        public Builder credentials(Credentials credentials) {
            this.credentials = credentials;
            return this;
        }

        public Builder host(String host) {
            this.host = host;
            return this;
        }

        public Builder path(String path) {
            this.path = path;
            return this;
        }

        public Builder OAuthTokenUrl(String OAuthTokenUrl) {
            this.OAuthTokenUrl = OAuthTokenUrl;
            return this;
        }

        public NexarClient build() {
            return new NexarClient(this);
        }
    }
}
