const path = require('path')
const webpack = require('webpack')
const HtmlWebpackPlugin = require('html-webpack-plugin')
const CopyWebpackPlugin = require('copy-webpack-plugin')
const ReactRefreshWebpackPlugin = require('@pmmmwh/react-refresh-webpack-plugin');

// If we're running the webpack-dev-server, assume we're in development mode
const isProduction = 
    // !process.argv.find(v => v.indexOf('webpack-dev-server') !== -1)
    process.env.NODE_ENV === 'production'
const isDevelopment = !isProduction && process.env.NODE_ENV !== 'production'

const CONFIG = {
    fsharpEntry: './build/Program.js',
    assetsDir: './public',
    outputDir: './dist',
    indexHtmlTemplate: './src/enty.WebApp/index.html',
    devServer: {
        port: 8080,
        host: '0.0.0.0',
        proxy: {
            '/mind/**': {
                target: 'http://localhost:' + '5010',
                changeOrigin: true,
                pathRewrite: {
                    '^/mind': '',
                },
            },
            '/storage/**': {
                target: 'http://localhost:' + '5021',
                changeOrigin: true,
                pathRewrite: { '^/storage': '' },
            }
        }
    }
}

console.log("Bundling for " + (isProduction ? "production" : "development") + "...")

const commonPlugins = [
    new HtmlWebpackPlugin({
        filename: 'index.html',
        template: CONFIG.indexHtmlTemplate,
    }),
]

module.exports = {
    mode: isProduction ? 'production' : 'development',
    entry: {
        webapp: [ resolve(CONFIG.fsharpEntry) ]
    },
    output: {
        path: resolve(CONFIG.outputDir),
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
    plugins: commonPlugins.concat(
        isProduction ? [
            new CopyWebpackPlugin({
                patterns: [
                    { 
                        from: resolve(CONFIG.assetsDir),
                        noErrorOnMissing: true,
                    }
                ]
            })
        ] : [
            new ReactRefreshWebpackPlugin()
        ]
    ),
    resolve: {
        // See https://github.com/fable-compiler/Fable/issues/1490
        symlinks: false,
        modules: [ resolve('./node_modules') ]
    },
    devServer: {
        publicPath: '/',
        contentBase: resolve(CONFIG.assetsDir),
        host: CONFIG.devServer.host,
        port: CONFIG.devServer.port,
        hot: true,
        inline: true,
        proxy: CONFIG.devServer.proxy,
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
