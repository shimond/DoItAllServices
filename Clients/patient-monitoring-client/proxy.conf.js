module.exports = {
  "/api": {
    ws: true,
    target:
      process.env["services__webclientbffgateway__http__0"],
    secure: process.env["NODE_ENV"] !== "development",
    pathRewrite: {
      "^/api": "",
    },
  },
  '/v1/traces': {
    target: process.env["OTEL_EXPORTER_OTLP_ENDPOINT"],
    secure: process.env['NODE_ENV'] !== 'development',
    headers: parseHeaders(process.env['OTEL_EXPORTER_OTLP_HEADERS']),
  }
};

function getEnv() {
  var e = process.env['OTEL_EXPORTER_OTLP_ENDPOINT'];
  console.log('E', e);
  return e;
}
function parseHeaders(s) {
  const headers = s.split(','); // Split by comma
  const result = {};

  headers.forEach((header) => {
    const [key, value] = header.split('='); // Split by equal sign
    result[key.trim()] = value.trim(); // Add to the object, trimming spaces
  });

  return result;
}