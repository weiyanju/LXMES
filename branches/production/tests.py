from django.db import models
from django.test import SimpleTestCase

from production.barcode_rules import (
    BarcodeFormatError,
    expected_barcode_type,
    match_internal_main_barcode_range,
    parse_internal_barcode,
    validate_barcode_type,
)
from product.models import Product
from production.models import ProductionPlan, WorkOrder


class ProductionProductRelationTests(SimpleTestCase):
    def test_production_plan_has_product_foreign_key(self):
        field = ProductionPlan._meta.get_field("product")

        self.assertIsInstance(field, models.ForeignKey)
        self.assertIs(field.remote_field.model, Product)
        self.assertTrue(field.null)
        self.assertTrue(field.blank)
        self.assertEqual(field.remote_field.on_delete, models.PROTECT)

    def test_work_order_has_product_foreign_key(self):
        field = WorkOrder._meta.get_field("product")

        self.assertIsInstance(field, models.ForeignKey)
        self.assertIs(field.remote_field.model, Product)
        self.assertTrue(field.null)
        self.assertTrue(field.blank)
        self.assertEqual(field.remote_field.on_delete, models.PROTECT)


class CustomerBarcodeRangeTests(SimpleTestCase):
    def test_production_plan_has_customer_barcode_range(self):
        self.assert_customer_barcode_field(ProductionPlan, "customer_start_barcode")
        self.assert_customer_barcode_field(ProductionPlan, "customer_end_barcode")

    def test_work_order_has_customer_barcode_range(self):
        self.assert_customer_barcode_field(WorkOrder, "customer_start_barcode")
        self.assert_customer_barcode_field(WorkOrder, "customer_end_barcode")

    def assert_customer_barcode_field(self, model, field_name):
        field = model._meta.get_field(field_name)

        self.assertIsInstance(field, models.CharField)
        self.assertEqual(field.max_length, 50)
        self.assertTrue(field.blank)


class InternalBarcodeRuleTests(SimpleTestCase):
    def test_parse_vfd_main_barcode(self):
        barcode = parse_internal_barcode("LXVFD11000001")

        self.assertEqual(barcode.raw, "LXVFD11000001")
        self.assertEqual(barcode.business_prefix, "VFD")
        self.assertEqual(barcode.role_code, "1")
        self.assertEqual(barcode.component_code, "1")
        self.assertEqual(barcode.serial, "000001")
        self.assertEqual(barcode.serial_number, 1)
        self.assertTrue(barcode.is_main)
        self.assertFalse(barcode.is_sub)
        self.assertEqual(barcode.component_name, "整机/变频器本体")

    def test_parse_vfd_sub_barcode(self):
        barcode = parse_internal_barcode("LXVFD23005210")

        self.assertEqual(barcode.role_code, "2")
        self.assertEqual(barcode.component_code, "3")
        self.assertEqual(barcode.serial, "005210")
        self.assertFalse(barcode.is_main)
        self.assertTrue(barcode.is_sub)
        self.assertEqual(barcode.component_name, "主板")

    def test_rejects_barcode_with_separator(self):
        with self.assertRaises(BarcodeFormatError):
            parse_internal_barcode("LX-VFD-000001")

    def test_rejects_unknown_role(self):
        with self.assertRaises(BarcodeFormatError):
            parse_internal_barcode("LXVFD31000001")

    def test_vfd_main_range_matching(self):
        self.assertTrue(
            match_internal_main_barcode_range(
                "LXVFD11000352",
                "LXVFD11000001",
                "LXVFD11010000",
            )
        )
        self.assertFalse(
            match_internal_main_barcode_range(
                "LXVFD11010001",
                "LXVFD11000001",
                "LXVFD11010000",
            )
        )

    def test_range_matching_requires_main_barcode(self):
        with self.assertRaises(BarcodeFormatError):
            match_internal_main_barcode_range(
                "LXVFD23005210",
                "LXVFD11000001",
                "LXVFD11010000",
            )

    def test_expected_barcode_type_comes_from_role_and_component(self):
        self.assertEqual(expected_barcode_type("LXVFD11000001"), 1)
        self.assertEqual(expected_barcode_type("LXVFD23005210"), 3)

    def test_validate_barcode_type_rejects_mismatch(self):
        with self.assertRaises(BarcodeFormatError):
            validate_barcode_type("LXVFD23005210", 4)
