# UnfathomableMirrors - Simulador Óptico 2D

UnfathomableMirrors es una aplicación interactiva de trazado de rayos (Raytracing) en dos dimensiones desarrollada en C# y WPF. El programa permite modelar y visualizar en tiempo real cómo se comportan los haces de luz al interactuar con diferentes componentes físicos y superficies de reflexión o refracción.

## 🚀 Funcionalidades

* Trazado de Rayos en Tiempo Real: Cálculo instantáneo de las trayectorias de los haces luminosos mediante un motor de físicas analítico interactivo con el mouse.
* Múltiples Fuentes de Luz: Soporte para añadir rayos simples, lámparas radiales omnidireccionales y haces de luz láser paralelos.
* Simulación de Espectro y Dispersión: Control de la longitud de onda de los rayos (en nanómetros) y simulación de luz blanca (espectro completo) mostrando la separación cromática a través de medios refringentes.
* Variedad de Componentes Ópticos: Instanciación y manipulación de lentes biconvexas, bloques de refracción rectangulares, espejos planos y espejos curvos (cóncavos/convexos).
* Herramientas de Medición: Incorporación de una regla métrica calibrada a escala real por arrastre para medir distancias dentro del lienzo y activación de líneas normales de guía.
* Telemetría Analítica: Panel de datos integrado que expone en tiempo real los ángulos de incidencia, el orden de los impactos y el fenómeno físico activo (Reflexión, Refracción o R.I.T.).
* Persistencia y Personalización: Capacidad para guardar y cargar las escenas diseñadas en archivos JSON y soporte para alternar entre modo claro y modo oscuro.

## 🛠️ Requisitos del Sistema

* Sistema Operativo: Windows 10 / 11 (64 bits)
* Entorno de Ejecución: .NET 8.0 SDK o superior (o la versión de .NET correspondiente a la solución)

## 📦 Instalación y Compilación

Para clonar y compilar este proyecto desde el código fuente, seguí estos pasos:

1. Clonar el repositorio:
   ```bash
   git clone [https://github.com/tu-usuario/UnfathomableMirrors.git](https://github.com/tu-usuario/UnfathomableMirrors.git)
   cd UnfathomableMirrors

2. Restaurar las dependencias del proyecto:
   ```bash
   dotnet restore

3. Compilar y publicar en modo Release (Alta Performance):
   Para garantizar la máxima fluidez y generar un único archivo ejecutable .exe independiente, ejecutá:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfContained=true

4. Ejecutar la aplicación:
   Andá a la carpeta de publicación y abrí el binario generado:
   ```bash
   \bin\Release\netX.X\win-x64\publish\UnfathomableMirrors.exe

## 🎮 Controles e Interacción

* Clic Izquierdo (Arrastrar): Selecciona y desplaza los componentes ópticos o los emisores de luz por el lienzo.
* Tecla 'A': Alterna el modo de interacción de la interfaz entre MOVER (desplazar objetos) y APUNTAR (cambiar la dirección angular del emisor o de la superficie seleccionada con el mouse).
* Clic Derecho: Elimina el componente óptico o el grupo de rayos sobre el que se encuentra el cursor.
