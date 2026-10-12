/* ============================================================
   PUENTE ENTRE LA PANTALLA (HTML) Y C#
   - Puente.enviar({...})  manda un mensaje a C#
   - Puente.escuchar(fn)   recibe la respuesta de C#

   Si abres el .html en un navegador normal (sin C#), funciona en
   "modo vista previa" con respuestas de mentira, para que puedas
   diseñar y ver cambios sin ejecutar el programa.
   ============================================================ */
const enWebView = !!(window.chrome && window.chrome.webview);

const Puente = {
  _fn: null,

  enviar(mensaje) {
    if (enWebView) { window.chrome.webview.postMessage(mensaje); return; }

    setTimeout(() => {
      if (mensaje.accion === 'login') {
        this._fn?.({ accion: 'login', ok: mensaje.clave === '1234',
                     mensaje: 'Usuario, contraseña o rol incorrectos. (vista previa: la clave es 1234)' });
      }
      if (mensaje.accion === 'sesion') this._fn?.({ accion: 'sesion', usuario: 'demo', rol: 'Administrador' });
      if (mensaje.accion === 'logout') location.href = 'login.html';
    }, 150);
  },

  escuchar(fn) {
    if (enWebView) window.chrome.webview.addEventListener('message', e => fn(e.data));
    else this._fn = fn;
  }
};
