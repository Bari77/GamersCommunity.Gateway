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
    "Password": "admin",
    "Timeout": 10
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
    "Microservices": [
      {
        "Id": "mainsite",
        "Queue": "mainsite_queue",
        "Scope": "Private",
        "Tables": [
          {
            "Name": "Countries",
            "Scope": "Public",
            "Actions": [
              {
                "Name": "List",
                "Scope": "Public"
              }
            ]
          },
          {
            "Name": "GameTypes",
            "Scope": "Private",
            "Actions": [
              {
                "Name": "List",
                "Scope": "Public"
              }
            ]
          }
        ]
      },
      {
        "Id": "worldofwarcraft",
        "Queue": "worldofwarcraft_queue",
        "Scope": "Private",
        "Tables": [
          {
            "Name": "Classes",
            "Scope": "Public",
            "Actions": [
              {
                "Name": "List"
              }
            ]
          }
        ]
      }
    ]
  }
}
