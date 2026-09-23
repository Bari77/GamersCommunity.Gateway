# Agent guidelines — Gateway

Technical rules for AI agents working in GamersCommunity.Gateway.

## Never start servers

Do **not** run `dotnet run` or long-lived Gateway/Docker app processes. The developer owns terminals. One-shot builds/tests are OK.

## Routing contracts

- Game and Platform actions are declared in Gateway `appsettings*.json` routing tables. Keep Public/Private scopes intentional.
- Prefer aligning new resources with existing games (WoW / LoL patterns) and the Template when the shape is shared.
- Keep game `contracts/federation.contract.json` aligned with the matching microservice block here.
- Do **not** register a `template` microservice on the main Gateway (Template uses its own DevGateway).
- Bus-only Platform RPC (`Friends.AreFriends`, managed Conversations) stays off HTTP — see `docs/ROUTING.md`.

## Shared logic

If Gateway needs a generic helper used elsewhere, prefer **GamersCommunity.Core** over a one-off copy.

## Commits / push

Only when the developer explicitly asks.
