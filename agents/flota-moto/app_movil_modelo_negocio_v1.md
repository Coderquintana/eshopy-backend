# App movil y modelo de negocio digital v1

## Estado del documento
- Version: 1.0
- Fecha: 22/04/2026
- Uso: concepto de app movil para operar la flota y relacionarse con choferes

## Idea central
La app no debe ser solo un "extra lindo". Debe resolver control, soporte, evidencia y disciplina operativa. Si se hace bien, reduce friccion, baja desorden y deja trazabilidad.

---

## 1. Objetivo de la app

La app movil serviria para conectar al chofer con la empresa y digitalizar tareas que hoy, si se hacen por WhatsApp o papel, terminan desordenadas.

### La app deberia permitir

- onboarding de choferes;
- acceso individual por cuenta;
- reporte de kilometraje;
- reporte de incidentes;
- soporte operativo;
- consulta de pagos y estado de cuenta;
- agenda de revisiones;
- alertas y recordatorios;
- historial de la unidad asignada;
- evidencia fotografica.

---

## 2. Rol de la app dentro del modelo de negocio

La app no reemplaza Bolt. La app acompana la operacion de flota.

### Bolt resuelve

- viajes;
- pasajeros;
- ingresos de plataforma;
- calificacion dentro de la plataforma;
- comision;
- flujo operativo del viaje.

### La app de la empresa resuelve

- relacion empresa-chofer;
- control del activo;
- control de soporte;
- registro interno;
- reportes y evidencias;
- trazabilidad;
- liquidacion interna;
- cumplimiento del reglamento.

---

## 3. Usuario principal: el chofer

Cuando se contrata a un chofer nuevo se le crea una cuenta en la app con:

- usuario;
- clave temporal;
- estado de onboarding;
- moto asignada;
- documentos asociados;
- nivel de confianza;
- estado contractual.

### Primer acceso del chofer

En el primer login la app deberia obligar a:

1. cambiar la clave;
2. aceptar reglamento y contrato;
3. confirmar datos personales;
4. ver la moto asignada;
5. leer reglas basicas de uso;
6. cargar foto de perfil si hace falta.

---

## 4. Modulos principales para el chofer

### 4.1 Inicio

Pantalla resumen con:

- nombre del chofer;
- moto asignada;
- estado operativo;
- proximas tareas;
- alertas activas;
- saldo o liquidacion pendiente;
- proximas revisiones.

### 4.2 Mi moto

Deberia mostrar:

- marca y modelo;
- chapa;
- kilometraje registrado;
- proximo service;
- accesorios asignados;
- estado general;
- historial basico de mantenimiento.

### 4.3 Reportar kilometraje

El chofer deberia poder:

- cargar km actual;
- adjuntar foto del odometro;
- registrar inicio y fin de jornada si el modelo operativo lo exige;
- dejar observacion.

### 4.4 Reportar incidentes

La app deberia permitir reportar:

- accidente;
- caida;
- falla mecanica;
- multa;
- perdida de accesorio;
- robo o intento de robo;
- problema con pasajero;
- problema con documentacion.

Cada reporte deberia incluir:

- tipo de incidente;
- fecha y hora;
- ubicacion;
- descripcion;
- fotos o video;
- nivel de urgencia.

### 4.5 Soporte

Seccion para pedir ayuda por:

- tema mecanico;
- tema administrativo;
- tema de pago;
- tema documental;
- tema disciplinario;
- otro.

Cada solicitud deberia generar:

- numero de ticket;
- prioridad;
- responsable asignado;
- estado del caso.

### 4.6 Mis pagos

El chofer deberia poder ver:

- liquidacion semanal;
- pagos realizados;
- saldos pendientes;
- descuentos por danos o faltantes;
- comprobantes o historial.

### 4.7 Mi desempeno

La app deberia mostrar datos simples y utiles:

- calificacion en Bolt si se integra o se carga manualmente;
- cumplimiento de revisiones;
- nivel de confianza interno;
- alertas o advertencias;
- racha de semanas sin incidentes.

### 4.8 Documentos

Seccion con:

- contrato;
- reglamento;
- acta de entrega;
- cedula y licencia cargadas;
- vencimientos;
- archivos descargables.

---

## 5. Modulos para la empresa

La app no deberia pensarse solo desde el chofer. Tambien necesita panel o app para administradores.

### 5.1 Panel de operaciones

- ver motos activas;
- ver motos en taller;
- ver tickets abiertos;
- ver choferes en mora;
- ver revisiones pendientes;
- ver incidentes sin cerrar.

### 5.2 Panel de activos

- alta de moto;
- baja de moto;
- asignacion y reasignacion;
- historial de km;
- historial de mantenimiento;
- control de accesorios.

### 5.3 Panel de choferes

- alta de chofer;
- aprobacion de onboarding;
- bloqueo o suspension;
- nivel de confianza;
- documentos pendientes;
- historial disciplinario.

### 5.4 Panel financiero

- canon semanal;
- pagos cobrados;
- mora;
- descuentos;
- rentabilidad por unidad;
- exportacion de datos.

---

## 6. Flujo ideal de onboarding digital

### Paso 1

La empresa crea pre-registro del chofer.

### Paso 2

El chofer recibe acceso por:

- SMS: `[________________]`
- WhatsApp: `[________________]`
- Email: `[________________]`

### Paso 3

El chofer completa:

- datos personales;
- foto;
- licencia;
- cedula;
- antecedentes si aplica;
- aceptacion digital del reglamento.

### Paso 4

La empresa valida y aprueba.

### Paso 5

La app muestra:

- moto asignada;
- fecha de entrega;
- checklist de recepcion;
- boton para reportar primer km y primeras observaciones.

---

## 7. Alertas y automatizaciones que valen la pena

La app deberia empujar disciplina con alertas automaticas:

- recordatorio de revision semanal;
- recordatorio de pago;
- alerta de mora;
- alerta por vencimiento documental;
- alerta por km de service;
- alerta por falta de reporte de km;
- alerta por ticket sin respuesta;
- alerta por seguro proximo a vencer.

---

## 8. Ideas de negocio alrededor de la app

Si el negocio crece, la app deja de ser solo una herramienta interna y puede convertirse en un activo del negocio.

### Modelo 1 - Uso interno

La app sirve solo para operar la flota propia.

Ventajas:

- control total;
- menos complejidad comercial;
- desarrollo enfocado.

### Modelo 2 - SaaS para flotas pequenas

La app podria luego venderse o alquilarse a terceros que tengan motos, taxis o vehiculos de trabajo.

La app podria cobrar por:

- unidad activa por mes;
- chofer activo por mes;
- modulo premium;
- soporte o implementacion.

### Modelo 3 - App + operacion

La empresa podria combinar:

- ingresos por flota propia;
- ingresos por software;
- servicios de onboarding, mantenimiento y control para terceros.

---

## 9. Funciones que pueden dar ventaja competitiva

Estas funciones no son obligatorias en una v1, pero si pueden diferenciar:

- check-in con foto obligatoria al inicio y fin de semana;
- scoring interno del chofer;
- geolocalizacion de incidentes;
- biblioteca de tutoriales;
- firma digital de recepcion;
- boton de emergencia;
- encuesta de estado de moto;
- ranking de cumplimiento;
- historial de sanciones y reconocimientos;
- soporte con SLA visible.

---

## 10. Datos que conviene guardar

### Datos del chofer

- nombre;
- cedula;
- telefono;
- licencia;
- documentos;
- historial operativo;
- estado contractual.

### Datos de la moto

- identidad de unidad;
- km historico;
- servicios;
- accidentes;
- accesorios;
- seguro;
- fotos historicas.

### Datos operativos

- tickets;
- pagos;
- mora;
- revisiones;
- eventos;
- evidencia multimedia.

---

## 11. Reglas de permisos

No todos deberian ver todo.

### Chofer

Puede ver:

- su moto;
- sus pagos;
- sus tickets;
- sus documentos;
- su nivel de cumplimiento.

### Operaciones

Puede ver:

- todas las motos;
- todos los tickets;
- incidentes;
- revisiones;
- asignaciones.

### Finanzas

Puede ver:

- pagos;
- liquidaciones;
- mora;
- descuentos.

### Direccion

Puede ver:

- resumen general;
- rentabilidad;
- incidentes criticos;
- estado de flota.

---

## 12. MVP recomendado

No conviene empezar con una app gigantesca. Una v1 seria de verdad podria incluir solo esto:

- login;
- perfil de chofer;
- moto asignada;
- carga de km con foto;
- ticket de soporte;
- agenda de revision;
- consulta de liquidacion;
- carga de incidentes;
- panel admin basico.

### Lo que puede esperar a v2

- integraciones automaticas con Bolt;
- scoring avanzado;
- GPS;
- notificaciones inteligentes;
- firma avanzada;
- dashboards complejos.

---

## 13. Preguntas clave antes de desarrollarla

- sera app nativa o web app;
- quien la desarrolla;
- cuanto presupuesto hay;
- quien la mantiene;
- como se autentica el chofer;
- como se resguarda la evidencia;
- como se integra con caja, excel o sistema contable;
- si habra integracion real con Bolt o carga manual;
- quien modera tickets y en cuanto tiempo.

---

## 14. Riesgos de la app

- desarrollar demasiado antes de validar el negocio;
- cargar demasiadas funciones que nadie usa;
- no tener a nadie respondiendo tickets;
- datos mal cargados;
- evidencia sin orden;
- fuga de informacion;
- choferes sin disciplina digital.

### Mitigacion

- empezar con MVP;
- medir adopcion;
- mantener interfaz simple;
- obligar ciertos flujos minimos;
- definir responsable claro de soporte y datos.

---

## 15. Temas faltantes que conviene pensar

Ademas del concepto general, faltaria definir:

- nombre de la app: `[________________]`
- presupuesto maximo de desarrollo: `Gs. [________________]`
- tecnologia preferida: `[________________]`
- proveedor o equipo de desarrollo: `[________________]`
- tiempo objetivo de MVP: `[________________]`
- responsable interno del producto: `[________________]`
- si habra panel web adicional: `[Si / No]`
- politica de privacidad y terminos de uso: `[________________]`

---

## 16. Propuesta de estructura de pantallas

| Pantalla | Objetivo |
|---|---|
| Login | acceso seguro |
| Inicio | resumen operativo del chofer |
| Mi moto | ver datos de unidad y proximos mantenimientos |
| Reportar km | registrar km con foto |
| Incidentes | abrir reporte con evidencia |
| Soporte | crear ticket y ver estado |
| Pagos | consultar liquidacion e historial |
| Documentos | consultar contrato y vencimientos |
| Notificaciones | ver alertas y recordatorios |

---

## 17. Vision a futuro

Si la flota funciona, la app puede convertirse en la columna vertebral de la operacion:

- menos WhatsApp desordenado;
- menos papel;
- mejor evidencia;
- mejor disciplina;
- mejor trazabilidad;
- mas facilidad para escalar.

La app solo tiene sentido si ayuda a controlar un negocio real. Primero valida la operacion. Despues digitaliza lo que mas friccion genera.

