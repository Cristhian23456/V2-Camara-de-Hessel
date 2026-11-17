# CHANGELOG — Mejoras del escenario

**Rama:** `feature/mejoras-escenario` · **Fecha:** 17/11/2026 · **Escena:** `Assets/Scenes/SampleScene.unity`

## Resumen
1. **Cámara en vivo**: el monitor de la sala de control muestra la sala de entrevista en tiempo real.
2. **Firma del consentimiento**: tras la animación del consentimiento, el estudiante lee el documento, dibuja su firma y acepta; la entrevista continúa solo después de firmar.
3. **Avisos de área restringida**: rótulo rojo en las puertas de Archivo, Consultorio 1 y Consultorio 2, y un aviso en pantalla al acercarse.

## Archivos nuevos

| Archivo | Qué hace |
|---|---|
| `Assets/Scripts/Escenario/CamaraEnVivo.cs` | Va en una cámara desactivada. La renderiza a mano en una `RenderTexture` a 20 FPS y **solo cuando la pantalla es visible**. Hace parpadear el indicador "REC". |
| `Assets/Scripts/Escenario/AvisoUI.cs` | Avisos tipo *toast* (Info, Advertencia, Restringido, Error). Crea su propio Canvas al primer uso y no bloquea clics. API: `AvisoUI.Mostrar(titulo, mensaje, tipo, duracion)`. |
| `Assets/Scripts/Escenario/AreaRestringida.cs` | Trigger frente a la puerta: muestra el aviso cuando entra el `Player` (con enfriamiento de 4 s). |
| `Assets/Scripts/Escenario/LienzoFirma.cs` | Área de dibujo (mouse o táctil) sobre una `Texture2D`. Cuenta los píxeles pintados para rechazar firmas vacías. |
| `Assets/Scripts/Escenario/ConsentimientoFirma.cs` | Documento de consentimiento con scroll, datos (estudiante, fecha, caso), lienzo de firma, casilla de aceptación y botones Limpiar / Firmar y continuar. Guarda la firma en PNG (`persistentDataPath/Firmas`). La interfaz se crea en tiempo de ejecución. |
| `Assets/Editor/Escenario/EscenarioSetup.cs` | Menú **Tools ▸ Escenario** para crear los objetos de escena. Todas las opciones son idempotentes: se pueden ejecutar varias veces sin duplicar nada. |
| `Assets/Materials/Escenario/RT_CamaraGesell.renderTexture` | 640×360, profundidad 16, MSAA ×2. |
| `Assets/Materials/Escenario/MAT_PantallaEnVivo.mat` | `Unlit/Texture` con la RenderTexture. |
| `Assets/Materials/Escenario/MAT_RotuloRestringido.mat` | `Unlit/Color` rojo `#C62828`. |

## Archivo modificado: `Assets/Scripts/legacy/DialogosManager.cs`
- Nuevo campo en el Inspector `requerirFirmaEstudiante` (por defecto **true**, en la cabecera "Consentimiento informado") y una bandera privada `pendienteFirmaConsentimiento`.
- `TriggerInformedConsent()`: activa la bandera antes de iniciar las corrutinas.
- `esperarAnimacion()`: después de `panel.SetActive(false)`, si la bandera está activa abre `ConsentimientoFirma` y espera con `WaitUntil` hasta que el estudiante firme. Recién entonces continúa con el diálogo siguiente.
- Si `requerirFirmaEstudiante = false`, el flujo queda exactamente igual que antes.

## Cambios en `SampleScene`
Todo queda agrupado bajo un objeto raíz nuevo, **`[MejorasEscenario]`**:
- `PantallaEnVivo_SalaControl`: un quad sobre la cara frontal del monitor `E7_PB.003` (0,69 × 0,39 m) más el texto `IndicadorEnVivo` ("REC EN VIVO · Sala de entrevista").
- `CamaraVigilancia_Gesell`: Camera (FOV 65, sin AudioListener, desactivada) + `CamaraEnVivo`. Posición (-15.85, 1.95, -3.55): vista lateral de terapeuta y paciente desde la pared del espejo.
- `AvisoRestringido_Archivo`, `AvisoRestringido_Consultorio1`, `AvisoRestringido_Consultorio2`: BoxCollider trigger de 0,9 × 2 × 0,8 m en el pasillo, `AreaRestringida` y un hijo `Rotulo` (placa de 0,40 × 0,20 m con el texto "ÁREA RESTRINGIDA / Solo personal autorizado").

No se modificó ningún objeto existente de la escena.

## Integración en el Editor
1. Abrir `SampleScene`.
2. **Tools ▸ Escenario ▸ Aplicar todo**. La consola muestra ✓ por cada elemento y la escena se guarda sola.
3. Ajustes opcionales:
   - **Encuadre de la cámara:** mover `CamaraVigilancia_Gesell` a mano. Al volver a ejecutar el menú se respeta su posición.
   - **FPS y resolución:** campo `fps` en `CamaraEnVivo` y tamaño de `RT_CamaraGesell`.
   - **Texto del consentimiento:** se edita en el campo `cuerpo`. Para eso, poner un objeto con `ConsentimientoFirma` en la escena; si no hay ninguno, se crea uno con el texto por defecto.
   - **Desactivar la firma:** desmarcar `requerirFirmaEstudiante` en `DialogosManager`.
   - **Otra área restringida:** agregarla al arreglo `AreasRestringidas` de `EscenarioSetup.cs`, o duplicar un `AvisoRestringido_*` y cambiar `nombreArea`.

## Pruebas realizadas (Play mode, Unity 6000.3.18f1)

| Prueba | Resultado |
|---|---|
| Compilación | 0 errores. Sin advertencias nuevas (se usa `textWrappingMode`, no la API obsoleta). |
| Jugador entra al trigger de Consultorio 1 | Aviso "Área restringida: Consultorio 1" arriba de la pantalla ✔ |
| Rótulos visibles desde el pasillo (3 puertas) | ✔ |
| Monitor de la sala de control | Muestra la sala de entrevista. Al mover al paciente, el cambio aparece en el monitor ✔ |
| `esperarAnimacion` con firma pendiente | Abre el documento. "Firmar y continuar" deshabilitado sin firma y sin casilla. Con firma (5868 px) y casilla marcada (clic en el texto) se habilita. Al firmar se cierra, se guarda el PNG y la corrutina continúa ✔ |
| Ejecutar el menú dos veces | Sin duplicados (5 objetos bajo la raíz) ✔ |

**Pendiente:** probar el flujo completo dentro de un caso real (login → Caso 1/4 → momento del consentimiento). Por ahora no se puede porque el backend devuelve 500 al cargar los diálogos.

## Pendientes y observaciones
- **Validar el texto del consentimiento** con los docentes de Psicología. El actual es un borrador genérico.
- La firma PNG se guarda en el equipo local y no se envía al backend. Si se requiere como evidencia, hay que crear un endpoint.
- Durante las pruebas apareció el aviso "2 audio listeners". Es el bug ya registrado; la cámara nueva no tiene AudioListener.
