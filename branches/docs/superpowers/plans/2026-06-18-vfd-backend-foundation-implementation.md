# VFD Backend Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish a secure, factory-scoped Django foundation and create the center database schema required for configuration synchronization and complete VFD execution traceability.

**Architecture:** Existing VFD tables remain authoritative and receive additive fields. Five new tables provide device identity, lifecycle events, WPF client registration, ordered configuration changes and idempotent ingest batches; no live production migration is applied in this plan.

**Tech Stack:** Python 3.13, Django 6, Django REST Framework, SQL Server, Django migrations

---

## File map

- Modify `branches/LXMES/settings.py`: environment-backed production settings and explicit VFD API authentication defaults.
- Modify `branches/core/base.py`: reusable revision and soft-delete mixin for configuration records.
- Modify `branches/vfd_control/models.py`: center schema and consistency constraints.
- Create `branches/vfd_control/services/factory_scope.py`: factory consistency validation shared by later APIs.
- Create `branches/vfd_control/services/__init__.py`: service package marker.
- Replace `branches/vfd_control/tests.py`: model, constraint and security regression tests.
- Create `branches/vfd_control/migrations/0002_vfd_sync_foundation.py`: additive schema migration generated from models.
- Modify `branches/.env.example`: complete environment contract without real credentials.

### Task 1: Secure and test environment-backed settings

**Files:**
- Modify: `branches/LXMES/settings.py`
- Modify: `branches/.env.example`
- Test: `branches/vfd_control/tests.py`

- [ ] **Step 1: Write the failing settings tests**

```python
import importlib
import os
from unittest import mock

from django.test import SimpleTestCase


class ProductionSettingsTests(SimpleTestCase):
    def test_database_password_comes_from_environment(self):
        with mock.patch.dict(os.environ, {"DB_PASSWORD": "test-secret"}, clear=False):
            from LXMES import settings
            reloaded = importlib.reload(settings)
        self.assertEqual(reloaded.DATABASES["default"]["PASSWORD"], "test-secret")

    def test_cors_is_not_open_by_default(self):
        from LXMES import settings
        self.assertFalse(settings.CORS_ORIGIN_ALLOW_ALL)
```

- [ ] **Step 2: Run the tests and verify the open-CORS test fails**

Run: `python manage.py test vfd_control.tests.ProductionSettingsTests -v 2`

Expected: FAIL because `CORS_ORIGIN_ALLOW_ALL` is currently `True`.

- [ ] **Step 3: Implement environment parsing and closed CORS defaults**

```python
def env_bool(name, default=False):
    value = os.getenv(name)
    if value is None:
        return default
    return value.strip().lower() in {"1", "true", "yes", "on"}


def env_list(name, default=""):
    return [item.strip() for item in os.getenv(name, default).split(",") if item.strip()]


DEBUG = env_bool("DJANGO_DEBUG", False)
CORS_ORIGIN_ALLOW_ALL = env_bool("DJANGO_CORS_ALLOW_ALL", False)
CORS_ALLOWED_ORIGINS = env_list("DJANGO_CORS_ALLOWED_ORIGINS")
CSRF_TRUSTED_ORIGINS = env_list("DJANGO_CSRF_TRUSTED_ORIGINS")
```

Add to `branches/.env.example`:

```dotenv
DJANGO_CORS_ALLOW_ALL=false
DJANGO_CORS_ALLOWED_ORIGINS=http://localhost:8080
DJANGO_CSRF_TRUSTED_ORIGINS=http://localhost:8080
```

- [ ] **Step 4: Run the focused settings tests**

Run: `python manage.py test vfd_control.tests.ProductionSettingsTests -v 2`

Expected: PASS.

- [ ] **Step 5: Commit the settings hardening**

```powershell
git add branches/LXMES/settings.py branches/.env.example branches/vfd_control/tests.py
git commit -m "security: harden Django environment settings"
```

### Task 2: Add reusable synchronized configuration fields

**Files:**
- Modify: `branches/core/base.py`
- Modify: `branches/vfd_control/models.py`
- Test: `branches/vfd_control/tests.py`

- [ ] **Step 1: Write failing field contract tests**

```python
from django.test import SimpleTestCase
from vfd_control.models import VfdDeviceModel, VfdLogicalPoint, VfdProcessPlan, VfdStation


class SyncFieldContractTests(SimpleTestCase):
    def test_configuration_models_have_revision_and_soft_delete(self):
        for model in (VfdDeviceModel, VfdLogicalPoint, VfdProcessPlan, VfdStation):
            with self.subTest(model=model.__name__):
                self.assertIsNotNone(model._meta.get_field("revision"))
                self.assertIsNotNone(model._meta.get_field("is_deleted"))
```

- [ ] **Step 2: Run and verify the test fails on missing `revision`**

Run: `python manage.py test vfd_control.tests.SyncFieldContractTests -v 2`

Expected: FAIL with `FieldDoesNotExist`.

- [ ] **Step 3: Add the abstract mixin**

```python
class SynchronizedConfigModel(FactoryScopedModel):
    revision = models.PositiveBigIntegerField(default=1, verbose_name="数据版本")
    is_deleted = models.BooleanField(default=False, db_index=True, verbose_name="是否删除")

    class Meta:
        abstract = True
```

Change configuration models in `vfd_control/models.py` to inherit from `SynchronizedConfigModel`: `VfdDeviceModel`, `VfdLogicalPoint`, `VfdLogicalPointWriteOption`, `VfdStation`, `VfdStationSlot`, `VfdProcessPlan`.

- [ ] **Step 4: Run the field contract tests**

Run: `python manage.py test vfd_control.tests.SyncFieldContractTests -v 2`

Expected: PASS.

- [ ] **Step 5: Commit synchronized fields**

```powershell
git add branches/core/base.py branches/vfd_control/models.py branches/vfd_control/tests.py
git commit -m "feat: add synchronized configuration fields"
```

### Task 3: Add WPF client, change feed and ingest batch models

**Files:**
- Modify: `branches/vfd_control/models.py`
- Test: `branches/vfd_control/tests.py`

- [ ] **Step 1: Write failing model and uniqueness tests**

```python
from django.test import SimpleTestCase
from vfd_control.models import VfdChangeFeed, VfdClientNode, VfdIngestBatch


class SyncInfrastructureModelTests(SimpleTestCase):
    def test_sync_tables_use_expected_names(self):
        self.assertEqual(VfdClientNode._meta.db_table, "vfd_client_nodes")
        self.assertEqual(VfdChangeFeed._meta.db_table, "vfd_change_feed")
        self.assertEqual(VfdIngestBatch._meta.db_table, "vfd_ingest_batches")

    def test_ingest_batch_has_client_batch_unique_constraint(self):
        fields = {tuple(item.fields) for item in VfdIngestBatch._meta.constraints}
        self.assertIn(("client_node", "batch_id"), fields)
```

- [ ] **Step 2: Run and verify imports fail**

Run: `python manage.py test vfd_control.tests.SyncInfrastructureModelTests -v 2`

Expected: FAIL because the three models do not exist.

- [ ] **Step 3: Implement the infrastructure models**

```python
class VfdClientNode(FactoryScopedModel):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    client_code = models.CharField(max_length=64)
    station = models.ForeignKey("VfdStation", on_delete=models.SET_NULL, null=True, blank=True)
    machine_name = models.CharField(max_length=128, blank=True)
    app_version = models.CharField(max_length=32, blank=True)
    last_seen_at = models.DateTimeField(null=True, blank=True)
    is_active = models.BooleanField(default=True)

    class Meta:
        db_table = "vfd_client_nodes"
        constraints = [models.UniqueConstraint(fields=["factory", "client_code"], name="uq_vfd_client_code")]


class VfdChangeFeed(models.Model):
    cursor = models.BigAutoField(primary_key=True)
    factory = models.ForeignKey("core.Factory", on_delete=models.PROTECT)
    entity_type = models.CharField(max_length=64)
    entity_id = models.CharField(max_length=64)
    operation = models.CharField(max_length=16)
    revision = models.PositiveBigIntegerField()
    changed_at = models.DateTimeField(auto_now_add=True)
    payload_json = models.JSONField(default=dict, blank=True)

    class Meta:
        db_table = "vfd_change_feed"
        indexes = [models.Index(fields=["factory", "cursor"], name="idx_vfd_change_cursor")]


class VfdIngestBatch(FactoryScopedModel):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    client_node = models.ForeignKey(VfdClientNode, on_delete=models.PROTECT)
    batch_id = models.UUIDField()
    status = models.CharField(max_length=20, default="processing")
    payload_hash = models.CharField(max_length=64)
    received_at = models.DateTimeField(auto_now_add=True)
    processed_at = models.DateTimeField(null=True, blank=True)
    result_json = models.JSONField(default=dict, blank=True)

    class Meta:
        db_table = "vfd_ingest_batches"
        constraints = [models.UniqueConstraint(fields=["client_node", "batch_id"], name="uq_vfd_ingest_batch")]
```

- [ ] **Step 4: Run the infrastructure model tests**

Run: `python manage.py test vfd_control.tests.SyncInfrastructureModelTests -v 2`

Expected: PASS.

- [ ] **Step 5: Commit sync infrastructure models**

```powershell
git add branches/vfd_control/models.py branches/vfd_control/tests.py
git commit -m "feat: add VFD synchronization infrastructure models"
```

### Task 4: Add device identity and lifecycle events

**Files:**
- Modify: `branches/vfd_control/models.py`
- Test: `branches/vfd_control/tests.py`

- [ ] **Step 1: Write failing identity and event contract tests**

```python
from django.test import SimpleTestCase
from vfd_control.models import VfdDeviceUnit, VfdRunEvent


class DeviceTraceModelTests(SimpleTestCase):
    def test_device_barcode_is_unique_per_factory(self):
        constraints = {tuple(item.fields) for item in VfdDeviceUnit._meta.constraints}
        self.assertIn(("factory", "barcode"), constraints)

    def test_run_event_sequence_is_unique_per_run(self):
        constraints = {tuple(item.fields) for item in VfdRunEvent._meta.constraints}
        self.assertIn(("device_run", "sequence"), constraints)
```

- [ ] **Step 2: Run and verify imports fail**

Run: `python manage.py test vfd_control.tests.DeviceTraceModelTests -v 2`

Expected: FAIL because the models do not exist.

- [ ] **Step 3: Implement device identity and event models**

```python
class VfdDeviceUnit(FactoryScopedModel):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    barcode = models.CharField(max_length=100)
    device_model = models.ForeignKey(VfdDeviceModel, on_delete=models.PROTECT, null=True, blank=True)
    product = models.ForeignKey("product.Product", on_delete=models.PROTECT, null=True, blank=True)
    work_order = models.ForeignKey("production.WorkOrder", on_delete=models.PROTECT, null=True, blank=True)
    current_status = models.CharField(max_length=32, default="created")
    first_scanned_at = models.DateTimeField()
    last_tested_at = models.DateTimeField(null=True, blank=True)

    class Meta:
        db_table = "vfd_device_units"
        constraints = [models.UniqueConstraint(fields=["factory", "barcode"], name="uq_vfd_device_barcode")]
        indexes = [models.Index(fields=["factory", "current_status"], name="idx_vfd_device_status")]


class VfdRunEvent(FactoryScopedModel):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    device_run = models.ForeignKey("VfdDeviceRun", on_delete=models.CASCADE, related_name="events")
    sequence = models.PositiveIntegerField()
    event_type = models.CharField(max_length=50)
    occurred_at = models.DateTimeField()
    received_at = models.DateTimeField(auto_now_add=True)
    operator = models.ForeignKey("user.SysUser", on_delete=models.SET_NULL, null=True, blank=True)
    client_node = models.ForeignKey(VfdClientNode, on_delete=models.PROTECT)
    payload_json = models.JSONField(default=dict, blank=True)

    class Meta:
        db_table = "vfd_run_events"
        constraints = [models.UniqueConstraint(fields=["device_run", "sequence"], name="uq_vfd_run_event_sequence")]
```

- [ ] **Step 4: Run the trace model tests**

Run: `python manage.py test vfd_control.tests.DeviceTraceModelTests -v 2`

Expected: PASS.

- [ ] **Step 5: Commit device trace models**

```powershell
git add branches/vfd_control/models.py branches/vfd_control/tests.py
git commit -m "feat: add VFD device identity and run events"
```

### Task 5: Enrich configuration and execution records

**Files:**
- Modify: `branches/vfd_control/models.py`
- Test: `branches/vfd_control/tests.py`

- [ ] **Step 1: Write failing required-field tests**

```python
class EnrichedTraceFieldTests(SimpleTestCase):
    def test_logical_point_has_register_layout(self):
        names = {field.name for field in VfdLogicalPoint._meta.fields}
        self.assertTrue({"register_count", "byte_order", "word_order", "bit_index"} <= names)

    def test_device_run_has_identity_attempt_and_snapshot(self):
        names = {field.name for field in VfdDeviceRun._meta.fields}
        self.assertTrue({"device_unit", "attempt_no", "status", "process_plan_version", "plan_snapshot"} <= names)

    def test_plan_and_session_have_runtime_ownership(self):
        plan_names = {field.name for field in VfdProcessPlan._meta.fields}
        session_names = {field.name for field in VfdStationSession._meta.fields}
        self.assertTrue({"device_model", "current_published_version"} <= plan_names)
        self.assertTrue({"client_node", "work_order", "status"} <= session_names)

    def test_measurement_has_sample_metadata(self):
        names = {field.name for field in VfdMeasurementResult._meta.fields}
        self.assertTrue({"sampled_at", "raw_value", "quality_code"} <= names)

    def test_audit_has_request_origin_and_revision(self):
        names = {field.name for field in VfdAuditLog._meta.fields}
        self.assertTrue({"source", "client_node", "request_id", "revision"} <= names)
```

- [ ] **Step 2: Run and verify missing fields fail**

Run: `python manage.py test vfd_control.tests.EnrichedTraceFieldTests -v 2`

Expected: FAIL with missing field assertions.

- [ ] **Step 3: Add configuration fields**

Add these model fields with backward-compatible defaults:

```python
# VfdLogicalPoint
register_count = models.PositiveSmallIntegerField(default=1)
byte_order = models.CharField(max_length=10, default="big")
word_order = models.CharField(max_length=10, default="big")
bit_index = models.PositiveSmallIntegerField(null=True, blank=True)

# VfdStationSlot
port_name = models.CharField(max_length=32, blank=True)
baud_rate = models.PositiveIntegerField(default=9600)
data_bits = models.PositiveSmallIntegerField(default=8)
stop_bits = models.PositiveSmallIntegerField(default=1)
parity = models.CharField(max_length=20, default="None")
vfd_slave_address = models.PositiveSmallIntegerField(default=1)
voltage_meter_address = models.PositiveSmallIntegerField(null=True, blank=True)
current_meter_address = models.PositiveSmallIntegerField(null=True, blank=True)

# VfdProcessStep
timeout_ms = models.PositiveIntegerField(default=5000)
retry_delay_ms = models.PositiveIntegerField(default=200)
config_snapshot = models.JSONField(default=dict, blank=True)

# VfdProcessPlan
device_model = models.ForeignKey(VfdDeviceModel, on_delete=models.PROTECT, null=True, blank=True)
current_published_version = models.ForeignKey(
    "VfdProcessPlanVersion",
    on_delete=models.PROTECT,
    null=True,
    blank=True,
    related_name="published_by_plans",
)

# VfdProcessPlanVersion
checksum = models.CharField(max_length=64, blank=True)
```

- [ ] **Step 4: Add execution fields**

```python
# VfdDeviceRun
device_unit = models.ForeignKey(VfdDeviceUnit, on_delete=models.PROTECT, null=True, blank=True)
attempt_no = models.PositiveIntegerField(default=1)
status = models.CharField(max_length=20, default="pending")
process_plan_version = models.ForeignKey(VfdProcessPlanVersion, on_delete=models.PROTECT, null=True, blank=True)
plan_snapshot = models.JSONField(default=dict, blank=True)

# VfdStepRun
attempt_no = models.PositiveIntegerField(default=1)
status = models.CharField(max_length=20, default="pending")

# VfdMeasurementResult
sampled_at = models.DateTimeField(null=True, blank=True)
raw_value = models.CharField(max_length=200, blank=True)
quality_code = models.CharField(max_length=32, default="good")

# VfdCommandTrace
occurred_at = models.DateTimeField(null=True, blank=True)
duration_ms = models.PositiveIntegerField(null=True, blank=True)
error_category = models.CharField(max_length=50, blank=True)

# VfdStationSession
client_node = models.ForeignKey(VfdClientNode, on_delete=models.PROTECT, null=True, blank=True)
work_order = models.ForeignKey("production.WorkOrder", on_delete=models.PROTECT, null=True, blank=True)
status = models.CharField(max_length=20, default="running")

# VfdAuditLog
source = models.CharField(max_length=20, default="api")
client_node = models.ForeignKey(VfdClientNode, on_delete=models.SET_NULL, null=True, blank=True)
request_id = models.UUIDField(null=True, blank=True)
revision = models.PositiveBigIntegerField(null=True, blank=True)
```

- [ ] **Step 5: Run the enriched-field tests**

Run: `python manage.py test vfd_control.tests.EnrichedTraceFieldTests -v 2`

Expected: PASS.

- [ ] **Step 6: Commit enriched records**

```powershell
git add branches/vfd_control/models.py branches/vfd_control/tests.py
git commit -m "feat: enrich VFD configuration and execution records"
```

### Task 6: Add factory consistency validation

**Files:**
- Create: `branches/vfd_control/services/__init__.py`
- Create: `branches/vfd_control/services/factory_scope.py`
- Test: `branches/vfd_control/tests.py`

- [ ] **Step 1: Write failing validation tests**

```python
from django.core.exceptions import ValidationError
from vfd_control.services.factory_scope import ensure_same_factory


class FactoryScopeServiceTests(SimpleTestCase):
    def test_accepts_matching_factory_ids(self):
        ensure_same_factory(7, device_model_factory_id=7, station_factory_id=7)

    def test_rejects_mismatched_factory_ids(self):
        with self.assertRaisesMessage(ValidationError, "station belongs to another factory"):
            ensure_same_factory(7, station_factory_id=9)
```

- [ ] **Step 2: Run and verify the module import fails**

Run: `python manage.py test vfd_control.tests.FactoryScopeServiceTests -v 2`

Expected: FAIL with `ModuleNotFoundError`.

- [ ] **Step 3: Implement the validation helper**

```python
from django.core.exceptions import ValidationError


def ensure_same_factory(expected_factory_id, **related_factory_ids):
    for relation, actual_factory_id in related_factory_ids.items():
        if actual_factory_id is not None and actual_factory_id != expected_factory_id:
            label = relation.removesuffix("_factory_id").replace("_", " ")
            raise ValidationError(f"{label} belongs to another factory")
```

- [ ] **Step 4: Run the service tests**

Run: `python manage.py test vfd_control.tests.FactoryScopeServiceTests -v 2`

Expected: PASS.

- [ ] **Step 5: Commit factory validation**

```powershell
git add branches/vfd_control/services branches/vfd_control/tests.py
git commit -m "feat: validate VFD factory ownership"
```

### Task 7: Generate and inspect the additive migration

**Files:**
- Create: `branches/vfd_control/migrations/0002_vfd_sync_foundation.py`
- Test: `branches/vfd_control/tests.py`

- [ ] **Step 1: Generate the migration**

Run: `python manage.py makemigrations vfd_control --name vfd_sync_foundation`

Expected: one `0002_vfd_sync_foundation.py` migration; no migration for unrelated applications.

- [ ] **Step 2: Verify model/migration drift is empty**

Run: `python manage.py makemigrations --check --dry-run`

Expected: `No changes detected`.

- [ ] **Step 3: Inspect SQL without applying it**

Run: `python manage.py sqlmigrate vfd_control 0002`

Expected: additive `ALTER TABLE`, `CREATE TABLE`, index and constraint statements; no `DROP TABLE` for existing VFD history tables.

- [ ] **Step 4: Run the complete VFD test module**

Run: `python manage.py test vfd_control -v 2`

Expected: PASS.

- [ ] **Step 5: Commit the migration**

```powershell
git add branches/vfd_control/migrations/0002_vfd_sync_foundation.py branches/vfd_control/models.py branches/vfd_control/tests.py
git commit -m "db: add VFD synchronization foundation schema"
```

### Task 8: Verify the phase without touching production data

**Files:**
- Verify: `branches/vfd_control/models.py`
- Verify: `branches/vfd_control/migrations/0002_vfd_sync_foundation.py`
- Verify: `VfdProductionControl/VfdProductionControl.sln`

- [ ] **Step 1: Run Django system checks**

Run: `python manage.py check --deploy`

Expected: no errors; deployment warnings must be recorded for the later rollout plan.

- [ ] **Step 2: Run all available backend tests**

Run: `python manage.py test -v 2`

Expected: PASS.

- [ ] **Step 3: Re-run the WPF regression suite**

Run from repository root: `dotnet test VfdProductionControl/VfdProductionControl.sln --no-restore`

Expected: 222 tests pass, 0 fail.

- [ ] **Step 4: Confirm production migration remains unapplied**

Run: `python manage.py showmigrations vfd_control`

Expected: local/test environment may show `[ ] 0002_vfd_sync_foundation`; production application is deferred until a verified SQL Server backup exists.

- [ ] **Step 5: Commit any verification documentation changes**

```powershell
git status --short
git log --oneline --max-count=8
```

Expected: clean worktree and one focused commit per task.
