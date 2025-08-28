{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "LoggerSettings": {
    "FilePath": null,
    "SeqPath": null,
    "SeqKey": null
  },
  "RabbitMQ": {
    "Hostname": "localhost",
    "Username": "admin",
    "Password": "admin"
  },
  "AppSettings": {
    "Keycloak": {
      "Authority": "https://idp-gc.bariserv.net/realms/gc-dev",
      "Audience": "gc-gateway-api",
      "RequireHttpsMetadata": true
    },
    "AllowedOrigins": [
      "http://localhost:4200"
    ]
  },
  "GatewayRouting": {
    "QueueByGame": {
      "worldofwarcraft": "wow_queue",
      "mainsite": "mainsite_queue"
    },
    "AllowedTablesByGame": {
      "worldofwarcraft": [
        "Classes"
      ],
      "mainsite": [
        "Countries"
      ]
    },
    "AllowedActionsByResource": {
      "worldofwarcraft/Classes": [
        "Test"
      ]
    }
  }
}
