const express = require('express')
const fallback = require('express-history-api-fallback')
const { createProxyMiddleware } = require('http-proxy-middleware')

const LISTEN_PORT = process.env.LISTEN_PORT
const LISTEN_ADDRESS = process.env.LISTEN_ADDRESS
const MIND_ADDRESS = process.env.MIND_ADDRESS
const STORAGE_ADDRESS = process.env.STORAGE_ADDRESS
const WEBAPP_PUBLIC = process.env.WEBAPP_PUBLIC

const app = express()

const mindProxy = createProxyMiddleware('/mind', { target: MIND_ADDRESS, ws: true, pathRewrite: { '^/mind': '/' } })
const storageProxy = createProxyMiddleware('/storage', { target: STORAGE_ADDRESS, ws: true, pathRewrite: { '^/storage': '/' } })

app.use(mindProxy)
app.use(storageProxy)
app.use(express.static(WEBAPP_PUBLIC))
app.use(fallback('/', { root: WEBAPP_PUBLIC }))

app.listen(LISTEN_PORT, LISTEN_ADDRESS, () => {
    console.log(`Listening on ${LISTEN_ADDRESS}:${LISTEN_PORT}`)
})
