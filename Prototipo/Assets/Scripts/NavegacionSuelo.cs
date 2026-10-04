using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buscador de rutas que camina por el suelo sin necesitar un NavMesh horneado.
/// Construye una rejilla bajo demanda con raycasts: cada celda se sondea sólo si el A*
/// realmente llega a ella, y el resultado se cachea unos segundos (así funcionan las
/// puertas que se abren y el suelo del túnel que aparece a mitad de partida).
/// </summary>
public class NavegacionSuelo
{
    public static readonly NavegacionSuelo Instancia = new NavegacionSuelo();

    // El radio de sondeo es mayor que media diagonal de celda (0.4 * 0.707 = 0.283),
    // así ninguna pared delgada puede colarse entre dos celdas vecinas "libres".
    public float tamanoCelda = 0.4f;
    public float radioSondeo = 0.32f;
    public float alturaLibre = 1.9f;
    public float maxDesnivel = 0.45f;
    public float alturaSondeoArriba = 1.6f;
    public float alcanceSondeoAbajo = 6f;
    public float segundosValidezCelda = 3f;
    public int presupuestoNodos = 6000;
    public float radioBusquedaMaximo = 80f;

    private class Celda
    {
        public bool caminable;
        public float alturaSuelo;
        public float sondeadaEn;
    }

    private readonly Dictionary<Vector2Int, Celda> cache = new Dictionary<Vector2Int, Celda>();
    private readonly Collider[] bufferColisiones = new Collider[16];
    private readonly RaycastHit[] bufferRayos = new RaycastHit[32];
    private readonly HashSet<Collider> colisionadoresIgnorados = new HashSet<Collider>();

    // Estructuras del A*, reutilizadas entre búsquedas para no generar basura.
    private readonly Dictionary<Vector2Int, float> costeG = new Dictionary<Vector2Int, float>();
    private readonly Dictionary<Vector2Int, Vector2Int> padres = new Dictionary<Vector2Int, Vector2Int>();
    private readonly HashSet<Vector2Int> cerrados = new HashSet<Vector2Int>();
    private readonly MonticuloBinario abiertos = new MonticuloBinario();
    private readonly List<Vector3> rutaCruda = new List<Vector3>();

    /// <summary>Cola de prioridad mínima; sin ella el A* degeneraría a O(n²) al buscar el menor coste.</summary>
    private class MonticuloBinario
    {
        private readonly List<Vector2Int> celdas = new List<Vector2Int>();
        private readonly List<float> prioridades = new List<float>();

        public int Cantidad => celdas.Count;

        public void Limpiar()
        {
            celdas.Clear();
            prioridades.Clear();
        }

        public void Insertar(Vector2Int celda, float prioridad)
        {
            celdas.Add(celda);
            prioridades.Add(prioridad);

            int hijo = celdas.Count - 1;
            while (hijo > 0)
            {
                int padre = (hijo - 1) / 2;
                if (prioridades[padre] <= prioridades[hijo]) break;
                Intercambiar(padre, hijo);
                hijo = padre;
            }
        }

        public Vector2Int ExtraerMinimo()
        {
            Vector2Int minimo = celdas[0];
            int ultimo = celdas.Count - 1;

            celdas[0] = celdas[ultimo];
            prioridades[0] = prioridades[ultimo];
            celdas.RemoveAt(ultimo);
            prioridades.RemoveAt(ultimo);

            int padre = 0;
            while (true)
            {
                int izquierda = padre * 2 + 1;
                int derecha = izquierda + 1;
                int menor = padre;

                if (izquierda < celdas.Count && prioridades[izquierda] < prioridades[menor]) menor = izquierda;
                if (derecha < celdas.Count && prioridades[derecha] < prioridades[menor]) menor = derecha;
                if (menor == padre) break;

                Intercambiar(padre, menor);
                padre = menor;
            }

            return minimo;
        }

        private void Intercambiar(int a, int b)
        {
            (celdas[a], celdas[b]) = (celdas[b], celdas[a]);
            (prioridades[a], prioridades[b]) = (prioridades[b], prioridades[a]);
        }
    }

    private static readonly Vector2Int[] vecinos =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
    };

    public void LimpiarCache()
    {
        cache.Clear();
    }

    /// <summary>
    /// Marca un objeto (y sus hijos) como "no es geometría del nivel": el jugador y el bot
    /// no deben contar como pared ni como suelo al sondear la rejilla.
    /// </summary>
    public void RegistrarIgnorado(GameObject objeto)
    {
        if (objeto == null) return;
        foreach (Collider colisionador in objeto.GetComponentsInChildren<Collider>(true))
        {
            colisionadoresIgnorados.Add(colisionador);
        }
    }

    /// <summary>
    /// Calcula una ruta pegada al suelo entre dos puntos. Devuelve false si no hay camino
    /// (por ejemplo, si el destino está detrás de una puerta todavía cerrada).
    /// </summary>
    public bool CalcularRuta(Vector3 origen, Vector3 destino, List<Vector3> resultado)
    {
        resultado.Clear();

        Vector2Int celdaOrigen = ACelda(origen);
        Vector2Int celdaDestino = ACelda(destino);

        Celda inicio = Sondear(celdaOrigen, origen.y);
        if (inicio == null || !inicio.caminable)
        {
            // El jugador puede estar justo sobre el borde de una celda ocupada: probamos alrededor.
            bool encontrada = false;
            foreach (Vector2Int desplazamiento in vecinos)
            {
                Celda alternativa = Sondear(celdaOrigen + desplazamiento, origen.y);
                if (alternativa != null && alternativa.caminable)
                {
                    celdaOrigen += desplazamiento;
                    inicio = alternativa;
                    encontrada = true;
                    break;
                }
            }
            if (!encontrada) return false;
        }

        Celda meta = Sondear(celdaDestino, destino.y);
        if (meta == null || !meta.caminable)
        {
            // El objetivo suele ser el centro de una baldosa o una puerta: buscamos la celda
            // caminable más cercana en un anillo pequeño para poder llegar "hasta su lado".
            Vector2Int mejorCelda = celdaDestino;
            float mejorDistancia = float.MaxValue;
            for (int x = -3; x <= 3; x++)
            {
                for (int z = -3; z <= 3; z++)
                {
                    Vector2Int candidata = celdaDestino + new Vector2Int(x, z);
                    Celda c = Sondear(candidata, destino.y);
                    if (c == null || !c.caminable) continue;

                    float distancia = (candidata - celdaDestino).sqrMagnitude;
                    if (distancia < mejorDistancia)
                    {
                        mejorDistancia = distancia;
                        mejorCelda = candidata;
                        meta = c;
                    }
                }
            }
            if (meta == null || !meta.caminable) return false;
            celdaDestino = mejorCelda;
        }

        if (!BuscarA(celdaOrigen, celdaDestino, inicio.alturaSuelo)) return false;

        ReconstruirRuta(celdaOrigen, celdaDestino, origen, rutaCruda);
        Suavizar(rutaCruda, resultado);
        return resultado.Count >= 2;
    }

    private bool BuscarA(Vector2Int origen, Vector2Int destino, float alturaOrigen)
    {
        costeG.Clear();
        padres.Clear();
        cerrados.Clear();
        abiertos.Limpiar();

        costeG[origen] = 0f;
        abiertos.Insertar(origen, Heuristica(origen, destino));

        int nodosExplorados = 0;
        int celdasRadioMaximo = Mathf.CeilToInt(radioBusquedaMaximo / tamanoCelda);

        while (abiertos.Cantidad > 0 && nodosExplorados < presupuestoNodos)
        {
            Vector2Int actual = abiertos.ExtraerMinimo();
            if (!cerrados.Add(actual)) continue;
            nodosExplorados++;

            if (actual == destino) return true;

            if (Mathf.Abs(actual.x - origen.x) > celdasRadioMaximo ||
                Mathf.Abs(actual.y - origen.y) > celdasRadioMaximo) continue;

            Celda celdaActual = Sondear(actual, alturaOrigen);
            if (celdaActual == null || !celdaActual.caminable) continue;

            for (int v = 0; v < vecinos.Length; v++)
            {
                Vector2Int siguiente = actual + vecinos[v];
                if (cerrados.Contains(siguiente)) continue;

                Celda celdaSiguiente = Sondear(siguiente, celdaActual.alturaSuelo);
                if (celdaSiguiente == null || !celdaSiguiente.caminable) continue;

                float desnivel = Mathf.Abs(celdaSiguiente.alturaSuelo - celdaActual.alturaSuelo);
                if (desnivel > maxDesnivel) continue;

                bool esDiagonal = vecinos[v].x != 0 && vecinos[v].y != 0;
                if (esDiagonal)
                {
                    // Sin recortar esquinas: las dos celdas ortogonales también deben estar libres.
                    Celda lateralX = Sondear(new Vector2Int(siguiente.x, actual.y), celdaActual.alturaSuelo);
                    Celda lateralZ = Sondear(new Vector2Int(actual.x, siguiente.y), celdaActual.alturaSuelo);
                    if (lateralX == null || !lateralX.caminable) continue;
                    if (lateralZ == null || !lateralZ.caminable) continue;
                }

                float paso = esDiagonal ? 1.41421f : 1f;
                float nuevoG = costeG[actual] + paso + desnivel * 2f;

                if (costeG.TryGetValue(siguiente, out float gPrevio) && nuevoG >= gPrevio) continue;

                costeG[siguiente] = nuevoG;
                padres[siguiente] = actual;
                abiertos.Insertar(siguiente, nuevoG + Heuristica(siguiente, destino));
            }
        }

        return false;
    }

    private float Heuristica(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dz = Mathf.Abs(a.y - b.y);
        int menor = Mathf.Min(dx, dz);
        return (dx + dz) + (1.41421f - 2f) * menor;
    }

    private void ReconstruirRuta(Vector2Int origen, Vector2Int destino, Vector3 posicionOrigenReal, List<Vector3> salida)
    {
        salida.Clear();

        Vector2Int actual = destino;
        while (actual != origen)
        {
            salida.Add(APunto(actual));
            if (!padres.TryGetValue(actual, out actual)) break;
        }

        salida.Add(APunto(origen));
        salida.Reverse();

        // El primer punto es la posición real del jugador/bot, no el centro de su celda.
        if (salida.Count > 0)
        {
            salida[0] = new Vector3(posicionOrigenReal.x, salida[0].y, posicionOrigenReal.z);
        }
    }

    /// <summary>
    /// "String pulling": quita los puntos intermedios mientras el tramo directo siga libre,
    /// para que la línea del suelo sea recta en los pasillos y sólo gire en las esquinas.
    /// </summary>
    private void Suavizar(List<Vector3> entrada, List<Vector3> salida)
    {
        salida.Clear();
        if (entrada.Count == 0) return;

        salida.Add(entrada[0]);
        int indiceAncla = 0;

        while (indiceAncla < entrada.Count - 1)
        {
            // Avance en un solo sentido: en cuanto un tramo deja de estar libre, la esquina
            // anterior se convierte en vértice. Es O(n) en vez de O(n²) probando desde el final.
            int siguienteVisible = indiceAncla + 1;
            while (siguienteVisible + 1 < entrada.Count && TramoLibre(entrada[indiceAncla], entrada[siguienteVisible + 1]))
            {
                siguienteVisible++;
            }

            salida.Add(entrada[siguienteVisible]);
            indiceAncla = siguienteVisible;
        }
    }

    private bool TramoLibre(Vector3 a, Vector3 b)
    {
        if (Mathf.Abs(a.y - b.y) > maxDesnivel) return false;

        Vector3 desdeBajo = a + Vector3.up * 0.55f;
        Vector3 hastaBajo = b + Vector3.up * 0.55f;
        Vector3 desdeAlto = a + Vector3.up * 1.45f;
        Vector3 hastaAlto = b + Vector3.up * 1.45f;

        if (HayColision(desdeBajo, hastaBajo) || HayColision(desdeAlto, hastaAlto)) return false;

        return HaySueloContinuo(a, b);
    }

    /// <summary>
    /// Impide que el suavizado recorte una esquina pasando por encima de un precipicio:
    /// muestrea el tramo y exige suelo a una altura coherente en todo el recorrido.
    /// </summary>
    private bool HaySueloContinuo(Vector3 a, Vector3 b)
    {
        float longitud = Vector3.Distance(a, b);
        int muestras = Mathf.CeilToInt(longitud / tamanoCelda);

        for (int i = 1; i < muestras; i++)
        {
            Vector3 punto = Vector3.Lerp(a, b, i / (float)muestras);
            if (!AlturaDelSuelo(punto, out float altura)) return false;
            if (Mathf.Abs(altura - punto.y) > maxDesnivel) return false;
        }

        return true;
    }

    // ------------------------------------------------------------------
    // CENTRADO EN EL PASILLO: la ruta de CalcularRuta va tensa, pegada a las esquinas.
    // Esto la lleva al medio para que la línea y el bot (y su globo) no rocen las paredes.
    // ------------------------------------------------------------------

    private readonly List<Vector3> muestrasCentrado = new List<Vector3>();
    private readonly List<Vector3> puntosCentrados = new List<Vector3>();

    /// <summary>
    /// Lleva una ruta al medio de los pasillos: la remuestrea cada 'paso' metros, corre cada
    /// punto al centro, la suaviza (curvas redondeadas) y descarta cualquier corrimiento que
    /// la haga atravesar una pared o salirse del suelo. Sólo centra los primeros
    /// 'metrosCentrados' (por rendimiento en el celular); el primer punto no se mueve.
    /// </summary>
    public void CentrarRuta(List<Vector3> ruta, float anchoBusqueda, float separacionMinima, float metrosCentrados, float paso = 0.4f)
    {
        if (ruta.Count < 2) return;

        // 1. Remuestreo de los tramos que caen dentro de 'metrosCentrados'.
        muestrasCentrado.Clear();
        muestrasCentrado.Add(ruta[0]);
        float recorrido = 0f;
        int tramo = 0;
        for (; tramo < ruta.Count - 1 && recorrido < metrosCentrados; tramo++)
        {
            float largo = Vector3.Distance(ruta[tramo], ruta[tramo + 1]);
            int muestras = Mathf.Max(1, Mathf.CeilToInt(largo / paso));
            for (int j = 1; j <= muestras; j++)
            {
                muestrasCentrado.Add(Vector3.Lerp(ruta[tramo], ruta[tramo + 1], j / (float)muestras));
            }
            recorrido += largo;
        }

        // Los puntos [1, ultimoCentrable) se centran; el último muestreado queda fijo como
        // empalme con el resto de la ruta, que sigue tal cual.
        int ultimoCentrable = muestrasCentrado.Count - 1;
        for (int k = tramo + 1; k < ruta.Count; k++) muestrasCentrado.Add(ruta[k]);
        if (ultimoCentrable < 2) return;

        // 2. Cada punto, al medio del pasillo.
        puntosCentrados.Clear();
        puntosCentrados.AddRange(muestrasCentrado);
        for (int i = 1; i < ultimoCentrable; i++)
        {
            Vector3 direccion = muestrasCentrado[i + 1] - muestrasCentrado[i - 1];
            puntosCentrados[i] = CentrarPunto(muestrasCentrado[i], direccion, anchoBusqueda, separacionMinima);
        }

        // 3. Suavizado: quita los saltos donde el pasillo cambia de ancho y redondea las curvas.
        for (int pasada = 0; pasada < 4; pasada++)
        {
            Vector3 anterior = puntosCentrados[0];
            for (int i = 1; i < ultimoCentrable; i++)
            {
                Vector3 actual = puntosCentrados[i];
                puntosCentrados[i] = (anterior + actual * 2f + puntosCentrados[i + 1]) * 0.25f;
                anterior = actual;
            }
        }

        // 4. Validación: si un tramo centrado choca o pierde el suelo, ese punto vuelve a la ruta original.
        ruta.Clear();
        ruta.Add(puntosCentrados[0]);
        for (int i = 1; i < puntosCentrados.Count; i++)
        {
            Vector3 candidato = puntosCentrados[i];
            if (i < ultimoCentrable)
            {
                if (AlturaDelSuelo(candidato, out float alturaSuelo)) candidato.y = alturaSuelo;
                if (!TramoLibre(ruta[ruta.Count - 1], candidato)) candidato = muestrasCentrado[i];
            }
            ruta.Add(candidato);
        }
    }

    /// <summary>
    /// Corre un punto del suelo hacia el medio del pasillo, midiendo con rayos las paredes a
    /// cada lado de 'direccion'. Si sólo hay pared de un lado, se aleja de ella hasta
    /// 'separacionMinima'. Nunca lo corre a donde no hay suelo continuo (puente, vacío).
    /// </summary>
    public Vector3 CentrarPunto(Vector3 punto, Vector3 direccion, float anchoBusqueda, float separacionMinima, float alturaAlta = 1.25f)
    {
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.0001f) return punto;
        Vector3 derecha = Vector3.Cross(Vector3.up, direccion.normalized);

        float izquierda = DistanciaAPared(punto, -derecha, anchoBusqueda, alturaAlta);
        float aLaDerecha = DistanciaAPared(punto, derecha, anchoBusqueda, alturaAlta);
        bool paredIzquierda = izquierda < anchoBusqueda;
        bool paredDerecha = aLaDerecha < anchoBusqueda;

        float desplazamiento = 0f;
        if (paredIzquierda && paredDerecha) desplazamiento = (aLaDerecha - izquierda) * 0.5f;
        else if (paredIzquierda) desplazamiento = Mathf.Max(0f, separacionMinima - izquierda);
        else if (paredDerecha) desplazamiento = -Mathf.Max(0f, separacionMinima - aLaDerecha);

        // Si no hay suelo hasta el punto corrido, prueba con la mitad del desplazamiento.
        for (int intento = 0; intento < 2 && Mathf.Abs(desplazamiento) > 0.05f; intento++)
        {
            Vector3 candidato = punto + derecha * desplazamiento;
            if (HaySueloContinuo(punto, candidato) && AlturaDelSuelo(candidato, out float alturaSuelo)
                && Mathf.Abs(alturaSuelo - punto.y) <= maxDesnivel)
            {
                return candidato;
            }
            desplazamiento *= 0.5f;
        }

        return punto;
    }

    /// <summary>Distancia a la pared más cercana en esa dirección, a la altura de las piernas y a 'alturaAlta'.</summary>
    private float DistanciaAPared(Vector3 suelo, Vector3 direccion, float alcance, float alturaAlta)
    {
        return Mathf.Min(
            DistanciaLibre(suelo + Vector3.up * 0.5f, direccion, alcance),
            DistanciaLibre(suelo + Vector3.up * alturaAlta, direccion, alcance));
    }

    /// <summary>Metros libres desde 'origen' en 'direccion' (o 'alcance' si no choca con nada del nivel).</summary>
    public float DistanciaLibre(Vector3 origen, Vector3 direccion, float alcance)
    {
        float menor = alcance;
        int impactos = Physics.RaycastNonAlloc(origen, direccion, bufferRayos, alcance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < impactos; i++)
        {
            if (EsIgnorable(bufferRayos[i].collider)) continue;
            menor = Mathf.Min(menor, bufferRayos[i].distance);
        }
        return menor;
    }

    private bool HayColision(Vector3 a, Vector3 b)
    {
        int cantidad = Physics.OverlapCapsuleNonAlloc(a, b, radioSondeo, bufferColisiones, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cantidad; i++)
        {
            if (EsIgnorable(bufferColisiones[i])) continue;
            return true;
        }
        return false;
    }

    private Celda Sondear(Vector2Int celda, float alturaReferencia)
    {
        if (cache.TryGetValue(celda, out Celda existente))
        {
            bool vigente = Time.time - existente.sondeadaEn < segundosValidezCelda;
            bool mismaAltura = Mathf.Abs(existente.alturaSuelo - alturaReferencia) < 2f;
            if (vigente && (mismaAltura || !existente.caminable)) return existente;
        }

        Celda resultado = existente ?? new Celda();
        resultado.sondeadaEn = Time.time;
        resultado.caminable = false;

        Vector3 centro = new Vector3((celda.x + 0.5f) * tamanoCelda, 0f, (celda.y + 0.5f) * tamanoCelda);
        Vector3 origenRayo = new Vector3(centro.x, alturaReferencia + alturaSondeoArriba, centro.z);

        float mejorAltura = float.MinValue;
        bool haySuelo = false;

        int impactos = Physics.RaycastNonAlloc(origenRayo, Vector3.down, bufferRayos, alcanceSondeoAbajo, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < impactos; i++)
        {
            RaycastHit impacto = bufferRayos[i];
            if (EsIgnorable(impacto.collider)) continue;
            if (impacto.normal.y < 0.6f) continue; // Superficie demasiado inclinada para caminar.

            if (impacto.point.y > mejorAltura)
            {
                mejorAltura = impacto.point.y;
                haySuelo = true;
            }
        }

        if (haySuelo)
        {
            resultado.alturaSuelo = mejorAltura;

            Vector3 pieCapsula = new Vector3(centro.x, mejorAltura + radioSondeo + 0.1f, centro.z);
            Vector3 cabezaCapsula = new Vector3(centro.x, mejorAltura + alturaLibre - radioSondeo, centro.z);
            resultado.caminable = !HayColision(pieCapsula, cabezaCapsula);
        }

        cache[celda] = resultado;
        return resultado;
    }

    /// <summary>
    /// El jugador y el propio bot no cuentan como paredes ni como suelo.
    /// </summary>
    private bool EsIgnorable(Collider colisionador)
    {
        if (colisionador == null) return true;
        if (colisionador.isTrigger) return true;
        if (colisionadoresIgnorados.Contains(colisionador)) return true;
        if (colisionador.CompareTag("Player")) return true;
        return false;
    }

    public Vector2Int ACelda(Vector3 punto)
    {
        return new Vector2Int(
            Mathf.FloorToInt(punto.x / tamanoCelda),
            Mathf.FloorToInt(punto.z / tamanoCelda));
    }

    private Vector3 APunto(Vector2Int celda)
    {
        float altura = cache.TryGetValue(celda, out Celda c) ? c.alturaSuelo : 0f;
        return new Vector3((celda.x + 0.5f) * tamanoCelda, altura, (celda.y + 0.5f) * tamanoCelda);
    }

    /// <summary>
    /// Devuelve la altura exacta del suelo bajo un punto (para pegar la línea y el bot al piso).
    /// </summary>
    public bool AlturaDelSuelo(Vector3 punto, out float altura)
    {
        altura = punto.y;
        Vector3 origenRayo = punto + Vector3.up * alturaSondeoArriba;

        float mejorAltura = float.MinValue;
        bool encontrado = false;

        int impactos = Physics.RaycastNonAlloc(origenRayo, Vector3.down, bufferRayos, alcanceSondeoAbajo, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < impactos; i++)
        {
            if (EsIgnorable(bufferRayos[i].collider)) continue;
            if (bufferRayos[i].normal.y < 0.6f) continue;
            if (bufferRayos[i].point.y > mejorAltura)
            {
                mejorAltura = bufferRayos[i].point.y;
                encontrado = true;
            }
        }

        if (encontrado) altura = mejorAltura;
        return encontrado;
    }
}
