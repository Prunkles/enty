const express = require('express')
const { createProxyMiddleware } = require('http-proxy-middleware')

const PORT = process.env.PORT
const MIND_ADDRESS = process.env.MIND_ADDRESS
const STORAGE_ADDRESS = process.env.STORAGE_ADDRESS
const WEBAPP_PUBLIC = process.env.WEBAPP_PUBLIC

const app = express()

const mindProxy = createProxyMiddleware('/mind', { target: MIND_ADDRESS, ws: true, pathRewrite: { '^/mind': '/' } })
const storageProxy = createProxyMiddleware('/storage', { target: STORAGE_ADDRESS, ws: true, pathRewrite: { '^/storage': '/' } })

app.use(mindProxy)
app.use(storageProxy)
app.use(express.static(WEBAPP_PUBLIC))

app.listen(PORT, () => {
    console.log('Listening')
})
