# ST0263 - TÓPICOS ESPECIALES EN TELEMÁTICA
#
## Estudiante: Samuel Rendón Trujillo - srendont@eafit.edu.co
## Video del proyecto: https://youtu.be/9G0vz8kZ7UM (EL README SE ACTUALIZÓ EL DÍA 02/09/2024 PARA AÑADIR EL LINK DEL VIDEO ÚNICAMENTE, DEBIDO AL PLAZO ACORDADO EN CLASE Y DADO QUE EL BUZÓN EN INTERACTIVA YA SE ENCONTRABA VENCIDO)
## Profesor: Alvaro Enrique Ospina Sanjuan - aeospinas@eafit.edu.co


<hr>


# Reto 1. Red P2P - RPC


# 1. Descripción

El sistema se desarrolló en C# - .NET. <br>
Se escogió esta tecnología debido al conocimiento previo de uso de esta y el uso del enfoque orientado a objetos para una mejor implentación de las abstracciones de los componenetes del sistema desarrollado.

## 1.1. Aspectos logrados

Se cumplió con la implementación de una red P2P funcional con los siguientes componentes:
- Nodos independientes que se pueden unir a la red conociendo la dirección de un nodo perteneciente a la red
- Nodos concurentes que actúan como Cliente y Servidor dentro de la red.
- Comunicación entre nodos utilizando gRPC.
- Creación de recursos (archivos) y almacenado de estos en la red.
- Consulta de recursos (archivos) almacenados en diferentes nodos de la red.
- Implementación de DHT.
- Implementación de anillo Chord.

## 1.2 Aspectos no logrados

No se cumplió con lo siguiente:

- Despliegue en Docker o instancias de AWS. (Debido a problemas con el manejo del tiempo)
- Utilización de MOM y API REST. (Esto debido a que bajo criterio personal, los requisitos planteados para el sistema se podían alcanzar usando gRPC y manteniendo una estructura e implementación más consistente)

# 2. información general de diseño de alto nivel, arquitectura, patrones, mejores prácticas utilizadas.

Anillo Chord y DHT:
Se utilizó una Distributed Hash Table para la comunicación entre nodos, creación y obtención de archivos.

La utilización de la DHT parte de la implementación del anillo Chord, estableciendo para cada nodo sus nodos vecinos (sucesor y predecesor), utilzando el algoritmo SHA-1 para la obtención de valores hash partiendo de la dirección de los nodos y los nombres de los recursos a almacenar en la red.

Se estucturó la DHT de la siguiente manera:

Estructura de datos: Dictionary<string, string>

Claves de la forma: "Node1IdHash,Node2IdHash" para almacenar los rangos de las claves.

Valores de la forma: "NodeAddress" para almacenar la diercción del nodo encargado de los valores encontrados dentro del rango de valores almacenados en la clave.

DHT = 
{
    {"hashedIdRange" : "responsibleNodeAddress"}, ...
}



<img src="https://upload.wikimedia.org/wikipedia/commons/2/20/Chord_network.png">

# 3. Descripción del ambiente de desarrollo y técnico:

Lenguaje de Programación: C# - .NET

Librerías:
- Google.Protobuf (3.28.0)
- Grpc.AspNet.Core (2.65.0)
- Grpc.Tools (2.66.0)
- Grpc.Core (2.46.6)

Todas las anteriores son utilizadas para la implementación de gRPC dentro del ambiente de .NET.


## Estructura del código:

## Solución: 
Componente base de todo proyecto de .NET, contiene y administra todo el proyecto.

### Aplicación de consola: 
P2PNode. Contiene toda la implementación de la lógica y comportamiento de un nodo dentro dentro de una red P2P.

#### Extensions:
Contiene extensiones de diferentes clases nativas para añadir funcionalidades. En este caso, se implemtó para el Dictionary<string, string>, para añadir funcionalidades relativas a la implementación de la DHT.

#### Node:
Contiene 3 clases con implementación de funcionalidades para el nodo de la red P2P:
- P2PNode: Contiene la implementación de más alto nivel para la abstracción del nodo del sistema. Conteniendo los métodos que corren asíncronicamente diferentes hilos para las funcionalidades que implementa el nodo.
- NodeClient: Contiene toda la lógica e implementación de la parte del nodo de la red correspondiente a su comportamiento como cliente.
- NodeServer: Contiene toda la lógica e implementación de la parte del nodo de la red correspondiente a su comportamiento como servidor. 

#### Protos:
Contiene el archivo p2p.proto donde se declaran todos los mensajes y funciones que se implementarán por medio de gRPC.

#### Resources: 
Carpeta donde se almacenan las carpetas nombradas con los puertos de las instancias de los nodos que se ejecuten para almacenar los archivos de los que son responsables dichos nodos.

#### Services:
Contiene clases estáticas que representan servicios fijos que utilizan las demás clases del sistema para su funcionamiento:

- FileService: Clase estática donde se implementan métodos para la manipulación de recursos (archivos) y sus carpetas relativas a cada nodo.
- P2PServiceImpl: Clase donde se sobreescribne los métodos generados al compilar el archivo p2p.ptoro para determinar el comportamiento de los nodos ante los diferentes comandos establecidos.



## Compilación y ejecución

Dentro de la carpeta P2P-gRPC/P2PNode: 

### Compilación:

dotnet build


### Ejecución:

dotnet run -- <"puerto">

Una vez ejecutándose el programa pregunta si se desea unir a un anillo existente, se debe responder y (Sí) en caso de que ya haya un anillo creado, n (No) en caso de que sea el primer nodo dentro de la red que se va a crear.

En caso de responder a y (Sí) para unirse a un anillo existente, se debe poner la dirección de un nodo existente dentro de la red a la que se quiere unir. Ejemplo: localhost:<"puertoNodoExistente">

Esto vinculará el nodo a una red nueva o a una existente dependiendo de la elección.

Tras haber instanciado varios nodos corriendo diferentes instancias del programa, se podrá realizar acciones partiendo de las opciones:

0 -> Subir un recurso
1 -> Buscar un recurso

Subir un recurso: Tendrá que ingresar nombre y contenido del recurso a agregar, tras ingresar esto, el nodo creador buscará al nodo responsable de almacenar este recurso basado en el hash obtenido de sus título y se lo enviará por medio de gRPC para que el nodo responsble lo almacene.

Buscar un recurso: Tendrá que ingresar el nombre del recurso a buscar, el nodo que consulta buscará el nodo responsable del recurso buscado y se lo solicitará por medio de mensajes gRPC. El nodo responsable proveerá el nombre y el contenido del recurso solicitado.

Los resultados, ejecución y descripción del sistema se detallan mejor en el video correspondiente de la entrega.


# 5. Opinión personal:

Desde lo personal, encontré el proyecto muy interesante y la implementación de la red y de CHORD / DHT resultó en un reto de complejidad moderada. Requerí de consultar mucha información del funcionamiento de Chord y DHT para redes P2P, pero la implementación completa del sistema me permitió alcanzar un muy buen dominio del flujo de la comunicación en una red P2P y la implementación de mensajería entre sistemas utilizando gRPC. Me gustaría haber podido desplegar la aplicación en un servicio como Docker ya que no lo he utilizado hasta el momento, pero por cuestiones de tiempo de entrega y problemas con la utilización del plazo dado no lo pude lograr, intentaré hacerlo y en caso de conseguirlo incluiré la muestra del despliegue en el video.

# Referencias:

 - Video para muestra visual del funcionamiento básico de Chord y DHT:
https://www.youtube.com/watch?v=1wTucsUm64s&t=140s

- Documentación de gRPC para .NET: https://learn.microsoft.com/en-us/aspnet/core/grpc/?view=aspnetcore-8.0

- DHT Theory: https://interactivavirtual.eafit.edu.co/d2l/le/content/169826/viewContent/921695/View



