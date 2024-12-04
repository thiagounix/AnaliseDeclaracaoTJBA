const { env } = require('process');

const PROXY_CONFIG = [
  {
    context: ["/api"],
    target: "https://localhost:7022", // HTTPS (padrão preferido)
    secure: false, // Ignora certificados autoassinados
    changeOrigin: true
  },
  {
    context: ["/api"],
    target: "http://localhost:5162", // HTTP
    secure: false,
    changeOrigin: true
  }
];

module.exports = PROXY_CONFIG;

