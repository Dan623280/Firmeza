# Implementation status

The approved API plan has been implemented in the current API workspace. No branch, commit, push, merge, or external deployment was performed.

| Task | Delivered | Evidence |
| --- | --- | --- |
| 1 | Projects and complete core domain | Money, stock, snapshots, transitions, date periods and customer validation tests |
| 2 | Application interfaces, DTOs, feature services and mappings | Real service behavior covered through PostgreSQL and HTTP tests |
| 3 | Identity/business schema and migration configuration | Migrations applied to isolated empty databases; constraints and restricted deletes tested; no pending model changes |
| 4 | Transactional stock, sales, idempotency and rentals | Simultaneous last-unit purchases and overlapping reservations tested; rollback and replay tests |
| 5 | Identity, JWT, controllers, roles, ownership and Swagger | Registration, permissions, expiry, tampering, deactivation and HTTP validation tested |
| 6 | Private PDF receipts and SMTP outbox | PDF generation and persisted five-attempt SMTP failure behavior tested; real SMTP delivery unverified |
| 7 | Defined mixed XLSX import and XLSX/PDF exports | Normalization, replay, atomic validation, CRC/metadata rejection and detail export tests |
| 8 | Docker build/configuration and technical documentation | Release build/publish and Compose configuration validated; published application HTTP smoke check |

Final validation: `dotnet test FirmezaApi.slnx --configuration Release` with a dedicated PostgreSQL connection: **37 passed, 0 failed, 0 skipped**. Release build: **0 warnings, 0 errors**. Published API smoke checks returned 200 for live/ready, catalogue, Swagger, administrator login and dashboard. Swagger contains 23 documented paths.

A separate reviewer identified an import replay stock-reset defect and a stale tracked-customer validation defect. Both were reproduced with failing regression tests, corrected, and verified with the full suite. Additional file integrity and large-sale export probes were implemented.

## Decisions made during implementation

- Work in the authorized current API directory after branch/worktree creation was blocked by parent Git filesystem permissions. Cost: changes remain uncommitted on the existing checkout.
- Make workbook imports atomic rather than partially applying valid rows. Cost: an invalid row requires correcting and resubmitting the workbook.
- Run migrations and Identity initialization as an explicit `--migrate` stage. Cost: deployments must run that stage before serving requests.
- Validate ZIP streams and CRC, then rebuild the archive before passing it to EPPlus. Cost: bounded additional upload compression CPU and memory.
- Export sales details in a separate linked `SaleItems` worksheet. Cost: report consumers join detail rows using saleId.

## External verification limits

Docker Desktop could not start its QEMU virtual machine. Compose configuration was checked, but image building and full Compose startup were not executed. PostgreSQL was run independently in a disposable temporary environment for integration tests.

Real SMTP delivery was not executed because credentials and a controlled recipient were not supplied. The SMTP implementation and failure behavior are tested without sending email to anyone.

EPPlus, QuestPDF and AutoMapper license configuration is provided. No commercial eligibility is assumed and no commercial key was supplied. Existing Razor database compatibility is not established because no schema or migration history for that module was provided.
