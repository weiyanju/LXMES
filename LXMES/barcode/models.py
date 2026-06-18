from django.db import models


class BarcodeRecord(models.Model):
    code = models.CharField(max_length=255, verbose_name='条码内容')
    scan_time = models.DateTimeField(auto_now_add=True, verbose_name='扫描时间')
    remark = models.CharField(max_length=200, blank=True, verbose_name='备注')

    class Meta:
        db_table = 'barcode_record'
        verbose_name = '条码记录'
        verbose_name_plural = '条码记录'
        ordering = ['-scan_time']

    def __str__(self):
        return f"{self.code} - {self.scan_time}"
