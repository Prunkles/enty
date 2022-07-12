const path = require('path')
const webpack = require('webpack')
const NodePolyfillPlugin = require('node-polyfill-webpack-plugin')
const HtmlWebpackPlugin = require('html-webpack-plugin')
const CopyWebpackPlugin = require('copy-webpack-plugin')
const ReactRefreshWebpackPlugin = require('@pmmmwh/react-refresh-webpack-plugin');

const isProduction = process.env.NODE_ENV === 'production'
const isDevelopment = !isProduction

console.log("Bundling for " + (isProduction ? "production" : "development"))

const commonPlugins = [
    new NodePolyfillPlugin(),
    new HtmlWebpackPlugin({
        filename: 'index.html',
        template: resolve('./public/index.html'),
    }),
]

module.exports = {
    mode: isProduction ? 'production' : 'development',
    entry: {
        webapp: [ resolve('./build/Program.fs.js') ]
    },
    output: {
        path: resolve('./dist'),
        publicPath: '/',
        filename: isProduction ? '[name].[fullhash].js' : '[name].js'
    },
    devtool: isProduction ? 'source-map' : 'eval-source-map',
    optimization: {
        // Split the code coming from npm packages into a different file.
        // 3rd party dependencies change less often, let the browser cache them.
        splitChunks: {
            cacheGroups: {
                commons: {
                    test: /node_modules/,
                    name: "vendors",
                    chunks: "all"
                }
            }
        },
    },
    plugins:
        (isProduction ? [
            new CopyWebpackPlugin({
                patterns: [
                    './public/enty.svg',
                ]
            })
        ] : [
            new ReactRefreshWebpackPlugin()
        ]).concat(commonPlugins),
    resolve: {
        // See https://github.com/fable-compiler/Fable/issues/1490
        symlinks: false,
        modules: [ resolve('./node_modules') ]
    },
    devServer: {
        host: '0.0.0.0',
        port: 8080,
        historyApiFallback: true,
        hot: true,
        proxy: {
            '/mind/**': {
                target: 'http://localhost:' + '5010',
                changeOrigin: true,
                pathRewrite: {
                    '^/mind': '',
                },
            },
            '/image-thumbnail/**': {
                target: 'http://localhost:' + '9000',
                changeOrigin: true,
                pathRewrite: {
                    '^/image-thumbnail': '',
                },
            }
        },
        setupMiddlewares: (middlewares, devServer) => {
            devServer.app.get('/storage-address', (_, response) => {
                response.send('http://localhost:5020/')
            })
            return middlewares
        },
    },
    // - babel-loader: transforms JS to old syntax (compatible with old browsers)
    // - file-loader: Moves files referenced in the code (fonts, images) into output folder
    module: {
        rules: [
            // Use babel-preset-env to generate JS compatible with most-used browsers.
            // More info at https://babeljs.io/docs/en/next/babel-preset-env.html
            {
                test: /\.(js|jsx)&/,
                exclude: /node-modules/,
                use: {
                    loader: 'babel-loader',
                    options: {
                        plugins: [ isDevelopment && require('react-refresh/babel') ].filter(Boolean),
                        presets: [ '@babel/preset-react' ]
                    }
                }
            },
            {
                test: /\.(png|jpg|jpeg|gif|svg|woff|woff2|ttf|eot)(\?.*)?$/,
                use: ['file-loader']
            }
        ]
    }
}

function resolve(filePath) {
    return path.isAbsolute(filePath) ? filePath : path.join(__dirname, filePath)
}
