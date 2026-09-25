# Agent guidelines — Gateway

Shared module: [`AgentKit/`](AgentKit/) → [GamersCommunity.AgentKit](https://github.com/Bari77/GamersCommunity.AgentKit)

- [`AgentKit/AGENTS.base.md`](AgentKit/AGENTS.base.md)
- [`AgentKit/ENGINEERING_STANDARDS.md`](AgentKit/ENGINEERING_STANDARDS.md)
- [`AgentKit/POLICY.md`](AgentKit/POLICY.md)
- Optional: [`AGENTS.override.md`](AGENTS.override.md)

## Repo-specific

- Routing tables in `appsettings*.json`: keep Public/Private scopes intentional.
- Align new resources with existing games and Template shapes; keep `contracts/federation.contract.json` in sync per game.
- Do **not** register a `template` microservice on the main Gateway.
- Bus-only Platform RPC stays off HTTP — see `docs/ROUTING.md`.
- Prefer **GamersCommunity.Core** for shared helpers.
