using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PrefabPainterWindow : EditorWindow
{
    private const string TITULO_VENTANA = "Pintor de Prefabs";
    private static readonly Color COLOR_ACTIVO   = new Color(0.25f, 0.75f, 0.35f, 1f);
    private static readonly Color COLOR_INACTIVO = new Color(0.22f, 0.22f, 0.22f, 1f);
    private static readonly Color COLOR_CABECERA = new Color(0.15f, 0.15f, 0.18f, 1f);

    [SerializeField] private List<GameObject> listaPrefabs       = new List<GameObject>();
    [SerializeField] private int              indicePrefabActivo = -1;
    [SerializeField] private Transform        transformPadre     = null;

    private GameObject ultimaInstanciaColocada = null;
    private bool       arrastrando             = false;
    private Vector2    ultimaPosRaton;
    private Vector2    posicionScroll;

    [MenuItem("Tools/Pintor de Prefabs")]
    public static void AbrirVentana()
    {
        PrefabPainterWindow ventana = GetWindow<PrefabPainterWindow>(TITULO_VENTANA);
        ventana.minSize = new Vector2(280f, 400f);
        ventana.Show();
    }

    private void OnEnable()  => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnGUI()
    {
        DibujarCabecera();
        DibujarListaPrefabs();
        DibujarCampoPadre();
        DibujarAyuda();
    }

    private void DibujarCabecera()
    {
        EditorGUI.DrawRect(new Rect(0, 0, position.width, 48), COLOR_CABECERA);

        GUIStyle estiloTitulo = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 16,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white }
        };
        GUILayout.Space(10);
        GUILayout.Label(TITULO_VENTANA, estiloTitulo);
        GUILayout.Space(8);

        GUIStyle estiloSubtitulo = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.7f, 0.9f, 0.7f) }
        };
        GUILayout.Label("Clic para colocar · Arrastra para rotar", estiloSubtitulo);
        GUILayout.Space(6);
    }

    private void DibujarListaPrefabs()
    {
        EditorGUILayout.LabelField("─── Prefabs ────────────────────────", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Añadir Slot", GUILayout.Height(22)))
            listaPrefabs.Add(null);

        GUI.enabled = listaPrefabs.Count > 0;
        if (GUILayout.Button("− Eliminar Último", GUILayout.Height(22)))
        {
            listaPrefabs.RemoveAt(listaPrefabs.Count - 1);
            indicePrefabActivo = Mathf.Clamp(indicePrefabActivo, -1, listaPrefabs.Count - 1);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);

        posicionScroll = EditorGUILayout.BeginScrollView(posicionScroll, GUILayout.MaxHeight(220));
        for (int i = 0; i < listaPrefabs.Count; i++)
        {
            bool estaActivo = (i == indicePrefabActivo);

            Rect rectFila = EditorGUILayout.BeginHorizontal(GUILayout.Height(30));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rectFila, estaActivo ? COLOR_ACTIVO : COLOR_INACTIVO);

            GUIStyle estiloSeleccion = new GUIStyle(EditorStyles.miniButtonLeft)
            {
                normal      = { textColor = estaActivo ? Color.black : Color.white },
                fontStyle   = estaActivo ? FontStyle.Bold : FontStyle.Normal,
                fixedWidth  = 24,
                fixedHeight = 22
            };
            if (GUILayout.Button(estaActivo ? "★" : "☆", estiloSeleccion))
                indicePrefabActivo = estaActivo ? -1 : i;

            EditorGUI.BeginChangeCheck();
            GameObject nuevoPrefab = (GameObject)EditorGUILayout.ObjectField(
                listaPrefabs[i], typeof(GameObject), false, GUILayout.Height(22));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Cambiar Prefab");
                listaPrefabs[i] = nuevoPrefab;
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }
        EditorGUILayout.EndScrollView();
        GUILayout.Space(4);

        if (indicePrefabActivo >= 0 && indicePrefabActivo < listaPrefabs.Count && listaPrefabs[indicePrefabActivo] != null)
        {
            GUIStyle estiloActivo = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = COLOR_ACTIVO } };
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Activo: ", estiloActivo);
            GUILayout.Label(listaPrefabs[indicePrefabActivo].name);
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Ningún prefab activo. Pulsa ☆ para activar uno.", MessageType.Info);
        }
    }

    private void DibujarCampoPadre()
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("─── Objeto Padre ───────────────────", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(4);

        EditorGUI.BeginChangeCheck();
        Transform nuevoPadre = (Transform)EditorGUILayout.ObjectField(
            "Transform Padre", transformPadre, typeof(Transform), true);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Establecer Padre");
            transformPadre = nuevoPadre;
        }

        if (transformPadre == null)
            EditorGUILayout.HelpBox("Déjalo vacío para colocar en la raíz de la escena.", MessageType.None);
    }

    private void DibujarAyuda()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("─── Instrucciones ──────────────────", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(2);

        GUIStyle estiloAyuda = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
        {
            normal = { textColor = new Color(0.75f, 0.75f, 0.75f) }
        };
        GUILayout.Label(
            "1. Añade prefabs y activa uno (★).\n" +
            "2. Opcionalmente, asigna un objeto padre.\n" +
            "3. Haz clic en la Scene View para colocar.\n" +
            "4. Mantén pulsado y arrastra para rotar.\n" +
            "5. Todo soporta Ctrl+Z (Deshacer).",
            estiloAyuda);
    }

    private void OnSceneGUI(SceneView _)
    {
        Event evento = Event.current;

        if (indicePrefabActivo < 0 || indicePrefabActivo >= listaPrefabs.Count) return;
        GameObject prefabActivo = listaPrefabs[indicePrefabActivo];
        if (prefabActivo == null) return;

        if (evento.button == 0 && (evento.type == EventType.MouseDown ||
                                    evento.type == EventType.MouseDrag ||
                                    evento.type == EventType.MouseUp))
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (evento.type == EventType.MouseDown && evento.button == 0 && !evento.alt)
        {
            Ray rayo = HandleUtility.GUIPointToWorldRay(evento.mousePosition);
            Vector3 posMundo;

            if (Physics.Raycast(rayo, out RaycastHit impacto))
                posMundo = impacto.point;
            else
            {
                Plane planoSuelo = new Plane(Vector3.up, Vector3.zero);
                posMundo = planoSuelo.Raycast(rayo, out float distancia)
                    ? rayo.GetPoint(distancia)
                    : rayo.GetPoint(10f);
            }

            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefabActivo, transformPadre);
            instancia.transform.SetPositionAndRotation(posMundo, Quaternion.identity);

            Undo.RegisterCreatedObjectUndo(instancia, "Colocar Prefab");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(instancia.scene);

            ultimaInstanciaColocada = instancia;
            arrastrando             = true;
            ultimaPosRaton          = evento.mousePosition;
            evento.Use();
            SceneView.RepaintAll();
        }
        else if (evento.type == EventType.MouseDrag && evento.button == 0 && arrastrando && ultimaInstanciaColocada != null)
        {
            float deltaRotacion = (evento.mousePosition.x - ultimaPosRaton.x) * 1.5f;
            Undo.RecordObject(ultimaInstanciaColocada.transform, "Rotar Prefab");
            ultimaInstanciaColocada.transform.Rotate(Vector3.up, deltaRotacion, Space.World);
            ultimaPosRaton = evento.mousePosition;
            evento.Use();
            SceneView.RepaintAll();
        }
        else if (evento.type == EventType.MouseUp && evento.button == 0)
        {
            arrastrando             = false;
            ultimaInstanciaColocada = null;
        }
    }
}
