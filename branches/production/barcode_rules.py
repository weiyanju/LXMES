import re
from dataclasses import dataclass


SYSTEM_PREFIX = "LX"
BUSINESS_PREFIX = "VFD"
SERIAL_LENGTH = 6
MAIN_ROLE_CODE = "1"
SUB_ROLE_CODE = "2"
MAIN_COMPONENT_CODE = "1"

ROLE_NAMES = {
    MAIN_ROLE_CODE: "主条码",
    SUB_ROLE_CODE: "子条码",
}

COMPONENT_NAMES = {
    "1": "整机/变频器本体",
    "2": "外壳",
    "3": "主板",
    "4": "电源板",
    "5": "通讯板",
    "6": "继电器板",
    "7": "驱动板",
    "8": "键盘板",
    "9": "预留",
}

COMPONENT_BARCODE_TYPES = {
    "1": 1,
    "2": 2,
    "3": 3,
    "4": 4,
    "5": 5,
    "6": 6,
    "7": 7,
    "8": 8,
}

BARCODE_PATTERN = re.compile(
    rf"^(?P<system>{SYSTEM_PREFIX})"
    rf"(?P<business>{BUSINESS_PREFIX})"
    r"(?P<role>\d)"
    r"(?P<component>\d)"
    rf"(?P<serial>\d{{{SERIAL_LENGTH}}})$"
)


class BarcodeFormatError(ValueError):
    pass


@dataclass(frozen=True)
class InternalBarcode:
    raw: str
    system_prefix: str
    business_prefix: str
    role_code: str
    component_code: str
    serial: str

    @property
    def serial_number(self):
        return int(self.serial)

    @property
    def is_main(self):
        return self.role_code == MAIN_ROLE_CODE

    @property
    def is_sub(self):
        return self.role_code == SUB_ROLE_CODE

    @property
    def role_name(self):
        return ROLE_NAMES[self.role_code]

    @property
    def component_name(self):
        return COMPONENT_NAMES[self.component_code]

    @property
    def range_key(self):
        return (
            self.system_prefix,
            self.business_prefix,
            self.role_code,
            self.component_code,
        )


def parse_internal_barcode(value):
    barcode = (value or "").strip().upper()
    match = BARCODE_PATTERN.match(barcode)
    if not match:
        raise BarcodeFormatError("内部条码格式必须为 LXVFD + 角色位 + 部件类型位 + 6 位数字")

    role_code = match.group("role")
    component_code = match.group("component")
    if role_code not in ROLE_NAMES:
        raise BarcodeFormatError(f"未知条码角色位: {role_code}")
    if component_code not in COMPONENT_NAMES:
        raise BarcodeFormatError(f"未知部件类型位: {component_code}")
    if role_code == MAIN_ROLE_CODE and component_code != MAIN_COMPONENT_CODE:
        raise BarcodeFormatError("内部主条码部件类型位必须为 1")

    return InternalBarcode(
        raw=barcode,
        system_prefix=match.group("system"),
        business_prefix=match.group("business"),
        role_code=role_code,
        component_code=component_code,
        serial=match.group("serial"),
    )


def is_vfd_main_barcode(value):
    try:
        return parse_internal_barcode(value).is_main
    except BarcodeFormatError:
        return False


def assert_vfd_main_barcode(value):
    barcode = parse_internal_barcode(value)
    if not barcode.is_main:
        raise BarcodeFormatError("请扫描变频器内部主条码")
    return barcode


def match_internal_main_barcode_range(value, start_barcode, end_barcode):
    barcode = assert_vfd_main_barcode(value)
    start = assert_vfd_main_barcode(start_barcode)
    end = assert_vfd_main_barcode(end_barcode)

    if barcode.range_key != start.range_key or barcode.range_key != end.range_key:
        raise BarcodeFormatError("条码范围前缀、角色位和部件类型位必须一致")
    if start.serial_number > end.serial_number:
        raise BarcodeFormatError("开始条码流水号不能大于结束条码流水号")

    return start.serial_number <= barcode.serial_number <= end.serial_number


def expected_barcode_type(value):
    barcode = parse_internal_barcode(value)
    if barcode.is_main:
        return 1
    if barcode.component_code not in COMPONENT_BARCODE_TYPES:
        raise BarcodeFormatError(f"部件类型位 {barcode.component_code} 暂未对应数据库条码类型")
    return COMPONENT_BARCODE_TYPES[barcode.component_code]


def validate_barcode_type(value, barcode_type):
    expected_type = expected_barcode_type(value)
    if expected_type != barcode_type:
        raise BarcodeFormatError(f"条码类型不匹配，应为 {expected_type}，实际为 {barcode_type}")
    return True
