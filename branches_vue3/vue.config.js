const webpack = require('webpack');

const path = require('path')
function resolve(dir) {
  return path.join(__dirname, dir)
}

module.exports = {
  lintOnSave: false,
    devServer: {
        host: '0.0.0.0',
        port: 8080,
        proxy: {
            // 这条暂时用不上，但留着无妨
            // '/api': {
            //     target: 'http://192.168.15.70:8000',   // 👈 IP 建议改成你实际测试通的 15.65
            //     changeOrigin: true,
            //     pathRewrite: { '^/api': '' }
            // },
            // '/media': {
            //     target: 'http://192.168.15.70:8000',
            //     changeOrigin: true
            // }
            // 👇 新增这一条，专门代理登录和用户相关接口
            // '/user': {
            //     target: 'http://192.168.15.65:8000',   // 同上
            //     changeOrigin: true
            //     // 不需要 pathRewrite，因为后端接口正是以 /user 开头
            // }
        }
    },
  chainWebpack(config) {
    // 设置 svg-sprite-loader
    // config 为 webpack 配置对象
    // config.module 表示创建一个具名规则，以后用来修改规则
    config.module
        // 规则
        .rule('svg')
        // 忽略
        .exclude.add(resolve('src/icons'))
        // 结束
        .end()
    // config.module 表示创建一个具名规则，以后用来修改规则
    config.module
        // 规则
        .rule('icons')
        // 正则，解析 .svg 格式文件
        .test(/\.svg$/)
        // 解析的文件
        .include.add(resolve('src/icons'))
        // 结束
        .end()
        // 新增了一个解析的loader
        .use('svg-sprite-loader')
        // 具体的loader
        .loader('svg-sprite-loader')
        // loader 的配置
        .options({
          symbolId: 'icon-[name]'
        })
        // 结束
        .end()
    config
        .plugin('ignore')
        .use(
            new webpack.ContextReplacementPlugin(/moment[/\\]locale$/, /zh-cn$/)
        )
    config.module
        .rule('icons')
        .test(/\.svg$/)
        .include.add(resolve('src/icons'))
        .end()
        .use('svg-sprite-loader')
        .loader('svg-sprite-loader')
        .options({
          symbolId: 'icon-[name]'
        })
        .end()
  }
}
