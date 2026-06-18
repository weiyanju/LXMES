# VFD Three-End Sync Delivery Roadmap

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the approved VFD three-end synchronization and traceability design as independently testable increments.

**Architecture:** Django owns the center database and all business writes. WPF consumes the API, stores executable configuration and run journals in SQLite, and uploads idempotent batches; Web uses the same API contract.

**Tech Stack:** Django 6, Django REST Framework, SQL Server, .NET 10 WPF, SQLite, xUnit

---

## Delivery plans

1. `2026-06-18-vfd-backend-foundation-implementation.md`
   - Secure settings, factory scope, center schema, device identity, run events, client nodes, change feed, ingest batches.
2. `2026-06-18-vfd-configuration-api-implementation.md`
   - Device model, logical point, station, draft/version/publish APIs, revision conflicts, audit, bootstrap and change feed.
3. `2026-06-18-vfd-wpf-api-cache-implementation.md`
   - HTTP authentication, DTO mapping, SQLite configuration packages, bootstrap/incremental sync, WPF editors through API.
4. `2026-06-18-vfd-execution-ingest-implementation.md`
   - Parent-first execution persistence, local run journal, outbox, atomic idempotent batch ingest and crash recovery.
5. `2026-06-18-vfd-trace-query-implementation.md`
   - Barcode/run queries, complete trace projection, WPF history screens and authorization.
6. `2026-06-18-vfd-integration-rollout-implementation.md`
   - Three-end acceptance, backup/restore drill, metrics, feature flags and staged rollout.

## Program acceptance gates

- [ ] Web-originated published configuration reaches online WPF clients within 10 seconds.
- [ ] WPF-originated draft edits are visible through the same API and cannot overwrite a newer revision.
- [ ] A running device remains pinned to its starting plan snapshot.
- [ ] Offline runs upload exactly once after reconnection.
- [ ] Barcode trace includes operator, station, slot, plan version, events, steps, commands, measurements and conclusion.
- [ ] Center database migrations are produced only by Django and are recoverable from a verified backup.

