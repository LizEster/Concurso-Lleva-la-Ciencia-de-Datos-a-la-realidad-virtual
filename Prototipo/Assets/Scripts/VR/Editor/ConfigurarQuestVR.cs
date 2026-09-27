using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

/// <summary>
/// Menú "VR > Configurar proyecto para Meta Quest": deja el proyecto listo para compilar un
/// APK que corre directo en el visor (sin PC). Se puede ejecutar varias veces sin problema.
/// Requiere tener instalado en Unity Hub el módulo "Android Build Support" (con OpenJDK y
/// Android SDK & NDK Tools).
/// </summary>
public static class ConfigurarQuestVR
{
    private const string CarpetaXR = "Assets/XR";
    private const string RutaAjustesXR = CarpetaXR + "/XRGeneralSettingsPerBuildTarget.asset";

    // Mandos de Quest 1/2 (Touch), Quest 3 (Touch Plus) y Quest Pro (Touch Pro), más el soporte de Quest.
    private static readonly string[] FeaturesOpenXR =
    {
        "MetaQuestFeature",
        "OculusTouchControllerProfile",
        "MetaQuestTouchPlusControllerProfile",
        "MetaQuestTouchProControllerProfile",
    };

    [MenuItem("VR/Configurar proyecto para Meta Quest")]
    public static void Configurar()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            EditorUtility.DisplayDialog("Falta el módulo Android",
                "Instala 'Android Build Support' (con OpenJDK y Android SDK & NDK Tools) para esta versión de Unity desde Unity Hub > Installs > Add modules, reinicia Unity y vuelve a ejecutar este menú.",
                "OK");
            return;
        }

        ConfigurarAndroid();
        ConfigurarXRManagement();
        ConfigurarOpenXR();
        ConfigurarURPParaVisor();

        ActivarAndroid();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Listo",
            "Proyecto configurado para Meta Quest.\n\nPara probar: conecta el Quest por USB (con el modo desarrollador activado) y usa File > Build Profiles > Android > Build And Run.\n\nRevisa también Project Settings > XR Plug-in Management > Project Validation por si queda alguna advertencia.",
            "OK");
    }

    /// <summary>
    /// Compila el APK con las escenas de Build Settings en "Builds/SedAlgoritmica.apk" (junto a
    /// la carpeta Assets). También se puede llamar por consola con
    /// -executeMethod ConfigurarQuestVR.CompilarAPK
    /// </summary>
    [MenuItem("VR/Compilar APK para Quest")]
    public static void CompilarAPK()
    {
        // URP elige qué pipelines y shaders incluir según la plataforma ACTIVA del editor, no
        // según la del build: si seguía activa Mac, el APK salía con el pipeline de PC.
        ActivarAndroid();

        string[] escenas = EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray();
        var opciones = new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = "Builds/SedAlgoritmica.apk",
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        };

        var reporte = BuildPipeline.BuildPlayer(opciones);
        Debug.Log($"ConfigurarQuestVR: build {reporte.summary.result}, {reporte.summary.totalErrors} errores, {reporte.summary.totalSize / (1024 * 1024)} MB -> {opciones.locationPathName}");

        if (Application.isBatchMode)
            EditorApplication.Exit(reporte.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }

    /// <summary>
    /// Deja Android como plataforma activa. El proyecto tiene un Build Profile "macOS"
    /// (Assets/Settings/Build Profiles) que Unity reactiva al abrir el proyecto si estaba
    /// activo, deshaciendo el cambio; por eso primero volvemos al perfil de plataforma normal.
    /// </summary>
    private static void ActivarAndroid()
    {
        BuildProfile activo = BuildProfile.GetActiveBuildProfile();
        if (activo != null)
        {
            Debug.Log($"ConfigurarQuestVR: se desactiva el Build Profile '{activo.name}' para usar la plataforma Android.");
            BuildProfile.SetActiveBuildProfile(null);
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
    }

    private static void ConfigurarAndroid()
    {
        NamedBuildTarget android = NamedBuildTarget.Android;

        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.colorSpace = ColorSpace.Linear;

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

        // El identificador venía de la plantilla de Unity; el Quest necesita uno propio.
        string id = PlayerSettings.GetApplicationIdentifier(android);
        if (string.IsNullOrEmpty(id) || id.Contains("unity.template"))
            PlayerSettings.SetApplicationIdentifier(android, "com.concursocienciadatos.sedalgoritmica");
    }

    private static void ConfigurarXRManagement()
    {
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget porPlataforma);
        if (porPlataforma == null)
        {
            if (!AssetDatabase.IsValidFolder(CarpetaXR)) AssetDatabase.CreateFolder("Assets", "XR");

            porPlataforma = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(RutaAjustesXR);
            if (porPlataforma == null)
            {
                porPlataforma = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(porPlataforma, RutaAjustesXR);
            }
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, porPlataforma, true);
        }

        XRGeneralSettings ajustes = porPlataforma.SettingsForBuildTarget(BuildTargetGroup.Android);
        if (ajustes == null)
        {
            ajustes = ScriptableObject.CreateInstance<XRGeneralSettings>();
            ajustes.name = "Android Settings";
            porPlataforma.SetSettingsForBuildTarget(BuildTargetGroup.Android, ajustes);
            AssetDatabase.AddObjectToAsset(ajustes, porPlataforma);
        }

        if (ajustes.Manager == null)
        {
            XRManagerSettings manager = ScriptableObject.CreateInstance<XRManagerSettings>();
            manager.name = "Android Providers";
            ajustes.Manager = manager;
            AssetDatabase.AddObjectToAsset(manager, porPlataforma);
        }

        ajustes.InitManagerOnStart = true;
        XRPackageMetadataStore.AssignLoader(ajustes.Manager, typeof(OpenXRLoader).FullName, BuildTargetGroup.Android);

        EditorUtility.SetDirty(ajustes);
        EditorUtility.SetDirty(porPlataforma);
    }

    private static void ConfigurarOpenXR()
    {
        // En modo batch (o si nadie abrió aún la ventana de OpenXR) la lista de features todavía
        // no existe: sin esto GetFeatures() vendría vacío y no se activaría nada.
        FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);

        OpenXRSettings openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (openXR == null)
        {
            Debug.LogWarning("ConfigurarQuestVR: no se encontraron los ajustes de OpenXR para Android. Abre Project Settings > XR Plug-in Management una vez y vuelve a ejecutar el menú.");
            return;
        }

        openXR.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;

        foreach (var feature in openXR.GetFeatures())
        {
            if (FeaturesOpenXR.Contains(feature.GetType().Name))
            {
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
                Debug.Log($"ConfigurarQuestVR: feature OpenXR activada: {feature.GetType().Name}");
            }
        }
        EditorUtility.SetDirty(openXR);
    }

    /// <summary>
    /// Ajustes del asset URP que usa Android (nivel de calidad "Mobile"): en un visor el HDR
    /// cuesta mucho, el MSAA 4x es casi gratis en la GPU del Quest y la escala 0.8 se ve borrosa.
    /// </summary>
    private static void ConfigurarURPParaVisor()
    {
        int nivelMovil = System.Array.IndexOf(QualitySettings.names, "Mobile");
        if (nivelMovil < 0) return;

        if (QualitySettings.GetRenderPipelineAssetAt(nivelMovil) is UniversalRenderPipelineAsset urp)
        {
            urp.supportsHDR = false;
            urp.msaaSampleCount = 4;
            urp.renderScale = 1f;
            EditorUtility.SetDirty(urp);
        }
    }
}
