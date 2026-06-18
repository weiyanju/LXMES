from django.http import HttpResponse
from django.utils.deprecation import MiddlewareMixin
from rest_framework_simplejwt.exceptions import TokenError, InvalidToken
from rest_framework_simplejwt.tokens import UntypedToken


class JwtAuthenticationMiddleware(MiddlewareMixin):

    def process_request(self, request):
        white_list = ["/user/login"]  # 请求白名单
        path = request.path
        if path not in white_list and not path.startswith("/media"):
            print("要进行token验证")
            auth_header = request.META.get('HTTP_AUTHORIZATION')
            if not auth_header:
                return HttpResponse('请先登录！', status=401)

            try:
                token_type, token = auth_header.split()
                if token_type.lower() != 'bearer':
                    raise ValueError("Token类型必须为Bearer")
            except ValueError:
                token = auth_header

            print("token:", token)

            try:
                UntypedToken(token)
            except TokenError as e:
                if "expired" in str(e).lower():
                    return HttpResponse('Token过期，请重新登录！', status=401)
                return HttpResponse(f'Token验证失败：{str(e)}', status=401)
            except Exception as e:
                return HttpResponse(f'Token验证异常：{str(e)}', status=401)
        else:
            print("不需要token验证")
            return None