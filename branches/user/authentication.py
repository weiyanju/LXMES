from rest_framework_simplejwt.authentication import JWTAuthentication
from .models import SysUser

class SysUserJWTAuthentication(JWTAuthentication):
    def get_user(self, validated_token):
        """
        根据 token 中的 user_id 从 SysUser 表中获取用户
        """
        user_id = validated_token.get('user_id')
        if not user_id:
            return None
        try:
            return SysUser.objects.get(id=user_id)
        except SysUser.DoesNotExist:
            return None