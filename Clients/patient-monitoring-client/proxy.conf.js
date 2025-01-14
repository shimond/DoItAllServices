module.exports = {
  "/api": {
    ws:true,
    target:
      "http://localhost:7810",
    secure: process.env["NODE_ENV"] !== "development",
    pathRewrite: {
      "^/api": "",
    },
  },
};