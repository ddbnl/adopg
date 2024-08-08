const path = require("path");
const fs = require("fs");
const CopyWebpackPlugin = require("copy-webpack-plugin");

// Webpack entry points. Mapping from resulting bundle name to the source file entry.
const entries = {};

// Loop through subfolders in the "Hub" folder and add an entry for each one
const hubDir = path.join(__dirname, "src/Hub");
fs.readdirSync(hubDir).filter(dir => {
    if (fs.statSync(path.join(hubDir, dir)).isDirectory()) {
        entries[dir] = "./" + path.relative(process.cwd(), path.join(hubDir, dir, dir));
    }
});

module.exports = (env, argv) => ({
    entry: "./src/Common.tsx",
    output: {
        filename: "policy-guard/[name].js",
    },
    resolve: {
        extensions: [".ts", ".tsx", ".js"],
        alias: {
            "azure-devops-extension-sdk": path.resolve("node_modules/azure-devops-extension-sdk")
        },
    },
    stats: {
        warnings: false
    },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                loader: "ts-loader"
            },
            {
                test: /\.scss$/,
                use: ["style-loader", "css-loader", "sass-loader"],
            },
            {
                test: /\.css$/,
                use: ["style-loader", "css-loader"],
            },
            {
                test: /\.(woff|woff2|eot|ttf|otf)$/, 
                type: 'asset/inline'
            },
            {
                test: /\.html$/, 
                type: 'asset/resource'
            },
            {
                test: /\.(jpe?g|png|gif|svg)$/i,
                loader: 'file-loader',
                options: {
                    name: '/public/icons/[name].[ext]'
                }
            }
        ]
    },
    plugins: [
        new CopyWebpackPlugin({
           patterns: [ 
               { from: "**/*.html", context: "src/Hub" }
           ]
        })
    ],
    ...(env.WEBPACK_SERVE
        ? {
              devtool: 'inline-source-map',
              devServer: {
                  liveReload: true,
                  server: 'https',
                  port: 3000,
              }
          }
        : {})
});
