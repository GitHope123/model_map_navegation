# 🗺️ Sistema de Navegación y Evaluación Espacial 3D (UNDC)

> **Estado del Proyecto:** 🚧 En Desarrollo Activo (Alpha)
>
> Este proyecto es parte de una investigación académica para evaluar la eficiencia y autonomía en la navegación de entornos virtuales.

[![Unity](https://img.shields.io/badge/Unity-2022%2B-black?style=flat&logo=unity)](https://unity.com/)
[![Firebase Hosting](https://img.shields.io/badge/Firebase-Hosting-orange?style=flat&logo=firebase)](https://firebase.google.com/)
[![Status](https://img.shields.io/website?url=https%3A%2F%2Fundcnav.web.app&label=Live%20Demo&up_message=Online&down_message=Offline)](https://undcnav.web.app)

## 🔗 Demo en Vivo
Puedes probar la versión más reciente del sistema directamente en tu navegador (PC/Laptop recomendado):
👉 **[https://undcnav.web.app](https://undcnav.web.app)**

---

## 📋 Descripción
Este simulador 3D permite a los usuarios navegar por un modelo digital de la universidad (UNDC), guiándolos a través de rutas específicas mientras el sistema recopila métricas de rendimiento en tiempo real.

### 🎯 Objetivos Principales
1.  **Evaluación de Espacialidad:** Medir la capacidad del usuario para orientarse.
2.  **Recolección de Datos:** Monitoreo de:
    *   Tiempo de recorrido.
    *   Desviaciones de la ruta óptima.
    *   Intervenciones de ayuda solicitadas.
    *   Distancia recorrida vs. Distancia óptima.

---

## 🎮 Controles
El sistema está diseñado para ser intuitivo, utilizando el estándar de juegos en primera persona (FPS):

| Acción | Tecla / Control |
| :--- | :--- |
| **Moverse** | `W`, `A`, `S`, `D` o Flechas Direccionales |
| **Mirar / Girar** | Movimiento del `Mouse` |
| **Interactuar / UI** | `Click Izquierdo` |
| **Menu / Pausa** | `Esc` |

---

## 🛠️ Tecnologías Utilizadas

*   **Motor Gráfico:** Unity (C#)
*   **Backend / Base de Datos:** Supabase (Gestión de usuarios y registros de sesiones).
*   **Hosting & Despliegue:** Firebase Hosting.
*   **CI/CD:** GitHub Actions (Despliegue automático al actualizar la rama `main`).
*   **Navegación:** Unity NavMesh Agent + LineRenderer para trazado de rutas dinámicas.

---

## 🚀 Hoja de Ruta (Roadmap)
Este proyecto está en constante evolución. Las próximas actualizaciones incluirán:

- [ ] **Optimización de Rendimiento:** Mejora en el cálculo de rutas (`RouteDrawer`) para dispositivos de gama baja.
- [ ] **Flecha Guía 3D:** Implementación de un indicador flotante en el entorno en lugar de solo en la UI 2D.
- [ ] **Minimapa:** Visualización top-down de la posición del usuario en tiempo real.
- [ ] **Refinamiento de UI:** Mejorar la estética del panel de login y las notificaciones de éxito.
- [ ] **Soporte Móvil:** Implementación de joysticks virtuales para compatibilidad con Android/iOS.

---

## 💻 Instalación y Desarrollo (Local)

Si deseas clonar y editar este proyecto:

1.  **Clonar el repositorio:**
    ```bash
    git clone https://github.com/GitHope123/model_map_navegation.git
    ```
2.  **Abrir en Unity:**
    *   Usa Unity Hub para abrir la carpeta del proyecto.
    *   Asegúrate de tener instalada una versión compatible de Unity.
3.  **Configuración:**
    *   El proyecto requiere conexión a internet para comunicar con Supabase.

---

## 📄 Licencia
Este proyecto es de uso académico y privado.
