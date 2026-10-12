const form    = document.getElementById('form-login');
const usuario = document.getElementById('usuario');
const clave   = document.getElementById('clave');
const boton   = document.getElementById('btn-ingresar');
const mensaje = document.getElementById('mensaje');

// Mostrar / ocultar contraseña
document.getElementById('ver-clave').addEventListener('click', () => {
  clave.type = clave.type === 'password' ? 'text' : 'password';
});

// "¿Olvidó su clave?"
document.getElementById('olvido').addEventListener('click', e => {
  e.preventDefault();
  mensaje.textContent = 'Para restablecer su clave, comuníquese con el Administrador.';
});

// Enviar el formulario (también funciona con Enter)
form.addEventListener('submit', e => {
  e.preventDefault();
  mensaje.textContent = '';

  if (usuario.value.trim() === '' || clave.value === '') {
    mensaje.textContent = 'Complete usuario y contraseña.';
    return;
  }

  boton.disabled = true;
  Puente.enviar({
    accion:  'login',
    usuario: usuario.value.trim(),
    clave:   clave.value,
    rol:     form.elements['rol'].value
  });
});

// Respuesta de C#
Puente.escuchar(r => {
  if (r.accion !== 'login') return;
  boton.disabled = false;

  if (r.ok) {
    location.href = 'panel.html';
  } else {
    mensaje.textContent = r.mensaje;
    clave.value = '';
    clave.focus();
  }
});

usuario.focus();
