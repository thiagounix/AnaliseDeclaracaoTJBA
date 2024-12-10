const { env } = require('process');

const PROXY_CONFIG = [
  {
    context: ["/api"],
    target: "https://localhost:7022", // HTTPS (prioritário)
    secure: false, // Ignora certificados autoassinados
    changeOrigin: true,
    logLevel: "debug", // Para maior depuração
    onError: (err, req, res) => {
      console.error('Erro no proxy HTTPS:', err.message);
      res.writeHead(500, { 'Content-Type': 'text/plain' });
      res.end('Erro ao redirecionar para HTTPS');
    }
  },
  {
    context: ["/api"],
    target: "http://localhost:5162", // HTTP (fallback)
    secure: false,
    changeOrigin: true,
    logLevel: "debug", // Para maior depuração
    onError: (err, req, res) => {
      console.error('Erro no proxy HTTP:', err.message);
      res.writeHead(500, { 'Content-Type': 'text/plain' });
      res.end('Erro ao redirecionar para HTTP');
    }
  }
];

module.exports = PROXY_CONFIG;
