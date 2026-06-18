from django.db import models

# ==================== 基础数据 ====================

class Customer(models.Model):
    customer_code = models.CharField(max_length=50, verbose_name='客户编码')
    customer_name = models.CharField(max_length=100, verbose_name='客户名称')
    contact_person = models.CharField(max_length=100, blank=True, verbose_name='联系人')
    contact_phone = models.CharField(max_length=20, blank=True, verbose_name='联系电话')
    email = models.EmailField(max_length=100, blank=True, verbose_name='邮箱')
    address = models.CharField(max_length=255, blank=True, verbose_name='地址')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'customers'
        verbose_name = '客户'
        verbose_name_plural = '客户'

    def __str__(self):
        return self.customer_name


class Factory(models.Model):
    factory_code = models.CharField(max_length=50, verbose_name='工厂编码')
    factory_name = models.CharField(max_length=100, verbose_name='工厂名称')
    address = models.CharField(max_length=255, blank=True, verbose_name='工厂地址')
    contact_person = models.CharField(max_length=100, blank=True, verbose_name='联系人')
    contact_phone = models.CharField(max_length=20, blank=True, verbose_name='联系电话')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'factories'
        verbose_name = '工厂'
        verbose_name_plural = '工厂'

    def __str__(self):
        return self.factory_name


class Product(models.Model):
    product_code = models.CharField(max_length=50, verbose_name='产品编码')
    product_name = models.CharField(max_length=100, verbose_name='产品名称')
    product_type = models.CharField(max_length=50, blank=True, verbose_name='产品类型')
    specification = models.CharField(max_length=255, blank=True, verbose_name='产品规格')
    product_image = models.CharField(max_length=255, blank=True, verbose_name='产品图片路径')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='所属工厂')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'products'
        verbose_name = '产品'
        verbose_name_plural = '产品'

    def __str__(self):
        return self.product_name


class Process(models.Model):
    process_code = models.CharField(max_length=50, verbose_name='工序编码')
    process_name = models.CharField(max_length=100, verbose_name='工序名称')
    description = models.CharField(max_length=255, blank=True, verbose_name='工序描述')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='所属工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'processes'
        verbose_name = '工序'
        verbose_name_plural = '工序'

    def __str__(self):
        return self.process_name


class ProductProcess(models.Model):
    product = models.ForeignKey(Product, on_delete=models.CASCADE, verbose_name='产品')
    process = models.ForeignKey(Process, on_delete=models.CASCADE, verbose_name='工序')
    sequence = models.IntegerField(verbose_name='工序顺序')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='所属工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')

    class Meta:
        db_table = 'product_processes'
        verbose_name = '产品工序'
        verbose_name_plural = '产品工序'


class Department(models.Model):
    department_code = models.CharField(max_length=50, verbose_name='部门编码')
    department_name = models.CharField(max_length=100, verbose_name='部门名称')
    parent = models.ForeignKey('self', on_delete=models.SET_NULL, null=True, blank=True, verbose_name='父部门')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'departments'
        verbose_name = '部门'
        verbose_name_plural = '部门'

    def __str__(self):
        return self.department_name


class Employee(models.Model):
    employee_code = models.CharField(max_length=50, verbose_name='员工编码')
    name = models.CharField(max_length=100, verbose_name='员工姓名')
    department = models.ForeignKey(Department, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='部门')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    position = models.CharField(max_length=100, blank=True, verbose_name='职位')
    phone = models.CharField(max_length=20, blank=True, verbose_name='联系电话')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'employees'
        verbose_name = '员工'
        verbose_name_plural = '员工'

    def __str__(self):
        return self.name


# ==================== 生产计划与工单 ====================

class ProductionPlan(models.Model):
    plan_name = models.CharField(max_length=100, verbose_name='计划名称')
    product_type = models.CharField(max_length=50, blank=True, verbose_name='产品类型')
    quantity = models.IntegerField(verbose_name='计划数量')
    start_barcode = models.CharField(max_length=30, blank=True, verbose_name='开始条码')
    end_barcode = models.CharField(max_length=30, blank=True, verbose_name='结束条码')
    start_date = models.DateField(blank=True, null=True, verbose_name='开始日期')
    end_date = models.DateField(blank=True, null=True, verbose_name='结束日期')
    demand_date = models.DateField(blank=True, null=True, verbose_name='需求日期')
    source = models.IntegerField(blank=True, null=True, verbose_name='来源(1客户订单2库存备货)')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    customer = models.ForeignKey(Customer, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='客户')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_by = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='创建人')
    remark = models.TextField(blank=True, verbose_name='备注')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'production_plans'
        verbose_name = '生产计划'
        verbose_name_plural = '生产计划'

    def __str__(self):
        return self.plan_name


class WorkOrder(models.Model):
    order_number = models.CharField(max_length=50, verbose_name='工单编号')
    plan = models.ForeignKey(ProductionPlan, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='生产计划')
    product_type = models.CharField(max_length=50, blank=True, verbose_name='产品类型')
    quantity = models.IntegerField(verbose_name='数量')
    start_barcode = models.CharField(max_length=30, blank=True, verbose_name='开始条码')
    end_barcode = models.CharField(max_length=30, blank=True, verbose_name='结束条码')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    demand_date = models.DateField(blank=True, null=True, verbose_name='需求日期')
    process_file = models.ForeignKey('ProcessFile', on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工艺文件')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    start_time = models.DateTimeField(blank=True, null=True, verbose_name='开始时间')
    end_time = models.DateTimeField(blank=True, null=True, verbose_name='结束时间')
    remark = models.TextField(blank=True, verbose_name='备注')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'work_orders'
        verbose_name = '工单'
        verbose_name_plural = '工单'

    def __str__(self):
        return self.order_number


class ProcessFile(models.Model):
    file_code = models.CharField(max_length=50, verbose_name='工艺文件编号')
    file_name = models.CharField(max_length=100, verbose_name='工艺文件名称')
    version = models.CharField(max_length=20, blank=True, verbose_name='版本号')
    file_path = models.CharField(max_length=255, verbose_name='文件存储路径')
    file_size = models.BigIntegerField(blank=True, null=True, verbose_name='文件大小(字节)')
    description = models.TextField(blank=True, verbose_name='工艺文件描述')
    product_type = models.CharField(max_length=50, blank=True, verbose_name='适用产品类型')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    uploaded_by = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='上传人')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'process_files'
        verbose_name = '工艺文件'
        verbose_name_plural = '工艺文件'

    def __str__(self):
        return self.file_name


# ==================== 设备管理 ====================

class Equipment(models.Model):
    equipment_code = models.CharField(max_length=50, verbose_name='设备编码')
    equipment_name = models.CharField(max_length=100, verbose_name='设备名称')
    type = models.CharField(max_length=50, blank=True, verbose_name='设备类型')
    model = models.CharField(max_length=100, blank=True, verbose_name='设备型号')
    specification = models.CharField(max_length=255, blank=True, verbose_name='设备规格')
    manufacturer = models.CharField(max_length=100, blank=True, verbose_name='制造商')
    supplier = models.CharField(max_length=100, blank=True, verbose_name='供应商')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    location = models.CharField(max_length=100, blank=True, verbose_name='位置')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    purchase_date = models.DateField(blank=True, null=True, verbose_name='购买日期')
    installation_date = models.DateField(blank=True, null=True, verbose_name='安装日期')
    warranty_end_date = models.DateField(blank=True, null=True, verbose_name='质保结束日期')
    last_maintenance = models.DateField(blank=True, null=True, verbose_name='上次维护日期')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'equipment'
        verbose_name = '设备'
        verbose_name_plural = '设备'

    def __str__(self):
        return self.equipment_name


class EquipmentStatus(models.Model):
    equipment = models.ForeignKey(Equipment, on_delete=models.CASCADE, verbose_name='设备')
    status = models.CharField(max_length=20, verbose_name='状态')
    description = models.CharField(max_length=255, blank=True, verbose_name='状态描述')
    operator = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='操作人')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')

    class Meta:
        db_table = 'equipment_status'
        verbose_name = '设备状态'
        verbose_name_plural = '设备状态'


class MaintenancePlan(models.Model):
    plan_code = models.CharField(max_length=50, verbose_name='计划编码')
    plan_name = models.CharField(max_length=100, verbose_name='计划名称')
    equipment = models.ForeignKey(Equipment, on_delete=models.CASCADE, verbose_name='设备')
    maintenance_type = models.CharField(max_length=50, blank=True, verbose_name='维护类型')
    scheduled_date = models.DateField(blank=True, null=True, verbose_name='计划维护日期')
    estimated_duration = models.IntegerField(blank=True, null=True, verbose_name='预计维护时长(小时)')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    assignee = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='指派人员')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'maintenance_plans'
        verbose_name = '设备维护计划'
        verbose_name_plural = '设备维护计划'


class MaintenanceRecord(models.Model):
    plan = models.ForeignKey(MaintenancePlan, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='维护计划')
    equipment = models.ForeignKey(Equipment, on_delete=models.CASCADE, verbose_name='设备')
    maintenance_type = models.CharField(max_length=50, blank=True, verbose_name='维护类型')
    maintenance_date = models.DateField(blank=True, null=True, verbose_name='维护日期')
    duration = models.IntegerField(blank=True, null=True, verbose_name='维护时长(小时)')
    maintenance_content = models.TextField(blank=True, verbose_name='维护内容')
    maintenance_result = models.CharField(max_length=20, blank=True, verbose_name='维护结果')
    maintenance_by = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='维护人员')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')

    class Meta:
        db_table = 'maintenance_records'
        verbose_name = '设备维护记录'
        verbose_name_plural = '设备维护记录'


class EquipmentFault(models.Model):
    fault_code = models.CharField(max_length=50, verbose_name='故障编码')
    equipment = models.ForeignKey(Equipment, on_delete=models.CASCADE, verbose_name='设备')
    fault_description = models.TextField(blank=True, verbose_name='故障描述')
    fault_time = models.DateTimeField(blank=True, null=True, verbose_name='故障发生时间')
    fault_level = models.CharField(max_length=20, blank=True, verbose_name='故障级别')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    repair_content = models.TextField(blank=True, verbose_name='维修内容')
    repair_time = models.DateTimeField(blank=True, null=True, verbose_name='维修时间')
    repair_by = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='维修人员')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'equipment_faults'
        verbose_name = '设备故障'
        verbose_name_plural = '设备故障'


# ==================== 生产数据采集 ====================

class ProductionData(models.Model):
    work_order = models.ForeignKey(WorkOrder, on_delete=models.CASCADE, verbose_name='工单')
    equipment = models.ForeignKey(Equipment, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='设备')
    product_code = models.CharField(max_length=50, blank=True, verbose_name='产品编码')
    parameter_name = models.CharField(max_length=100, blank=True, verbose_name='参数名称')
    parameter_value = models.CharField(max_length=100, blank=True, verbose_name='参数值')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    timestamp = models.DateTimeField(auto_now_add=True, verbose_name='采集时间')
    operator = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='操作人')

    class Meta:
        db_table = 'production_data'
        verbose_name = '生产数据'
        verbose_name_plural = '生产数据'


# ==================== 设备参数 ====================

class MeterParameter(models.Model):
    product_code = models.CharField(max_length=50, verbose_name='产品编码')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    voltage = models.CharField(max_length=20, blank=True, verbose_name='电压')
    current = models.CharField(max_length=20, blank=True, verbose_name='电流')
    power = models.CharField(max_length=20, blank=True, verbose_name='功率')
    frequency = models.CharField(max_length=20, blank=True, verbose_name='频率')
    accuracy = models.CharField(max_length=20, blank=True, verbose_name='精度')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')

    class Meta:
        db_table = 'meter_parameters'
        verbose_name = '电表参数'
        verbose_name_plural = '电表参数'


class InverterParameter(models.Model):
    product_code = models.CharField(max_length=50, verbose_name='产品编码')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    input_voltage = models.CharField(max_length=20, blank=True, verbose_name='输入电压')
    output_voltage = models.CharField(max_length=20, blank=True, verbose_name='输出电压')
    input_current = models.CharField(max_length=20, blank=True, verbose_name='输入电流')
    output_current = models.CharField(max_length=20, blank=True, verbose_name='输出电流')
    power = models.CharField(max_length=20, blank=True, verbose_name='功率')
    frequency_range = models.CharField(max_length=50, blank=True, verbose_name='频率范围')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')

    class Meta:
        db_table = 'inverter_parameters'
        verbose_name = '变频器参数'
        verbose_name_plural = '变频器参数'


# ==================== 质量管理 ====================

class CommonDefect(models.Model):
    defect_code = models.CharField(max_length=50, verbose_name='缺陷编码')
    defect_name = models.CharField(max_length=100, verbose_name='缺陷名称')
    description = models.CharField(max_length=255, blank=True, verbose_name='缺陷描述')
    severity = models.CharField(max_length=20, blank=True, verbose_name='严重程度')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'common_defects'
        verbose_name = '常见缺陷'
        verbose_name_plural = '常见缺陷'

    def __str__(self):
        return self.defect_name


class InspectionItem(models.Model):
    item_code = models.CharField(max_length=50, verbose_name='检测项编码')
    item_name = models.CharField(max_length=100, verbose_name='检测项名称')
    description = models.CharField(max_length=255, blank=True, verbose_name='检测项描述')
    standard = models.CharField(max_length=255, blank=True, verbose_name='检测标准')
    method = models.CharField(max_length=255, blank=True, verbose_name='检测方法')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'inspection_items'
        verbose_name = '检测项'
        verbose_name_plural = '检测项'

    def __str__(self):
        return self.item_name


class InspectionPlan(models.Model):
    plan_code = models.CharField(max_length=50, verbose_name='方案编码')
    plan_name = models.CharField(max_length=100, verbose_name='方案名称')
    inspection_type = models.CharField(max_length=50, verbose_name='检验类型(来料/过程/退料/出货)')
    product = models.ForeignKey(Product, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='产品')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'inspection_plans'
        verbose_name = '质检方案'
        verbose_name_plural = '质检方案'

    def __str__(self):
        return self.plan_name


class InspectionPlanItem(models.Model):
    plan = models.ForeignKey(InspectionPlan, on_delete=models.CASCADE, verbose_name='质检方案')
    item = models.ForeignKey(InspectionItem, on_delete=models.CASCADE, verbose_name='检测项')
    sequence = models.IntegerField(verbose_name='检测顺序')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')

    class Meta:
        db_table = 'inspection_plan_items'
        verbose_name = '质检方案检测项'
        verbose_name_plural = '质检方案检测项'


class InspectionTask(models.Model):
    task_code = models.CharField(max_length=50, verbose_name='任务编码')
    inspection_type = models.CharField(max_length=50, verbose_name='检验类型')
    plan = models.ForeignKey(InspectionPlan, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='质检方案')
    product = models.ForeignKey(Product, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='产品')
    batch_number = models.CharField(max_length=100, blank=True, verbose_name='批次号')
    quantity = models.IntegerField(verbose_name='检验数量')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    assignee = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='指派人员')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'inspection_tasks'
        verbose_name = '待检任务'
        verbose_name_plural = '待检任务'

    def __str__(self):
        return self.task_code


class IncomingInspection(models.Model):
    task = models.ForeignKey(InspectionTask, on_delete=models.CASCADE, verbose_name='待检任务')
    supplier = models.ForeignKey(Customer, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='供应商')
    material_code = models.CharField(max_length=50, blank=True, verbose_name='物料编码')
    batch_number = models.CharField(max_length=100, blank=True, verbose_name='批次号')
    inspection_result = models.CharField(max_length=20, blank=True, verbose_name='检验结果')
    inspector = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='检验员')
    inspection_time = models.DateTimeField(blank=True, null=True, verbose_name='检验时间')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')

    class Meta:
        db_table = 'incoming_inspections'
        verbose_name = '来料检验'
        verbose_name_plural = '来料检验'


class ProcessInspection(models.Model):
    task = models.ForeignKey(InspectionTask, on_delete=models.CASCADE, verbose_name='待检任务')
    work_order = models.ForeignKey(WorkOrder, on_delete=models.CASCADE, verbose_name='工单')
    process = models.ForeignKey(Process, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工序')
    batch_number = models.CharField(max_length=100, blank=True, verbose_name='批次号')
    inspection_result = models.CharField(max_length=20, blank=True, verbose_name='检验结果')
    inspector = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='检验员')
    inspection_time = models.DateTimeField(blank=True, null=True, verbose_name='检验时间')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')

    class Meta:
        db_table = 'process_inspections'
        verbose_name = '过程检验'
        verbose_name_plural = '过程检验'


class ReturnInspection(models.Model):
    task = models.ForeignKey(InspectionTask, on_delete=models.CASCADE, verbose_name='待检任务')
    material_code = models.CharField(max_length=50, blank=True, verbose_name='物料编码')
    batch_number = models.CharField(max_length=100, blank=True, verbose_name='批次号')
    return_reason = models.CharField(max_length=255, blank=True, verbose_name='退料原因')
    inspection_result = models.CharField(max_length=20, blank=True, verbose_name='检验结果')
    inspector = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='检验员')
    inspection_time = models.DateTimeField(blank=True, null=True, verbose_name='检验时间')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')

    class Meta:
        db_table = 'return_inspections'
        verbose_name = '退料检验'
        verbose_name_plural = '退料检验'


class ShippingInspection(models.Model):
    task = models.ForeignKey(InspectionTask, on_delete=models.CASCADE, verbose_name='待检任务')
    order = models.ForeignKey(WorkOrder, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='订单(工单)')  # 关联工单作为订单
    product = models.ForeignKey(Product, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='产品')
    batch_number = models.CharField(max_length=100, blank=True, verbose_name='批次号')
    quantity = models.IntegerField(verbose_name='检验数量')
    inspection_result = models.CharField(max_length=20, blank=True, verbose_name='检验结果')
    inspector = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='检验员')
    inspection_time = models.DateTimeField(blank=True, null=True, verbose_name='检验时间')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')

    class Meta:
        db_table = 'shipping_inspections'
        verbose_name = '出货检验'
        verbose_name_plural = '出货检验'


# ==================== 批次追溯 ====================

class BatchTracking(models.Model):
    batch_number = models.CharField(max_length=100, verbose_name='批次号')
    product = models.ForeignKey(Product, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='产品')
    quantity = models.IntegerField(verbose_name='数量')
    production_date = models.DateField(blank=True, null=True, verbose_name='生产日期')
    expiry_date = models.DateField(blank=True, null=True, verbose_name='有效期至')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'batch_tracking'
        verbose_name = '批次追溯'
        verbose_name_plural = '批次追溯'

    def __str__(self):
        return self.batch_number


# ==================== 物料管理（补充） ====================

class Material(models.Model):
    material_code = models.CharField(max_length=50, verbose_name='物料编码')
    material_name = models.CharField(max_length=100, verbose_name='物料名称')
    specification = models.CharField(max_length=255, blank=True, verbose_name='规格型号')
    unit = models.CharField(max_length=20, blank=True, verbose_name='单位')
    safety_stock = models.IntegerField(blank=True, null=True, verbose_name='安全库存')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    status = models.CharField(max_length=20, blank=True, verbose_name='状态')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'materials'
        verbose_name = '物料'
        verbose_name_plural = '物料'

    def __str__(self):
        return self.material_name


class Inventory(models.Model):
    material = models.ForeignKey(Material, on_delete=models.CASCADE, verbose_name='物料')
    quantity = models.IntegerField(verbose_name='库存数量')
    batch_number = models.CharField(max_length=100, blank=True, verbose_name='批次号')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    location = models.CharField(max_length=100, blank=True, verbose_name='库位')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'inventory'
        verbose_name = '库存'
        verbose_name_plural = '库存'


class MaterialConsumption(models.Model):
    work_order = models.ForeignKey(WorkOrder, on_delete=models.CASCADE, verbose_name='工单')
    material = models.ForeignKey(Material, on_delete=models.CASCADE, verbose_name='物料')
    quantity = models.IntegerField(verbose_name='消耗数量')
    batch_number = models.CharField(max_length=100, blank=True, verbose_name='批次号')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    consumption_time = models.DateTimeField(auto_now_add=True, verbose_name='消耗时间')
    operator = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='操作人')
    remark = models.TextField(blank=True, verbose_name='备注')

    class Meta:
        db_table = 'material_consumption'
        verbose_name = '物料消耗'
        verbose_name_plural = '物料消耗'


# ==================== 条码管理 ====================

class BarcodeRelation(models.Model):
    barcode = models.CharField(max_length=100, unique=True, verbose_name='条码')
    product = models.ForeignKey(Product, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='产品')
    work_order = models.ForeignKey(WorkOrder, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工单')
    batch_number = models.CharField(max_length=100, blank=True, verbose_name='批次号')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')

    class Meta:
        db_table = 'barcode_relations'
        verbose_name = '条码关系'
        verbose_name_plural = '条码关系'


class BarcodeScan(models.Model):
    barcode = models.CharField(max_length=100, verbose_name='条码')
    scan_time = models.DateTimeField(auto_now_add=True, verbose_name='扫描时间')
    operator = models.ForeignKey(Employee, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='操作人')
    action = models.CharField(max_length=50, blank=True, verbose_name='操作类型')
    factory = models.ForeignKey(Factory, on_delete=models.SET_NULL, null=True, blank=True, verbose_name='工厂')
    remark = models.TextField(blank=True, verbose_name='备注')

    class Meta:
        db_table = 'barcode_scans'
        verbose_name = '条码扫描记录'
        verbose_name_plural = '条码扫描记录'