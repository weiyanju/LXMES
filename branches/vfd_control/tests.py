from django.test import SimpleTestCase

from vfd_control.models import (
    VfdCommandTrace,
    VfdComparisonResult,
    VfdDeviceModel,
    VfdDeviceRun,
    VfdLogicalPoint,
    VfdLogicalPointWriteOption,
    VfdMeasurementResult,
    VfdProcessPlan,
    VfdProcessPlanVersion,
    VfdProcessStep,
    VfdStation,
    VfdStationSession,
    VfdStationSlot,
    VfdStepRun,
)


class VfdControlModelTests(SimpleTestCase):
    def test_vfd_models_use_expected_table_names(self):
        expected = {
            VfdDeviceModel: "vfd_device_models",
            VfdLogicalPoint: "vfd_logical_points",
            VfdLogicalPointWriteOption: "vfd_logical_point_write_options",
            VfdStation: "vfd_stations",
            VfdStationSlot: "vfd_station_slots",
            VfdProcessPlan: "vfd_process_plans",
            VfdProcessPlanVersion: "vfd_process_plan_versions",
            VfdProcessStep: "vfd_process_steps",
            VfdStationSession: "vfd_station_sessions",
            VfdDeviceRun: "vfd_device_runs",
            VfdStepRun: "vfd_step_runs",
            VfdMeasurementResult: "vfd_measurement_results",
            VfdComparisonResult: "vfd_comparison_results",
            VfdCommandTrace: "vfd_command_traces",
        }

        for model, table_name in expected.items():
            with self.subTest(model=model.__name__):
                self.assertEqual(model._meta.db_table, table_name)

    def test_process_plan_links_existing_product_and_process(self):
        self.assertEqual(
            VfdProcessPlan._meta.get_field("product").remote_field.model._meta.db_table,
            "products",
        )
        self.assertEqual(
            VfdProcessPlan._meta.get_field("process").remote_field.model._meta.db_table,
            "processes",
        )

    def test_device_run_links_existing_work_order_and_product(self):
        self.assertEqual(
            VfdDeviceRun._meta.get_field("work_order").remote_field.model._meta.db_table,
            "work_orders",
        )
        self.assertEqual(
            VfdDeviceRun._meta.get_field("product").remote_field.model._meta.db_table,
            "products",
        )

    def test_station_slot_links_existing_equipment(self):
        self.assertEqual(
            VfdStationSlot._meta.get_field("equipment").remote_field.model._meta.db_table,
            "equipment",
        )
