# Agent guidelines — Gateway

Technical rules for AI agents working in GamersCommunity.Gateway.

## Never start servers

Do **not** run `dotnet run` or long-lived Gateway/Docker app processes. The developer owns terminals. One-shot builds/tests are OK.

## Routing contracts

- Game and Platform actions are declared in Gateway `appsettings*.json` routing tables. Keep Public/Private scopes intentional.
- Prefer aligning new resources with existing games (WoW / LoL patterns) and the Template when the shape is shared.

## Shared logic

If Gateway needs a generic helper used elsewhere, prefer **GamersCommunity.Core** over a one-off copy.

## Commits / push

Only when the developer explicitly asks.
