# EzDdd.Integration.Tests

Integration tests for ezDDD.NET, covering complete workflows across all modules.

## Purpose

This test project verifies that all ezDDD components work correctly together in realistic scenarios, with special focus on Java ezddd 4.1.0 features:

- **IDomainEvent.Metadata**: Idempotency support throughout event lifecycle
- **IReconciler**: System state reconciliation workflows
- **Thread Safety**: Concurrent access patterns
- **Event Sourcing**: Complete aggregate lifecycle with metadata

## Test Categories

### 1. CQRS Flow Tests (`CqrsFlowWithMetadataTests`)
End-to-end CQRS workflows with metadata propagation:
- Command → Aggregate → Events (with Metadata)
- Repository → Relay publication → Projector
- Archive → Query
- Metadata idempotency detection

### 2. Event Sourcing Tests (`EventSourcingMetadataTests`)
Event sourcing with metadata support:
- EsAggregateRoot replay with metadata
- Metadata serialization/deserialization
- Idempotency verification during replay

### 3. Reconciler Tests (`ReconcilerExecutionTests`)
System reconciliation workflows:
- Context → IReconciler → Report flow
- Real-world scenarios (e.g., expired order cleanup)
- Error handling and reporting

### 4. Concurrent Operations Tests (`ConcurrentOperationsTests`)
Thread safety verification under high concurrency:
- DomainEventTypeMapper concurrent registration
- Repository concurrent operations

## Test Domain

Tests use realistic domain models:
- **BankAccount**: Event-sourced aggregate with deposit/withdraw operations
- **Order**: State-sourced aggregate with reconciliation support
- **Money**: Value object
- Banking and order management use cases

## Running Tests

```bash
# Run all integration tests
dotnet test tests/EzDdd.Integration.Tests/

# Run specific test category
dotnet test --filter "FullyQualifiedName~CqrsFlowWithMetadataTests"

# Run with detailed output
dotnet test --verbosity detailed
```

## Coverage Goals

- ✅ All Java 4.1.0 features covered
- ✅ Complete workflow scenarios (not isolated units)
- ✅ Realistic domain models
- ✅ Thread safety verification

## Related Documentation

- [ADR-0024](../../docs/adr/0024-ireconciler-interface-system-reconciliation.md) - IReconciler Interface
- [ADR-0025](../../docs/adr/0025-messageproducer-refactoring-java-4-1-0-alignment.md) - MessageProducer Refactoring
- [ADR-0026](../../docs/adr/0026-service-layer-pattern.md) - Service Layer Pattern
- [ADR-0027](../../docs/adr/0027-thread-null-safety-review.md) - Thread/Null Safety

---

_Part of Phase 6 Stage S6 - Integration Testing & Documentation_
