import random
from django.core.management.base import BaseCommand
from django.utils import timezone
from core.models import Factory, Customer, Department, Employee, User

class Command(BaseCommand):
    help = '生成 core 模块的测试数据'

    def handle(self, *args, **options):
        self.stdout.write('开始生成测试数据...')

        # 1. 创建工厂
        factories = []
        factory_data = [
            {'code': 'SH001', 'name': '上海总厂', 'address': '上海市浦东新区', 'status': 'active'},
            {'code': 'BJ002', 'name': '北京分厂', 'address': '北京市大兴区', 'status': 'active'},
            {'code': 'GZ003', 'name': '广州分厂', 'address': '广州市黄埔区', 'status': 'inactive'},
        ]
        for fd in factory_data:
            factory, created = Factory.objects.get_or_create(
                factory_code=fd['code'],
                defaults={
                    'factory_name': fd['name'],
                    'address': fd['address'],
                    'status': fd['status']
                }
            )
            factories.append(factory)
            if created:
                self.stdout.write(f'  创建工厂: {factory.factory_name}')
        self.stdout.write(f'工厂数据完成，共 {len(factories)} 条')

        # 2. 创建部门（关联工厂）
        departments = []
        dept_names = ['总经办', '生产部', '质量部', '技术部', '销售部', '采购部', '仓储部']
        for factory in factories:
            for i, name in enumerate(dept_names):
                dept_code = f'{factory.factory_code}_D{i+1:02d}'
                dept, created = Department.objects.get_or_create(
                    department_code=dept_code,
                    defaults={
                        'department_name': f'{factory.factory_name}-{name}',
                        'factory': factory,
                        'status': 'active'
                    }
                )
                departments.append(dept)
                if created:
                    self.stdout.write(f'  创建部门: {dept.department_name}')
        self.stdout.write(f'部门数据完成，共 {len(departments)} 条')

        # 3. 创建客户
        customers = []
        customer_data = [
            {'code': 'KH001', 'name': '华为技术', 'contact': '王经理', 'phone': '13800000001'},
            {'code': 'KH002', 'name': '比亚迪汽车', 'contact': '李总', 'phone': '13800000002'},
            {'code': 'KH003', 'name': '宁德时代', 'contact': '张工', 'phone': '13800000003'},
            {'code': 'KH004', 'name': '富士康', 'contact': '陈经理', 'phone': '13800000004'},
        ]
        for cd in customer_data:
            cust, created = Customer.objects.get_or_create(
                customer_code=cd['code'],
                defaults={
                    'customer_name': cd['name'],
                    'contact_person': cd['contact'],
                    'contact_phone': cd['phone'],
                    'status': 'active'
                }
            )
            customers.append(cust)
            if created:
                self.stdout.write(f'  创建客户: {cust.customer_name}')
        self.stdout.write(f'客户数据完成，共 {len(customers)} 条')

        # 4. 创建员工（关联工厂和部门）
        employees = []
        surnames = ['张', '王', '李', '刘', '陈', '杨', '赵', '黄', '周', '吴']
        names = ['伟', '芳', '娜', '强', '涛', '敏', '静', '磊', '洋', '杰']
        positions = ['经理', '主管', '工程师', '专员', '操作工', '质检员', '技术员']
        for factory in factories:
            # 获取该工厂的部门列表
            factory_depts = Department.objects.filter(factory=factory)
            if not factory_depts.exists():
                continue
            for i in range(15):  # 每个工厂生成15名员工
                surname = random.choice(surnames)
                name = random.choice(names)
                full_name = surname + name + (random.choice(['', '']) + random.choice(names) if random.random() > 0.5 else '')
                emp_code = f'EMP_{factory.factory_code}_{i+1:03d}'
                dept = random.choice(factory_depts)
                position = random.choice(positions)
                emp, created = Employee.objects.get_or_create(
                    employee_code=emp_code,
                    defaults={
                        'name': full_name,
                        'factory': factory,
                        'department': dept,
                        'position': position,
                        'phone': f'139{random.randint(10000000,99999999)}',
                        'status': random.choice(['active', 'active', 'active', 'inactive'])  # 大多数活跃
                    }
                )
                employees.append(emp)
                if created:
                    self.stdout.write(f'  创建员工: {emp.name} ({emp.employee_code})')
        self.stdout.write(f'员工数据完成，共 {len(employees)} 条')

        # 5. 创建用户（关联工厂）
        users = []
        user_data = [
            {'username': 'admin', 'name': '系统管理员', 'role': 'admin', 'factory_index': 0},
            {'username': 'sh_manager', 'name': '上海厂长', 'role': 'manager', 'factory_index': 0},
            {'username': 'bj_manager', 'name': '北京厂长', 'role': 'manager', 'factory_index': 1},
            {'username': 'gz_manager', 'name': '广州厂长', 'role': 'manager', 'factory_index': 2},
            {'username': 'operator1', 'name': '操作员甲', 'role': 'operator', 'factory_index': 0},
            {'username': 'operator2', 'name': '操作员乙', 'role': 'operator', 'factory_index': 1},
        ]
        for ud in user_data:
            factory = factories[ud['factory_index']]
            user, created = User.objects.get_or_create(
                username=ud['username'],
                defaults={
                    'name': ud['name'],
                    'role': ud['role'],
                    'factory': factory,
                    'department': '管理部' if ud['role'] in ['admin','manager'] else '生产部',
                    'email': f"{ud['username']}@example.com",
                    'is_active': True,
                    'is_staff': ud['role'] == 'admin',
                    'is_superuser': ud['role'] == 'admin'
                }
            )
            if created:
                user.set_password('123456')  # 统一设置密码为123456
                user.save()
                users.append(user)
                self.stdout.write(f'  创建用户: {user.username} (密码: 123456)')
        self.stdout.write(f'用户数据完成，共 {len(users)} 条')

        self.stdout.write(self.style.SUCCESS('测试数据生成完毕！'))