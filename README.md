# TFG — Simulador Quirúrgico de Realidad Mixta

Sistema de guiado quirúrgico basado en realidad aumentada para la inserción de
tornillos canulados en fracturas de cuello femoral, desarrollado con **Unity 3D**,
**Vuforia Engine** y **MRTK** sobre **Microsoft HoloLens 2**.

El sistema se estructura en dos fases: una fase de **planificación virtual**, donde se
calcula la trayectoria óptima de inserción en configuración de triángulo invertido, y una
fase de **guiado en tiempo real**, con seguimiento de la herramienta quirúrgica y
retroalimentación continua del error de alineación.

Trabajo de Fin de Grado — Grado en Ingeniería Informática y Robótica.
Autor: Sergi Navarro

## Estructura del repositorio

| Carpeta | Contenido |
|---|---|
| `Scripts/` | Scripts C# del sistema (planificación, seguimiento, evaluación de métricas y registro de datos) |
| `Models/` | Modelos 3D del proyecto (fémur con fractura simulada y herramienta quirúrgica para impresión 3D) |

## Vídeo de demostración

[Ver demostración del sistema completo]([[https://www.youtube.com/watch?v=qpwyrqIn6Bw](https://www.youtube.com/watch?v=qpwyrqIn6Bw)](https://www.youtube.com/watch?v=qpwyrqIn6Bw))
