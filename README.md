# Cámara de Gesell — Simulador clínico (UTPL XRlab)

Simulador en Unity 6 de una Cámara de Gesell para estudiantes de Psicología de la UTPL. El estudiante inicia sesión con su cuenta institucional, recorre el laboratorio y observa una entrevista clínica en tres fases (Inicial, Desarrollo y Final) de uno de dos casos (**Caso 1** o **Caso 4**, elegido al azar). Durante la entrevista responde preguntas, completa la ficha de criterios DSM‑5‑TR y puntúa el Inventario de Beck.

| Dato | Valor |
|---|---|
| Motor | Unity **6000.3.18f1** |
| Escenas en build | `Assets/Scenes/Iniciar sesion.unity` (0) → `Assets/Scenes/SampleScene.unity` (1) |
| Login | `https://campus3d.utpl.edu.ec/virtopsia-admin/api/authentication` (`LoginLab/`) |
| Contenido clínico | API Express en Render `https://api-labpsicologia.onrender.com/api` respaldada por **Firebase Firestore** |
| Serialización | Force Text (YAML) · Visible Meta Files |
| Control de versiones | Git + Git LFS (ver `.gitattributes`) |

## Puesta en marcha

```bash
git lfs install
git clone https://github.com/Cristhian23456/V2-Camara-de-Hessel.git
cd V2-Camara-de-Hessel
git lfs pull
```
1. Abrir con Unity Hub, usando exactamente **6000.3.18f1**.
2. Pulsar Play. `Assets/Editor/PlayModeStartSceneSetter.cs` fuerza el inicio desde la escena de login.
3. Herramientas de mantenimiento: **Tools ▸ Limpieza Proyecto** (ver `CHANGELOG_LimpiezaAssets.md`).

## Estructura relevante

```
Assets/
  LoginLab/            Login institucional (protegido: no modificar sin coordinación)
  Scripts/legacy/      Lógica en uso: ApiManager, DialogosManager, FichaDiagnostico, BeckInventory…
  Scripts/core|Systems|UI/  Arquitectura nueva SIN conectar a escenas (ver M9)
  Resources/           Audios de preguntas/retroalimentación y video del tutor (se cargan por ruta)
  Editor/              PlayModeStartSceneSetter, herramienta de limpieza
```

