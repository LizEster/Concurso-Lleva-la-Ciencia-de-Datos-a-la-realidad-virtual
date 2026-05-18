# Concurso-Lleva-la-Ciencia-de-Datos-a-la-realidad-virtual
# Sed Algorítmica: El Laberinto Matricial

Proyecto desarrollado en Unity para crear una experiencia inmersiva estilo laberinto VR/FPS enfocada en la ética de la inteligencia artificial y el consumo de recursos.

---

# Requisitos

## Unity

Todos los integrantes deben usar EXACTAMENTE esta versión:

```text
Unity 6000.4.7f1
```

Instalar desde Unity Hub.

---

# Clonar el proyecto

```bash
git clone https://github.com/LizEster/Concurso-Lleva-la-Ciencia-de-Datos-a-la-realidad-virtual.git
```

Luego abrir Unity Hub:

```text
Add Project → seleccionar carpeta clonada
```

---

# Assets necesarios

Cada integrante debe descargar/importar estos assets manualmente desde Unity Asset Store.

## 1. Starter Assets FPS

Buscar:

```text
Starter Assets - First Person Character Controller
```

Autor:

```text
Unity Technologies
```

Se usa para:

* movimiento FPS
* cámara
* salto
* controles

---

## 2. Sci-Fi Kit

Buscar/importar:

```text
3D Scifi Kit Starter Kit
```

Se usa para:

* paredes
* pisos
* ambiente sci-fi
* laberinto

---

# Configuración importante

## Input System

Ir a:

```text
Edit → Project Settings → Player
```

Cambiar:

```text
Active Input Handling
```

a:

```text
Both
```

Luego reiniciar Unity.

---

# Estructura básica del proyecto

```text
Assets/
├── Scenes/
├── Scripts/
├── Materials/
├── Prefabs/
├── Audio/
├── UI/
```

---

# Reglas del proyecto

## NO subir a GitHub:

* Library/
* Temp/
* Logs/
* Obj/
* Build/
* archivos .unitypackage
* texturas gigantes .tif

Los assets grandes deben:

* descargarse desde Asset Store
  o
* compartirse por Drive.

---

# Flujo de trabajo Git

## Obtener cambios

```bash
git pull
```

## Subir cambios

```bash
git add .
git commit -m "mensaje"
git push origin main
```

---

# Orden recomendado de desarrollo

1. Movimiento FPS
2. Construcción del laberinto
3. Sistema de puertas
4. Barra de agua
5. Interacciones
6. Audio ambiental
7. Efectos visuales
8. Integración VR

---

# Concepto del juego

El jugador queda atrapado dentro de una infraestructura algorítmica representada como un laberinto de servidores.

Cada vez que utiliza ayuda de la IA para resolver decisiones, consume agua utilizada para refrigerar servidores reales.

El objetivo es escapar administrando correctamente los recursos y usando lógica propia en vez de depender completamente de la IA.

---

