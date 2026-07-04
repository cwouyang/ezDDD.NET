# ezDDD.NET Roadmap

**Project**: ezDDD.NET - Tactical DDD Framework for .NET

---

## Current Status

ezDDD.NET is a .NET port of the Java [ezddd](https://gitlab.com/TeddyChen/ezddd) library, providing tactical Domain-Driven Design (DDD) patterns, Command Query Responsibility Segregation (CQRS), and Clean Architecture support. It supports both **state sourcing** and **event sourcing** for implementing aggregates and repositories.

- **Based on**: Java ezddd 6.0.1 (commit `3aac0f5`), ~99% semantic parity
- **Status**: first public release (1.0.0) in preparation - not yet published to NuGet
- **Modules**: ezDDD.Common, ezDDD.Entity, ezDDD.UseCase, ezDDD.Cqrs, ezDDD.Core (aggregator)
- **Architecture decisions**: documented as ADRs in [docs/adr/](docs/adr/)

---

## Future Considerations (Post-1.0.0, Not Committed)

- **ezDDD.Gateway package** — port of the upstream ezddd-gateway artifact (`MessageProducer`
  moved out of core in Java 6.0.0); deferred to post-1.0 (see [ADR-0029](docs/adr/0029-messageproducer-removal-gateway-deferral.md))
- **Post-release review pass** — final review & completion pass deferred to post-release maintenance
- **Performance** — benchmarks and optimization profiles (Expression Trees instead of
  reflection, AOT support, ValueTask for hot paths)
- **Event store adapters** — EventStoreDB, Marten
- **Messaging adapters** — Azure Service Bus, RabbitMQ, Kafka (natural candidates for ezDDD.Gateway)
- **ASP.NET Core integration** — helpers/middleware, projector lifecycle via BackgroundService
- **Additional examples** — e-commerce, reservation system, gRPC integration
- **Advanced event sourcing** — snapshots, temporal queries, saga pattern support

---

## Java Upstream Tracking

Monitor Java ezddd releases (post-6.0.1) and synchronize as needed to maintain ~99% semantic parity.

---

**Repository**: [github.com/cwouyang/ezDDD.NET](https://github.com/cwouyang/ezDDD.NET)
**Java upstream**: [gitlab.com/TeddyChen/ezddd](https://gitlab.com/TeddyChen/ezddd)
