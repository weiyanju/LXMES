import time
import serial
from django.core.management.base import BaseCommand
from barcode.models import BarcodeRecord

class Command(BaseCommand):
    help = '监听 COM3 串口，读取条码并保存'

    def handle(self, *args, **options):
        # 配置串口参数（根据你的扫码枪说明书修改波特率等）
        ser = serial.Serial(
            port='COM3',           # 你的串口端口
            baudrate=115200,         # 波特率，常见9600或115200
            bytesize=8,
            parity='N',
            stopbits=1,
            timeout=1
        )
        self.stdout.write(self.style.SUCCESS('开始监听 COM3 串口...'))

        try:
            while True:
                if ser.in_waiting > 0:
                    line = ser.readline().decode('utf-8').strip()
                    if line:
                        BarcodeRecord.objects.create(code=line)
                        self.stdout.write(f'已保存条码: {line}')
                time.sleep(0.1)
        except KeyboardInterrupt:
            self.stdout.write(self.style.WARNING('监听已停止'))
        finally:
            ser.close()